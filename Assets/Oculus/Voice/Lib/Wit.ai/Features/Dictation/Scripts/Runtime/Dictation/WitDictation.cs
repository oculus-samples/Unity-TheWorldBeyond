/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Facebook.WitAi.Configuration;
using Facebook.WitAi.Data.Configuration;
using Facebook.WitAi.Dictation.Data;
using Facebook.WitAi.Events;
using Facebook.WitAi.Interfaces;
using Facebook.WitAi.Lib;
using UnityEngine;

namespace Facebook.WitAi.Dictation
{
    public class WitDictation : DictationService, IWitRuntimeConfigProvider, IVoiceEventProvider, IWitRequestProvider
    {
        [SerializeField] private WitRuntimeConfiguration witRuntimeConfiguration;

        private WitService witService;

        public WitRuntimeConfiguration RuntimeConfiguration
        {
            get => witRuntimeConfiguration;
            set => witRuntimeConfiguration = value;
        }

        #region Voice Service Properties

        public override bool Active => null != witService && witService.Active;
        public override bool IsRequestActive => null != witService && witService.IsRequestActive;

        public override ITranscriptionProvider TranscriptionProvider
        {
            get => witService.TranscriptionProvider;
            set => witService.TranscriptionProvider = value;

        }

        public override bool MicActive => null != witService && witService.MicActive;

        protected override bool ShouldSendMicData => witRuntimeConfiguration.sendAudioToWit ||
                                                     null == TranscriptionProvider;

        private readonly VoiceEvents voiceEvents = new VoiceEvents();
        public VoiceEvents VoiceEvents
        {
            get => voiceEvents;
        }

        #endregion

        #region IWitRequestProvider
        public WitRequest CreateWitRequest(WitConfiguration config, WitRequestOptions requestOptions,
            IDynamicEntitiesProvider[] additionalEntityProviders = null)
        {
            return config.DictationRequest(requestOptions);
        }

        #endregion

        #region Voice Service Methods

        public override void Activate()
        {
            witService.Activate();
        }

        public override void Activate(WitRequestOptions options)
        {
            witService.Activate(options);
        }

        public override void ActivateImmediately()
        {
            witService.ActivateImmediately();
        }

        public override void ActivateImmediately(WitRequestOptions options)
        {
            witService.ActivateImmediately(options);
        }

        public override void Deactivate()
        {
            witService.Deactivate();
        }

        public override void Cancel()
        {
            witService.DeactivateAndAbortRequest();
        }
        #endregion

        protected override void Awake()
        {
            base.Awake();
            witService = gameObject.AddComponent<WitService>();
            witService.VoiceEventProvider = this;
            witService.ConfigurationProvider = this;
            witService.WitRequestProvider = this;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            VoiceEvents.OnFullTranscription.AddListener(OnFullTranscription);
            VoiceEvents.OnPartialTranscription.AddListener(OnPartialTranscription);
            VoiceEvents.OnStartListening.AddListener(OnStartedListening);
            VoiceEvents.OnStoppedListening.AddListener(OnStoppedListening);
            VoiceEvents.OnMicLevelChanged.AddListener(OnMicLevelChanged);
            VoiceEvents.OnError.AddListener(OnError);
            VoiceEvents.OnResponse.AddListener(OnResponse);

        }

        protected override void OnDisable()
        {
            base.OnDisable();
            VoiceEvents.OnFullTranscription.RemoveListener(OnFullTranscription);
            VoiceEvents.OnPartialTranscription.RemoveListener(OnPartialTranscription);
            VoiceEvents.OnStartListening.RemoveListener(OnStartedListening);
            VoiceEvents.OnStoppedListening.RemoveListener(OnStoppedListening);
            VoiceEvents.OnMicLevelChanged.RemoveListener(OnMicLevelChanged);
            VoiceEvents.OnError.RemoveListener(OnError);
            VoiceEvents.OnResponse.RemoveListener(OnResponse);
        }
        private void OnFullTranscription(string transcription)
        {
            DictationEvents.OnFullTranscription?.Invoke(transcription);
        }

        private void OnPartialTranscription(string transcription)
        {
            DictationEvents.OnPartialTranscription?.Invoke(transcription);
        }

        private void OnStartedListening()
        {
            DictationEvents.onStart?.Invoke();
        }

        private void OnStoppedListening()
        {
            DictationEvents.onStopped?.Invoke();
        }

        private void OnMicLevelChanged(float level)
        {
            DictationEvents.onMicAudioLevel?.Invoke(level);
        }

        private void OnError(string error, string message)
        {
            DictationEvents.onError?.Invoke(error, message);
        }

        private void OnResponse(WitResponseNode response)
        {
            DictationEvents.onResponse?.Invoke(response);
        }
    }
}
