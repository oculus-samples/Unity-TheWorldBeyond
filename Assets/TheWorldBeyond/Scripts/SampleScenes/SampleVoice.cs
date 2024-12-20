// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Oculus.Voice;
using TMPro;
using UnityEngine;
using UnityEngine.Android;

namespace TheWorldBeyond.SampleScenes
{
    public class SampleVoice : MonoBehaviour
    {
        [SerializeField]
        private AppVoiceExperience m_voiceExperience;
        public TextMeshProUGUI ThoughtText;

        public GameObject[] DemoObjects;
        public GameObject NotifScreen;
        private const string DEFAULT_MESSAGE = "(press to begin transcribing)";

        private void OnEnable()
        {
            m_voiceExperience.VoiceEvents.OnStartListening.AddListener(StartListening);
            m_voiceExperience.VoiceEvents.OnStoppedListening.AddListener(StopListening);
            m_voiceExperience.VoiceEvents.OnPartialTranscription.AddListener(LiveTranscriptionHandler);

            ThoughtText.text = DEFAULT_MESSAGE;

            // ensure the experience is hidden by default, until user has agreed to the notif
            foreach (var obj in DemoObjects)
            {
                obj.SetActive(false);
            }
        }

        private void OnDisable()
        {
            m_voiceExperience.VoiceEvents.OnStartListening.RemoveListener(StartListening);
            m_voiceExperience.VoiceEvents.OnStoppedListening.RemoveListener(StopListening);
            m_voiceExperience.VoiceEvents.OnPartialTranscription.RemoveListener(LiveTranscriptionHandler);
        }

        public void BeginTranscription()
        {
            m_voiceExperience.Activate();
        }

        private void StartListening()
        {
            ThoughtText.text = "(begin talking)";
        }

        private void StopListening()
        {
            _ = StartCoroutine(ClearText(3));
        }

        private void LiveTranscriptionHandler(string content)
        {
            ThoughtText.text = content;
        }

        private IEnumerator ClearText(float countdown)
        {
            yield return new WaitForSeconds(countdown);
            ThoughtText.text = DEFAULT_MESSAGE;
        }

        // this is only called from the UI button
        public void CheckPermissionsAndContinue(GameObject notifScreen)
        {
            // user should have already seen permission screen upon initial run
            // pop it up again if permission's still not granted
            if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                StartExperience(Permission.Microphone);
            }
            else
            {
                // display the Android permission screen, with a callback
                // if user denies permission, our notif screen is still visible
                // if you want to provide more user feedback, add PermissionDenied callback here
                var callbacks = new PermissionCallbacks();
                callbacks.PermissionGranted += StartExperience;
                Permission.RequestUserPermission(Permission.Microphone, callbacks);
            }
        }

        private void StartExperience(string permissionName)
        {
            if (permissionName == Permission.Microphone)
            {
                foreach (var obj in DemoObjects)
                {
                    obj.SetActive(true);
                }

                if (NotifScreen)
                {
                    NotifScreen.SetActive(false);
                }
            }
        }
    }
}
