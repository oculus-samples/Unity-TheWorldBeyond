/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Facebook.WitAi.Configuration;
using Facebook.WitAi.Dictation.Events;
using Facebook.WitAi.Events.UnityEventListeners;
using Facebook.WitAi.Interfaces;
using UnityEngine;

namespace Facebook.WitAi.Dictation
{
    public abstract class DictationService : MonoBehaviour, IDictationService, IAudioEventProvider, ITranscriptionEventProvider
    {
        [Tooltip("Events that will fire before, during and after an activation")]
        [SerializeField] public DictationEvents dictationEvents = new DictationEvents();

        /// <summary>
        /// Returns true if this voice service is currently active and listening with the mic
        /// </summary>
        public abstract bool Active { get; }

        /// <summary>
        /// Returns true if the service is actively communicating with Wit.ai during an Activation. The mic may or may not still be active while this is true.
        /// </summary>
        public abstract bool IsRequestActive { get; }

        /// <summary>
        /// Gets/Sets a custom transcription provider. This can be used to replace any built in asr
        /// with an on device model or other provided source
        /// </summary>
        public abstract ITranscriptionProvider TranscriptionProvider { get; set; }

        /// <summary>
        /// Returns true if this voice service is currently reading data from the microphone
        /// </summary>
        public abstract bool MicActive { get; }

        public DictationEvents DictationEvents
        {
            get => dictationEvents;
            set => dictationEvents = value;
        }

        /// <summary>
        /// A subset of events around collection of audio data
        /// </summary>
        public IAudioInputEvents AudioEvents => DictationEvents;

        /// <summary>
        /// A subset of events around receiving transcriptions
        /// </summary>
        public ITranscriptionEvent TranscriptionEvents => DictationEvents;

        /// <summary>
        /// Returns true if the audio input should be read in an activation
        /// </summary>
        protected abstract bool ShouldSendMicData { get; }

        /// <summary>
        /// Start listening for sound or speech from the user and start sending data to Wit.ai once sound or speech has been detected.
        /// </summary>
        public abstract void Activate();

        /// <summary>
        /// Activate the microphone and send data for NLU processing. Includes optional additional request parameters like dynamic entities and maximum results.
        /// </summary>
        /// <param name="requestOptions"></param>
        public abstract void Activate(WitRequestOptions requestOptions);

        /// <summary>
        /// Activate the microphone and send data for NLU processing immediately without waiting for sound/speech from the user to begin.
        /// </summary>
        public abstract void ActivateImmediately();

        /// <summary>
        /// Activate the microphone and send data for NLU processing immediately without waiting for sound/speech from the user to begin.  Includes optional additional request parameters like dynamic entities and maximum results.
        /// </summary>
        public abstract void ActivateImmediately(WitRequestOptions requestOptions);

        /// <summary>
        /// Stop listening and submit any remaining buffered microphone data for processing.
        /// </summary>
        public abstract void Deactivate();

        /// <summary>
        /// Cancels the current transcription. No FullTranscription event will fire.
        /// </summary>
        public abstract void Cancel();

        protected virtual void Awake()
        {
            var audioEventListener = GetComponent<AudioEventListener>();
            if (!audioEventListener)
            {
                gameObject.AddComponent<AudioEventListener>();
            }

            var transcriptionEventListener = GetComponent<TranscriptionEventListener>();
            if (!transcriptionEventListener)
            {
                gameObject.AddComponent<TranscriptionEventListener>();
            }
        }

        protected virtual void OnEnable()
        {
        }

        protected virtual void OnDisable()
        {
        }
    }

    public interface IDictationService
    {
        bool Active { get; }

        bool IsRequestActive { get; }

        bool MicActive { get; }

        ITranscriptionProvider TranscriptionProvider { get; set; }

        DictationEvents DictationEvents { get; set; }

        void Activate();

        void Activate(WitRequestOptions requestOptions);

        void ActivateImmediately();

        void ActivateImmediately(WitRequestOptions requestOptions);

        void Deactivate();
    }
}
