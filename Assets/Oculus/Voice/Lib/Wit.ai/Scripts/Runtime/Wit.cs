/*
 * Copyright (c) Facebook, Inc. and its affiliates.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Net;
using Facebook.WitAi.Configuration;
using Facebook.WitAi.Data;
using Facebook.WitAi.Events;
using Facebook.WitAi.Interfaces;
using Facebook.WitAi.Lib;
using UnityEngine.Serialization;

namespace Facebook.WitAi
{
    public class Wit : VoiceService, IWitRuntimeConfigProvider
    {
        [Header("Wit Configuration")]
        [FormerlySerializedAs("configuration")]
        [Tooltip("The configuration that will be used when activating wit. This includes api key.")]
        [SerializeField]
        private WitRuntimeConfiguration runtimeConfiguration = new WitRuntimeConfiguration();

        private IAudioInputSource micInput;
        private WitRequestOptions currentRequestOptions;
        private float lastMinVolumeLevelTime;
        private WitRequest activeRequest;

        private bool isSoundWakeActive;
        private RingBuffer<byte> micDataBuffer;
        private RingBuffer<byte>.Marker lastSampleMarker;
        private byte[] writeBuffer;
        private bool minKeepAliveWasHit;
        private bool isActive;
        private byte[] byteDataBuffer;

        private ITranscriptionProvider activeTranscriptionProvider;
        private Coroutine timeLimitCoroutine;

        // Transcription based endpointing
        private bool receivedTranscription;
        private float lastWordTime;

        #region Interfaces
        private IWitByteDataReadyHandler[] dataReadyHandlers;
        private IWitByteDataSentHandler[] dataSentHandlers;
        private Coroutine micInitCoroutine;

        #endregion

#if DEBUG_SAMPLE
        private FileStream sampleFile;
#endif

        /// <summary>
        /// Returns true if wit is currently active and listening with the mic
        /// </summary>
        public override bool Active => isActive || IsRequestActive;

        public override bool IsRequestActive => null != activeRequest && activeRequest.IsActive;

        public WitRuntimeConfiguration RuntimeConfiguration
        {
            get => runtimeConfiguration;
            set
            {
                runtimeConfiguration = value;

                InitializeConfig();
            }
        }

        /// <summary>
        /// Gets/Sets a custom transcription provider. This can be used to replace any built in asr
        /// with an on device model or other provided source
        /// </summary>
        public override ITranscriptionProvider TranscriptionProvider
        {
            get => activeTranscriptionProvider;
            set
            {
                if (null != activeTranscriptionProvider)
                {
                    activeTranscriptionProvider.OnFullTranscription.RemoveListener(
                        OnFullTranscription);
                    activeTranscriptionProvider.OnPartialTranscription.RemoveListener(
                        OnPartialTranscription);
                    activeTranscriptionProvider.OnMicLevelChanged.RemoveListener(
                        OnTranscriptionMicLevelChanged);
                    activeTranscriptionProvider.OnStartListening.RemoveListener(
                        OnStartListening);
                    activeTranscriptionProvider.OnStoppedListening.RemoveListener(
                        OnStoppedListening);
                }

                activeTranscriptionProvider = value;

                if (null != activeTranscriptionProvider)
                {
                    activeTranscriptionProvider.OnFullTranscription.AddListener(
                        OnFullTranscription);
                    activeTranscriptionProvider.OnPartialTranscription.AddListener(
                        OnPartialTranscription);
                    activeTranscriptionProvider.OnMicLevelChanged.AddListener(
                        OnTranscriptionMicLevelChanged);
                    activeTranscriptionProvider.OnStartListening.AddListener(
                        OnStartListening);
                    activeTranscriptionProvider.OnStoppedListening.AddListener(
                        OnStoppedListening);
                }
            }
        }

        public override bool MicActive => null != micInput && micInput.IsRecording;

        protected override bool ShouldSendMicData => runtimeConfiguration.sendAudioToWit ||
                                                  null == activeTranscriptionProvider;

        private void Awake()
        {
            if (null == activeTranscriptionProvider &&
                runtimeConfiguration.customTranscriptionProvider)
            {
                TranscriptionProvider = runtimeConfiguration.customTranscriptionProvider;
            }

            micInput = GetComponent<IAudioInputSource>();
            if (micInput == null)
            {
                micInput = gameObject.AddComponent<Mic>();
            }

            dataReadyHandlers = GetComponents<IWitByteDataReadyHandler>();
            dataSentHandlers = GetComponents<IWitByteDataSentHandler>();
        }

        private void OnEnable()
        {

#if UNITY_EDITOR
            // Make sure we have a mic input after a script recompile
            if (null == micInput)
            {
                micInput = GetComponent<IAudioInputSource>();
            }
#endif

            micInput.OnSampleReady += OnSampleReady;
            micInput.OnStartRecording += OnStartListening;
            micInput.OnStopRecording += OnStoppedListening;

            InitializeConfig();
        }

        private void InitializeConfig()
        {
            if (runtimeConfiguration.alwaysRecord)
            {
                StartRecording();
            }
        }

        private IEnumerator WaitForMic()
        {
            yield return new WaitUntil(() => micInput.IsInputAvailable);
            micInitCoroutine = null;
            StartRecording();
        }

        private void OnDisable()
        {
            micInput.OnSampleReady -= OnSampleReady;
            micInput.OnStartRecording -= OnStartListening;
            micInput.OnStopRecording -= OnStoppedListening;
        }

        private void OnSampleReady(int sampleCount, float[] sample, float levelMax)
        {
            if (null == TranscriptionProvider || !TranscriptionProvider.OverrideMicLevel)
            {
                OnMicLevelChanged(levelMax);
            }

            if (null != micDataBuffer)
            {
                if (isSoundWakeActive && levelMax > runtimeConfiguration.soundWakeThreshold)
                {
                    lastSampleMarker = micDataBuffer.CreateMarker(
                        (int) (-runtimeConfiguration.micBufferLengthInSeconds * 1000 *
                               runtimeConfiguration.sampleLengthInMs));
                }

                byte[] data = Convert(sample);
                micDataBuffer.Push(data, 0, data.Length);
                if (data.Length > 0)
                {
                    events.OnByteDataReady?.Invoke(data, 0, data.Length);
                    for(int i = 0; null != dataReadyHandlers && i < dataReadyHandlers.Length; i++)
                    {
                        dataReadyHandlers[i].OnWitDataReady(data, 0, data.Length);
                    }
                }
#if DEBUG_SAMPLE
                    sampleFile.Write(data, 0, data.Length);
#endif
            }

            if (IsRequestActive && activeRequest.IsRequestStreamActive)
            {
                if (null != micDataBuffer && micDataBuffer.Capacity > 0)
                {
                    if (null == writeBuffer)
                    {
                        writeBuffer = new byte[sample.Length * 2];
                    }

                    // Flush the marker buffer to catch up
                    int read;
                    while ((read = lastSampleMarker.Read(writeBuffer, 0, writeBuffer.Length, true)) > 0)
                    {
                        activeRequest.Write(writeBuffer, 0, read);
                        events.OnByteDataSent?.Invoke(writeBuffer, 0, read);
                        for (int i = 0; null != dataSentHandlers && i < dataSentHandlers.Length; i++)
                        {
                            dataSentHandlers[i].OnWitDataSent(writeBuffer, 0, read);
                        }
                    }
                }
                else
                {
                    byte[] sampleBytes = Convert(sample);
                    activeRequest.Write(sampleBytes, 0, sampleBytes.Length);
                }


                if (receivedTranscription)
                {
                    if (Time.time - lastWordTime >
                        runtimeConfiguration.minTranscriptionKeepAliveTimeInSeconds)
                    {
                        Debug.Log("Deactivated due to inactivity. No new words detected.");
                        DeactivateRequest();
                        events.OnStoppedListeningDueToInactivity?.Invoke();
                    }
                }
                else if (Time.time - lastMinVolumeLevelTime >
                         runtimeConfiguration.minKeepAliveTimeInSeconds)
                {
                    Debug.Log("Deactivated input due to inactivity.");
                    DeactivateRequest();
                    events.OnStoppedListeningDueToInactivity?.Invoke();
                }
            }
            else if (isSoundWakeActive && levelMax > runtimeConfiguration.soundWakeThreshold)
            {
                events.OnMinimumWakeThresholdHit?.Invoke();
                isSoundWakeActive = false;
                ActivateImmediately(currentRequestOptions);
            }
        }

        private void OnFullTranscription(string transcription)
        {
            DeactivateRequest();
            events.OnFullTranscription?.Invoke(transcription);
            if (runtimeConfiguration.customTranscriptionProvider)
            {
                SendTranscription(transcription, new WitRequestOptions());
            }
        }

        private void OnPartialTranscription(string transcription)
        {
            receivedTranscription = true;
            lastWordTime = Time.time;
            events.OnPartialTranscription.Invoke(transcription);
        }

        private void OnTranscriptionMicLevelChanged(float level)
        {
            if (null != TranscriptionProvider && TranscriptionProvider.OverrideMicLevel)
            {
                OnMicLevelChanged(level);
            }
        }

        private void OnMicLevelChanged(float level)
        {
            if (level > runtimeConfiguration.minKeepAliveVolume)
            {
                lastMinVolumeLevelTime = Time.time;
                minKeepAliveWasHit = true;
            }

            events.OnMicLevelChanged?.Invoke(level);
        }

        private void OnStoppedListening()
        {
            events?.OnStoppedListening?.Invoke();
        }

        private void OnStartListening()
        {
            events?.OnStartListening?.Invoke();
        }

        private IEnumerator DeactivateDueToTimeLimit()
        {
            yield return new WaitForSeconds(runtimeConfiguration.maxRecordingTime);
            Debug.Log("Deactivated due to time limit.");
            DeactivateRequest();
            events.OnStoppedListeningDueToTimeout?.Invoke();
            timeLimitCoroutine = null;
        }

        /// <summary>
        /// Activate the microphone and send data to Wit for NLU processing.
        /// </summary>
        public override void Activate()
        {
            Activate(new WitRequestOptions());
        }

        /// <summary>
        /// Activate the microphone and send data to Wit for NLU processing.
        /// </summary>
        public override void Activate(WitRequestOptions requestOptions)
        {
            if (isActive) return;
            StopRecording();

            if (!micInput.IsRecording && ShouldSendMicData)
            {
                minKeepAliveWasHit = false;
                isSoundWakeActive = true;

#if DEBUG_SAMPLE
                var file = Application.dataPath + "/test.pcm";
                sampleFile = File.Open(file, FileMode.Create);
                Debug.Log("Writing recording to file: " + file);
#endif

                StartRecording();
            }

            if (!isActive)
            {
                activeTranscriptionProvider?.Activate();
                isActive = true;

                lastMinVolumeLevelTime = float.PositiveInfinity;
                currentRequestOptions = requestOptions;
            }
        }

        private void InitializeMicDataBuffer()
        {
            if (null == micDataBuffer && runtimeConfiguration.micBufferLengthInSeconds > 0)
            {
                micDataBuffer = new RingBuffer<byte>((int) Mathf.Ceil(2 *
                    runtimeConfiguration.micBufferLengthInSeconds * 1000 *
                    runtimeConfiguration.sampleLengthInMs));
                lastSampleMarker = micDataBuffer.CreateMarker();
            }
        }

        private void StopRecording()
        {
            if (null != micInitCoroutine)
            {
                StopCoroutine(micInitCoroutine);
                micInitCoroutine = null;
            }

            if (micInput.IsRecording && !runtimeConfiguration.alwaysRecord)
            {
                micInput.StopRecording();
                lastSampleMarker = null;

#if DEBUG_SAMPLE
                sampleFile.Close();
#endif
            }
        }

        private void StartRecording()
        {
            if (null != micInitCoroutine)
            {
                StopCoroutine(micInitCoroutine);
                micInitCoroutine = null;
            }

            if (!micInput.IsInputAvailable)
            {
                micInitCoroutine = StartCoroutine(WaitForMic());
            }

            if (micInput.IsInputAvailable)
            {
                Debug.Log("Starting recording.");
                micInput.StartRecording(sampleLen: runtimeConfiguration.sampleLengthInMs);
                InitializeMicDataBuffer();
            }
            else
            {
                events.OnError.Invoke("Input Error",
                    "No input source was available. Cannot activate for voice input.");
            }
        }

        public override void ActivateImmediately()
        {
            ActivateImmediately(new WitRequestOptions());
        }

        public override void ActivateImmediately(WitRequestOptions requestOptions)
        {
            // Make sure we aren't checking activation time until
            // the mic starts recording. If we're already recording for a live
            // recording, we just triggered an activation so we will reset the
            // last minvolumetime to ensure a minimum time from activation time
            lastMinVolumeLevelTime = float.PositiveInfinity;
            lastWordTime = float.PositiveInfinity;
            receivedTranscription = false;

            if (ShouldSendMicData)
            {
                activeRequest = RuntimeConfiguration.witConfiguration.SpeechRequest(requestOptions);
                activeRequest.audioEncoding = micInput.AudioEncoding;
                activeRequest.onPartialTranscription = OnPartialTranscription;
                activeRequest.onFullTranscription = OnFullTranscription;
                activeRequest.onInputStreamReady = r => OnWitReadyForData();
                activeRequest.onResponse = HandleResult;
                events.OnRequestCreated?.Invoke(activeRequest);
                activeRequest.Request();
                timeLimitCoroutine = StartCoroutine(DeactivateDueToTimeLimit());
            }

            if (!isActive)
            {
                if (runtimeConfiguration.alwaysRecord && null != micDataBuffer)
                {
                    lastSampleMarker = micDataBuffer.CreateMarker();
                }
                activeTranscriptionProvider?.Activate();
                isActive = true;
            }
        }

        private void OnWitReadyForData()
        {
            lastMinVolumeLevelTime = Time.time;
            if (!micInput.IsRecording && micInput.IsInputAvailable)
            {
                micInput.StartRecording(runtimeConfiguration.sampleLengthInMs);
            }
        }

        /// <summary>
        /// Stop listening and submit the collected microphone data to wit for processing.
        /// </summary>
        public override void Deactivate()
        {
            var recording = micInput.IsRecording;
            DeactivateRequest();

            if (recording)
            {
                events.OnStoppedListeningDueToDeactivation?.Invoke();
            }
        }

        /// <summary>
        /// Stop listening and abort any requests that may be active without waiting for a response.
        /// </summary>
        public override void DeactivateAndAbortRequest()
        {
            var recording = micInput.IsRecording;
            DeactivateRequest(true);
            if (recording)
            {
                events.OnStoppedListeningDueToDeactivation?.Invoke();
            }
        }

        private void DeactivateRequest(bool abort = false)
        {
            if (null != timeLimitCoroutine)
            {
                StopCoroutine(timeLimitCoroutine);
                timeLimitCoroutine = null;
            }

            StopRecording();

            micDataBuffer?.Clear();
            writeBuffer = null;
            minKeepAliveWasHit = false;

            activeTranscriptionProvider?.Deactivate();

            if (IsRequestActive)
            {
                if (abort)
                {
                    activeRequest.AbortRequest();
                }
                else
                {
                    activeRequest.CloseRequestStream();
                }

                if (minKeepAliveWasHit)
                {
                    events.OnMicDataSent?.Invoke();
                }
            }

            isActive = false;
        }

        private byte[] Convert(float[] samples)
        {
            var sampleCount = samples.Length;

            if (null == byteDataBuffer || byteDataBuffer.Length != sampleCount)
            {
                byteDataBuffer = new byte[sampleCount * 2];
            }

            int rescaleFactor = 32767; //to convert float to Int16

            for (int i = 0; i < sampleCount; i++)
            {
                short data = (short) (samples[i] * rescaleFactor);
                byteDataBuffer[i * 2] = (byte) data;
                byteDataBuffer[i * 2 + 1] = (byte) (data >> 8);
            }

            return byteDataBuffer;
        }

        /// <summary>
        /// Send text data to Wit.ai for NLU processing
        /// </summary>
        /// <param name="text"></param>
        /// <param name="requestOptions"></param>
        public override void Activate(string text, WitRequestOptions requestOptions)
        {
            if (Active) return;

            SendTranscription(text, requestOptions);
        }

        /// <summary>
        /// Send text data to Wit.ai for NLU processing
        /// </summary>
        /// <param name="text"></param>
        public override void Activate(string text)
        {
            Activate(text, new WitRequestOptions());
        }

        private void SendTranscription(string transcription, WitRequestOptions requestOptions)
        {
            isActive = true;
            activeRequest =
                RuntimeConfiguration.witConfiguration.MessageRequest(transcription, requestOptions);
            activeRequest.onResponse = HandleResult;
            events.OnRequestCreated?.Invoke(activeRequest);
            activeRequest.Request();
        }

        /// <summary>
        /// Main thread call to handle result callbacks
        /// </summary>
        /// <param name="request"></param>
        private void HandleResult(WitRequest request)
        {
            isActive = false;
            if (request.StatusCode == (int) HttpStatusCode.OK)
            {
                if (null != request.ResponseData)
                {
                    events?.OnResponse?.Invoke(request.ResponseData);
                }
                else
                {
                    events?.OnError?.Invoke("No Data", "No data was returned from the server.");
                }
            }
            else
            {
                DeactivateRequest();
                if (request.StatusCode != WitRequest.ERROR_CODE_ABORTED)
                {
                    events?.OnError?.Invoke("HTTP Error " + request.StatusCode,
                        request.StatusDescription);
                }
                else
                {
                    events?.OnAborted?.Invoke();
                }
            }

            events?.OnRequestCompleted?.Invoke();

            activeRequest = null;
        }
    }

    public interface IWitRuntimeConfigProvider
    {
        WitRuntimeConfiguration RuntimeConfiguration { get; }
    }
}
