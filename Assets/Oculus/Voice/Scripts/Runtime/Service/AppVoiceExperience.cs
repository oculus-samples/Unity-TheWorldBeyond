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
using Facebook.WitAi.Interfaces;
using Facebook.WitAi.Lib;
using Oculus.Voice.Bindings.Android;
using Oculus.Voice.Core.Bindings.Android.PlatformLogger;
using Oculus.Voice.Core.Bindings.Interfaces;
using Oculus.Voice.Interfaces;
using Oculus.VoiceSDK.Utilities;
using UnityEngine;

namespace Oculus.Voice
{
    [HelpURL("https://developer.oculus.com/experimental/voice-sdk/tutorial-overview/")]
    public class AppVoiceExperience : VoiceService, IWitRuntimeConfigProvider
    {
        [SerializeField] private WitRuntimeConfiguration witRuntimeConfiguration;
        [Tooltip("Uses platform services to access wit.ai instead of accessing wit directly from within the application.")]
        [SerializeField] private bool usePlatformServices;

        [Tooltip("Enables logs related to the interaction to be displayed on console")]
        [SerializeField] private bool enableConsoleLogging;

        public WitRuntimeConfiguration RuntimeConfiguration
        {
            get => witRuntimeConfiguration;
            set => witRuntimeConfiguration = value;
        }

        private IPlatformVoiceService platformService;
        private IVoiceService voiceServiceImpl;
        private IVoiceSDKLogger voiceSDKLoggerImpl;
#if UNITY_ANDROID && !UNITY_EDITOR
        private readonly string PACKAGE_VERSION = "44.0.1";
#endif

        private bool Initialized => null != voiceServiceImpl;

        public event Action OnInitialized;

        #region Voice Service Properties
        public override bool Active => null != voiceServiceImpl && voiceServiceImpl.Active;
        public override bool IsRequestActive => null != voiceServiceImpl && voiceServiceImpl.IsRequestActive;
        public override ITranscriptionProvider TranscriptionProvider
        {
            get => voiceServiceImpl.TranscriptionProvider;
            set => voiceServiceImpl.TranscriptionProvider = value;

        }
        public override bool MicActive => null != voiceServiceImpl && voiceServiceImpl.MicActive;
        protected override bool ShouldSendMicData => witRuntimeConfiguration.sendAudioToWit ||
                                                  null == TranscriptionProvider;
        #endregion

        #if UNITY_ANDROID && !UNITY_EDITOR
        public bool HasPlatformIntegrations => usePlatformServices && voiceServiceImpl is VoiceSDKImpl;
        #else
        public bool HasPlatformIntegrations => false;
        #endif

        public bool EnableConsoleLogging => enableConsoleLogging;

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
                    InitVoiceSDK();
#endif
                }
            }
        }

        #region Voice Service Methods

        public override void Activate()
        {
            Activate(new WitRequestOptions());
        }

        public override void Activate(WitRequestOptions options)
        {
            voiceSDKLoggerImpl.LogInteractionStart(options.requestID, "speech");
            voiceServiceImpl.Activate(options);
        }

        public override void ActivateImmediately()
        {
            ActivateImmediately(new WitRequestOptions());
        }

        public override void ActivateImmediately(WitRequestOptions options)
        {
            voiceSDKLoggerImpl.LogInteractionStart(options.requestID, "speech");
            voiceServiceImpl.ActivateImmediately(options);
        }

        public override void Deactivate()
        {
            voiceServiceImpl.Deactivate();
        }

        public override void DeactivateAndAbortRequest()
        {
            voiceServiceImpl.DeactivateAndAbortRequest();
        }

        public override void Activate(string text)
        {
            Activate(text, new WitRequestOptions());
        }

        public override void Activate(string text, WitRequestOptions requestOptions)
        {
            voiceSDKLoggerImpl.LogInteractionStart(requestOptions.requestID, "message");
            voiceServiceImpl.Activate(text, requestOptions);
        }

        #endregion

        private void InitVoiceSDK()
        {
            // Clean up if we're switching to native C# wit impl
            if (!UsePlatformIntegrations && voiceServiceImpl is VoiceSDKImpl)
            {
                ((VoiceSDKImpl) voiceServiceImpl).Disconnect();
            }
#if UNITY_ANDROID && !UNITY_EDITOR
            var loggerImpl = new VoiceSDKPlatformLoggerImpl();
            loggerImpl.Connect(PACKAGE_VERSION);
            voiceSDKLoggerImpl = loggerImpl;
            if (UsePlatformIntegrations)
            {
                Debug.Log("Checking platform capabilities...");
                var platformImpl = new VoiceSDKImpl(this);
                platformImpl.OnServiceNotAvailableEvent += () => RevertToWitUnity();
                platformImpl.Connect(PACKAGE_VERSION);
                platformImpl.SetRuntimeConfiguration(RuntimeConfiguration);
                if (platformImpl.PlatformSupportsWit)
                {
                    voiceServiceImpl = platformImpl;

                    if (voiceServiceImpl is Wit wit)
                    {
                        wit.RuntimeConfiguration = witRuntimeConfiguration;
                    }

                    voiceServiceImpl.VoiceEvents = VoiceEvents;
                    voiceSDKLoggerImpl.LogAnnotation("isUsingPlatformSupport", "true");
                    voiceSDKLoggerImpl.IsUsingPlatformIntegration = true;
                }
                else
                {
                    Debug.Log("Platform registration indicated platform support is not currently available.");
                    RevertToWitUnity();
                }
            }
            else
            {
                RevertToWitUnity();
            }
#else
            voiceSDKLoggerImpl = new VoiceSDKConsoleLoggerImpl();
            RevertToWitUnity();
#endif
            voiceSDKLoggerImpl.WitApplication = RuntimeConfiguration?.witConfiguration?.application?.id;
            voiceSDKLoggerImpl.ShouldLogToConsole = EnableConsoleLogging;

            OnInitialized?.Invoke();
        }

        private void RevertToWitUnity()
        {
            Wit w = GetComponent<Wit>();
            if (null == w)
            {
                w = gameObject.AddComponent<Wit>();
                w.hideFlags = HideFlags.HideInInspector;
            }
            voiceServiceImpl = w;

            if (voiceServiceImpl is Wit wit)
            {
                wit.RuntimeConfiguration = witRuntimeConfiguration;
            }

            voiceServiceImpl.VoiceEvents = VoiceEvents;
            voiceSDKLoggerImpl.IsUsingPlatformIntegration = false;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (MicPermissionsManager.HasMicPermission())
            {
                InitVoiceSDK();
            }
            else
            {
                MicPermissionsManager.RequestMicPermission();
            }

            #if UNITY_ANDROID && !UNITY_EDITOR
            platformService?.SetRuntimeConfiguration(witRuntimeConfiguration);
            #endif

            // Logging
            VoiceEvents.OnResponse?.AddListener(OnWitResponseListener);
            VoiceEvents.OnAborted?.AddListener(OnAborted);
            VoiceEvents.OnError?.AddListener(OnError);
            VoiceEvents.OnStartListening?.AddListener(OnStartedListening);
            VoiceEvents.OnStoppedListening?.AddListener(OnStoppedListening);
            VoiceEvents.OnMicDataSent?.AddListener(OnMicDataSent);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            #if UNITY_ANDROID
            if (voiceServiceImpl is VoiceSDKImpl platformImpl)
            {
                platformImpl.Disconnect();
            }

            if (voiceSDKLoggerImpl is VoiceSDKPlatformLoggerImpl loggerImpl)
            {
                loggerImpl.Disconnect();
            }
            #endif
            voiceServiceImpl = null;

            // Logging
            VoiceEvents.OnResponse?.RemoveListener(OnWitResponseListener);
            VoiceEvents.OnAborted?.RemoveListener(OnAborted);
            VoiceEvents.OnError?.RemoveListener(OnError);
            VoiceEvents.OnStartListening?.RemoveListener(OnStartedListening);
            VoiceEvents.OnStoppedListening?.RemoveListener(OnStoppedListening);
            VoiceEvents.OnMicDataSent?.RemoveListener(OnMicDataSent);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && !Initialized)
            {
                if (MicPermissionsManager.HasMicPermission())
                {
                    InitVoiceSDK();
                }
            }
        }

        #region Event listerns for logging

        void OnWitResponseListener(WitResponseNode witResponseNode)
        {
            var tokens = witResponseNode?["speech"]?["tokens"];
            if (tokens != null)
            {
                int speechTokensLength = tokens.Count;
                string speechLength = witResponseNode["speech"]["tokens"][speechTokensLength - 1]?["end"]?.Value;
                voiceSDKLoggerImpl.LogAnnotation("audioLength", speechLength);
            }

            voiceSDKLoggerImpl.LogInteractionEndSuccess();
        }

        void OnAborted()
        {
            voiceSDKLoggerImpl.LogInteractionEndFailure("aborted");
        }

        void OnError(string errorType, string errorMessage)
        {
            voiceSDKLoggerImpl.LogInteractionEndFailure($"{errorType}:{errorMessage}");
        }

        void OnStartedListening()
        {
            voiceSDKLoggerImpl.LogInteractionPoint("startedListening");
        }

        void OnStoppedListening()
        {
            voiceSDKLoggerImpl.LogInteractionPoint("stoppedListening");
        }

        void OnMicDataSent()
        {
            voiceSDKLoggerImpl.LogInteractionPoint("micDataSent");
        }
        #endregion
    }
}
