/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using Facebook.WitAi;
using Facebook.WitAi.Configuration;
using Facebook.WitAi.Dictation;
using Facebook.WitAi.Dictation.Data;
using Facebook.WitAi.Interfaces;
using Facebook.WitAi.Lib;
using Oculus.Voice.Dictation.Bindings.Android;
using Oculus.VoiceSDK.Dictation.Utilities;
using Oculus.Voice.Core.Bindings.Android.PlatformLogger;
using Oculus.Voice.Core.Bindings.Interfaces;
using UnityEngine;

namespace Oculus.Voice.Dictation
{
    public class AppDictationExperience : DictationService
    {
        [SerializeField] private WitDictationRuntimeConfiguration runtimeConfiguration;
        [Tooltip("Uses platform dictation service instead of accessing wit directly from within the application.")]
        [SerializeField] private bool usePlatformServices;
        [Tooltip("Enables logs related to the interaction to be displayed on console")]
        [SerializeField] private bool enableConsoleLogging;

        public WitDictationRuntimeConfiguration RuntimeConfiguration
        {
            get => runtimeConfiguration;
            set => runtimeConfiguration = value;
        }

        private IDictationService _dictationServiceImpl;
        private IVoiceSDKLogger _voiceSDKLogger;

        public event Action OnInitialized;

#if UNITY_ANDROID && !UNITY_EDITOR
        private readonly string PACKAGE_VERSION = "44.0.0";
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        public bool HasPlatformIntegrations => usePlatformServices && _dictationServiceImpl is PlatformDictationImpl;
#else
        public bool HasPlatformIntegrations => false;
#endif

        public bool UsePlatformIntegrations
        {
            get => usePlatformServices;
            set
            {
                // If we're trying to turn on platform services and they're not currently active we
                // will forcably reinit and try to set the state.
                if (usePlatformServices != value || HasPlatformIntegrations != value)
                {
                    usePlatformServices = value;
#if UNITY_ANDROID && !UNITY_EDITOR
                    Debug.Log($"{(usePlatformServices ? "Enabling" : "Disabling")} platform integration.");
                    InitDictation();
#endif
                }
            }
        }

        private void InitDictation()
        {
            // Clean up if we're switching to native C# wit impl
            if (!UsePlatformIntegrations && _dictationServiceImpl is PlatformDictationImpl)
            {
                ((PlatformDictationImpl) _dictationServiceImpl).Disconnect();
            }
#if UNITY_ANDROID && !UNITY_EDITOR
            var loggerImpl = new VoiceSDKPlatformLoggerImpl();
            loggerImpl.Connect(PACKAGE_VERSION);
            _voiceSDKLogger = loggerImpl;
            if (UsePlatformIntegrations)
            {
                Debug.Log("Checking platform dictation capabilities...");
                var platformDictationImpl = new PlatformDictationImpl(this);
                platformDictationImpl.OnServiceNotAvailableEvent += RevertToWitDictation;
                platformDictationImpl.Connect(PACKAGE_VERSION);
                if (platformDictationImpl.PlatformSupportsDictation)
                {
                    _dictationServiceImpl = platformDictationImpl;
                    _dictationServiceImpl.DictationEvents = DictationEvents;
                    platformDictationImpl.SetDictationRuntimeConfiguration(RuntimeConfiguration);
                    Debug.Log("Dictation platform init complete");
                    _voiceSDKLogger.LogAnnotation("isUsingPlatformSupport", "true");
                    _voiceSDKLogger.IsUsingPlatformIntegration = true;
                }
                else
                {
                    Debug.Log("Platform dictation service unavailable. Falling back to WitDictation");
                    RevertToWitDictation();
                }
            }
            else
            {
                RevertToWitDictation();
            }
#else
            _voiceSDKLogger = new VoiceSDKConsoleLoggerImpl();
            RevertToWitDictation();
#endif
            _voiceSDKLogger.WitApplication = RuntimeConfiguration?.witConfiguration?.application?.id;
            _voiceSDKLogger.ShouldLogToConsole = enableConsoleLogging;

            OnInitialized?.Invoke();
        }

        private void RevertToWitDictation()
        {
            WitDictation witDictation = GetComponent<WitDictation>();
            if (null == witDictation)
            {
                witDictation = gameObject.AddComponent<WitDictation>();
                witDictation.hideFlags = HideFlags.HideInInspector;
            }

            witDictation.RuntimeConfiguration = this.RuntimeConfiguration;
            witDictation.DictationEvents = DictationEvents;
            _dictationServiceImpl = witDictation;
            Debug.Log("WitDictation init complete");
            _voiceSDKLogger.IsUsingPlatformIntegration = false;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (MicPermissionsManager.HasMicPermission())
            {
                InitDictation();
            }
            else
            {
                MicPermissionsManager.RequestMicPermission();
            }

            DictationEvents.onStart.AddListener(OnStarted);
            DictationEvents.onStopped.AddListener(OnStopped);
            DictationEvents.onResponse.AddListener(OnWitResponseListener);
            DictationEvents.onError.AddListener(OnError);
            DictationEvents.onDictationSessionStarted.AddListener(OnDictationSessionStarted);
        }

        protected override void OnDisable()
        {
#if UNITY_ANDROID
            if (_dictationServiceImpl is PlatformDictationImpl platformDictationImpl)
            {
                platformDictationImpl.Disconnect();
            }

            if (_voiceSDKLogger is VoiceSDKPlatformLoggerImpl loggerImpl)
            {
                loggerImpl.Disconnect();
            }
#endif
            _dictationServiceImpl = null;
            DictationEvents.onStart.RemoveListener(OnStarted);
            DictationEvents.onStopped.RemoveListener(OnStopped);
            DictationEvents.onResponse.RemoveListener(OnWitResponseListener);
            DictationEvents.onError.RemoveListener(OnError);
            DictationEvents.onDictationSessionStarted.RemoveListener(OnDictationSessionStarted);
            base.OnDisable();
        }

        #region DictationService properties

        public override bool Active => _dictationServiceImpl != null && _dictationServiceImpl.Active;
        public override bool IsRequestActive => _dictationServiceImpl != null && _dictationServiceImpl.IsRequestActive;

        public override ITranscriptionProvider TranscriptionProvider
        {
            get => _dictationServiceImpl.TranscriptionProvider;
            set => _dictationServiceImpl.TranscriptionProvider = value;

        }
        public override bool MicActive => null != _dictationServiceImpl && _dictationServiceImpl.MicActive;
        protected override bool ShouldSendMicData => RuntimeConfiguration.sendAudioToWit ||
                                                     null == TranscriptionProvider;
        #endregion

        #region DictationService APIs
        public override void Activate()
        {
            Activate(new WitRequestOptions());
        }

        public override void Activate(WitRequestOptions requestOptions)
        {
            _voiceSDKLogger.LogInteractionStart(requestOptions.requestID, "dictation");
            _dictationServiceImpl.Activate(requestOptions);
        }

        public override void ActivateImmediately()
        {
            ActivateImmediately(new WitRequestOptions());
        }

        public override void ActivateImmediately(WitRequestOptions requestOptions)
        {
            _voiceSDKLogger.LogInteractionStart(requestOptions.requestID, "dictation");
            _dictationServiceImpl.ActivateImmediately(requestOptions);
        }

        public override void Deactivate()
        {
            _dictationServiceImpl.Deactivate();
        }

        public override void Cancel()
        {
            _dictationServiceImpl.Deactivate();
        }
        #endregion

        #region Listeners for logging

        void OnWitResponseListener(WitResponseNode witResponseNode)
        {
            var tokens = witResponseNode?["speech"]?["tokens"];
            if (tokens != null)
            {
                int speechTokensLength = tokens.Count;
                string speechLength = witResponseNode["speech"]["tokens"][speechTokensLength - 1]?["end"]?.Value;
                _voiceSDKLogger.LogAnnotation("audioLength", speechLength);
            }


            _voiceSDKLogger.LogInteractionEndSuccess();
        }

        void OnError(string errorType, string errorMessage)
        {
            _voiceSDKLogger.LogInteractionEndFailure($"{errorType}:{errorMessage}");
        }

        void OnStarted()
        {
            _voiceSDKLogger.LogInteractionPoint("startedListening");
        }

        void OnStopped()
        {
            _voiceSDKLogger.LogInteractionPoint("stoppedListening");
            if (_voiceSDKLogger.IsUsingPlatformIntegration)
            {
                _voiceSDKLogger.LogInteractionEndSuccess();
            }
        }

        void OnDictationSessionStarted(DictationSession session)
        {
            if (session is PlatformDictationSession platformDictationSession)
            {
                _voiceSDKLogger.LogAnnotation("platformInteractionId", platformDictationSession.platformSessionId);
            }
        }

        #endregion
    }
}
