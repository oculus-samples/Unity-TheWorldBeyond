// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.WitAi;
using Meta.WitAi.Json;
using Oculus.Voice;
using TheWorldBeyond.Character.Oppy;
using UnityEngine;
using UnityEngine.Events;

#if PLATFORM_ANDROID
#endif

namespace TheWorldBeyond.Wit
{
    public class WitConnector : MonoBehaviour
    {
        public static WitConnector Instance = null;

        [SerializeField]
        private AppVoiceExperience m_voiceExperience;

        [SerializeField]
        private VirtualPet m_pet;

        public bool CurrentFocus { private set; get; } = false;
        public UnityEvent<bool> FocusChangeEvt = new();
        private int m_focusCount = 0;
        private int m_focusBuffer = 30;

        private bool m_listeningTranscription = false;

        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
            }

        }
        private void OnEnable()
        {
            m_voiceExperience.VoiceEvents.OnStartListening.AddListener(StartListening);
            m_voiceExperience.VoiceEvents.OnStoppedListening.AddListener(StopListening);
            m_voiceExperience.VoiceEvents.OnResponse.AddListener(WitResponseReceiver);
            m_voiceExperience.VoiceEvents.OnError.AddListener(OnError);
            m_voiceExperience.VoiceEvents.OnStoppedListeningDueToInactivity.AddListener(StoppedListeningDueToInactivity);
            m_voiceExperience.VoiceEvents.OnStoppedListeningDueToTimeout.AddListener(StoppedListeningDueToTimeout);
            m_voiceExperience.VoiceEvents.OnStoppedListeningDueToDeactivation.AddListener(StoppedListeningDueToDeactivation);
            m_voiceExperience.VoiceEvents.OnPartialTranscription.AddListener(LiveTranscriptionHandler);

            FocusChangeEvt.AddListener(FocusHandler);
        }

        private void OnDisable()
        {
            m_voiceExperience.VoiceEvents.OnStartListening.RemoveListener(StartListening);
            m_voiceExperience.VoiceEvents.OnStoppedListening.RemoveListener(StopListening);
            m_voiceExperience.VoiceEvents.OnResponse.RemoveListener(WitResponseReceiver);
            m_voiceExperience.VoiceEvents.OnError.RemoveListener(OnError);
            m_voiceExperience.VoiceEvents.OnStoppedListeningDueToInactivity.RemoveListener(StoppedListeningDueToInactivity);
            m_voiceExperience.VoiceEvents.OnStoppedListeningDueToTimeout.RemoveListener(StoppedListeningDueToTimeout);
            m_voiceExperience.VoiceEvents.OnStoppedListeningDueToDeactivation.RemoveListener(StoppedListeningDueToDeactivation);
            m_voiceExperience.VoiceEvents.OnPartialTranscription.RemoveListener(LiveTranscriptionHandler);

            FocusChangeEvt.RemoveListener(FocusHandler);

        }

        private void FixedUpdate()
        {
            var focus = HasFocus();
            if (CurrentFocus != focus)
            {
                CurrentFocus = focus;
                FocusChangeEvt.Invoke(CurrentFocus);
            }
        }

        #region GazeFocus
        private bool HasFocus()
        {
            float oppyFov = 20;
            var targetDir = m_pet.transform.position - Camera.main.transform.position;
            targetDir.y = 0;
            var distance = targetDir.sqrMagnitude;
            var distanceThreshold = 0.6f;
            var forward = Camera.main.transform.forward;
            forward.y = 0;
            var angle = Vector3.Angle(targetDir, forward);
            if (angle < oppyFov || distance < distanceThreshold)
            {
                m_focusCount++;
            }
            else
            {
                m_focusCount = 0;
                return false;
            }
            if (m_focusCount >= m_focusBuffer)
            {
                m_focusCount = m_focusBuffer;
                return true;
            }
            return CurrentFocus;
        }

        private void FocusHandler(bool isFocus)
        {
            _ = WitSwitcher(isFocus && m_pet.CanListen());
        }
        #endregion GazeFocus

        #region Wit
        public bool WitSwitcher(bool isOn)
        {
            if (isOn)
            {
                m_voiceExperience.Activate();
                return true;
            }

            if (!isOn)
            {
                m_voiceExperience.Deactivate();
                return true;
            }
            return false;
        }

        private void StartListening()
        {
            m_listeningTranscription = true;
            m_pet.Listening(true);
            m_pet.DisplayThought();
        }

        private void StopListening()
        {
            m_listeningTranscription = false;
            m_pet.Listening(false);
            m_pet.HideThought();
        }

        private void OnError(string error, string message)
        {
            Debug.LogWarning("Voice Error : " + message);
            ListenFailHandler();
        }

        private void StoppedListeningDueToTimeout()
        {
            Debug.LogWarning("Voice Debug : StoppedListeningDueToTimeout");
        }

        private void StoppedListeningDueToInactivity()
        {
            Debug.LogWarning("Voice Debug : StoppedListeningDueToInactivity");
        }

        private void StoppedListeningDueToDeactivation()
        {
            Debug.LogWarning("Voice Debug : StoppedListeningDueToDeactivation");
        }

        private void WitResponseReceiver(WitResponseNode response)
        {
            var intent = WitResultUtilities.GetIntentName(response);
            if (intent == "change_oz_animation")
            {
                var actionString = WitResultUtilities.GetFirstEntityValue(response, "oz_action:oz_action");
                switch (actionString)
                {
                    case "come":
                    case "jump":
                    case "hi":
                        m_pet.VoiceCommandHandler(actionString);
                        return;
                }
            }
        }
        #endregion Wit
        private void ListenFailHandler()
        {
            m_pet.ListenFail();
        }

        private void LiveTranscriptionHandler(string content)
        {

            if (m_listeningTranscription)
            {
                m_pet.DisplayThought(content);
            }

        }

    }
}
