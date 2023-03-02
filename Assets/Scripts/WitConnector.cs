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

using UnityEngine;
using UnityEngine.Events;
using Oculus.Voice;
using Facebook.WitAi.Lib;
using Facebook.WitAi;

#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif
public class WitConnector : MonoBehaviour
{
    static public WitConnector Instance = null;

    [SerializeField]
    private AppVoiceExperience _voiceExperience;

    [SerializeField]
    private VirtualPet pet;

    public bool currentFocus { private set; get; } = false;
    public UnityEvent<bool> FocusChangeEvt = new UnityEvent<bool>();
    private int focusCount = 0;
    private int focusBuffer = 30;

    private bool listeningTranscription = false;

    private void Awake()
    {
        if (!Instance)
        {
            Instance = this;
        }

    }
    private void OnEnable()
    {
        _voiceExperience.VoiceEvents.OnStartListening.AddListener(StartListening);
        _voiceExperience.VoiceEvents.OnStoppedListening.AddListener(StopListening);
        _voiceExperience.VoiceEvents.OnResponse.AddListener(WitResponseReceiver);
        _voiceExperience.VoiceEvents.OnError.AddListener(OnError);
        _voiceExperience.VoiceEvents.OnStoppedListeningDueToInactivity.AddListener(StoppedListeningDueToInactivity);
        _voiceExperience.VoiceEvents.OnStoppedListeningDueToTimeout.AddListener(StoppedListeningDueToTimeout);
        _voiceExperience.VoiceEvents.OnStoppedListeningDueToDeactivation.AddListener(StoppedListeningDueToDeactivation);
        _voiceExperience.VoiceEvents.OnPartialTranscription.AddListener(LiveTranscriptionHandler);

        FocusChangeEvt.AddListener(FocusHandler);
    }

    private void OnDisable()
    {
        _voiceExperience.VoiceEvents.OnStartListening.RemoveListener(StartListening);
        _voiceExperience.VoiceEvents.OnStoppedListening.RemoveListener(StopListening);
        _voiceExperience.VoiceEvents.OnResponse.RemoveListener(WitResponseReceiver);
        _voiceExperience.VoiceEvents.OnError.RemoveListener(OnError);
        _voiceExperience.VoiceEvents.OnStoppedListeningDueToInactivity.RemoveListener(StoppedListeningDueToInactivity);
        _voiceExperience.VoiceEvents.OnStoppedListeningDueToTimeout.RemoveListener(StoppedListeningDueToTimeout);
        _voiceExperience.VoiceEvents.OnStoppedListeningDueToDeactivation.RemoveListener(StoppedListeningDueToDeactivation);
        _voiceExperience.VoiceEvents.OnPartialTranscription.RemoveListener(LiveTranscriptionHandler);

        FocusChangeEvt.RemoveListener(FocusHandler);

    }
    void FixedUpdate()
    {
        bool focus = HasFocus();
        if (currentFocus != focus)
        {
            currentFocus = focus;
            FocusChangeEvt.Invoke(currentFocus);
        }
    }

#region GazeFocus
    bool HasFocus()
    {
        float oppyFov = 20;
        Vector3 targetDir = pet.transform.position - Camera.main.transform.position;
        targetDir.y = 0;
        float distance = targetDir.sqrMagnitude;
        float distanceThreshold = 0.6f;
        Vector3 forward = Camera.main.transform.forward;
        forward.y = 0;
        float angle = Vector3.Angle(targetDir, forward);
        if (angle < oppyFov || distance < distanceThreshold)
        {
            focusCount++;
        }
        else
        {
            focusCount = 0;
            return false;
        }
        if (focusCount >= focusBuffer)
        {
            focusCount = focusBuffer;
            return true;
        }
        return currentFocus;
    }

    void FocusHandler(bool isFocus) {
        WitSwitcher(isFocus && pet.CanListen());
    }
#endregion GazeFocus

#region Wit
    public bool WitSwitcher(bool isOn)
    {
        if (isOn) {
            _voiceExperience.Activate();
            return true;
        }

        if (!isOn)
        {
            _voiceExperience.Deactivate();
            return true;
        }
        return false;
    }

    void StartListening() {
        listeningTranscription = true;
        pet.Listening(true);
        pet.DisplayThought();
    }

    void StopListening()
    {
        listeningTranscription = false;
        pet.Listening(false);
        pet.HideThought();
    }

    void OnError(string error, string message)
    {
        Debug.LogWarning("Voice Error : " + message);
        ListenFailHandler();
    }
    void StoppedListeningDueToTimeout() {
        Debug.LogWarning("Voice Debug : StoppedListeningDueToTimeout");
    }
    void StoppedListeningDueToInactivity()
    {
        Debug.LogWarning("Voice Debug : StoppedListeningDueToInactivity");
    }
    void StoppedListeningDueToDeactivation() {
        Debug.LogWarning("Voice Debug : StoppedListeningDueToDeactivation");
    }

    void WitResponseReceiver(WitResponseNode response)
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
                    pet.VoiceCommandHandler(actionString);
                    return;
                    break;
            }
        }
    }
#endregion Wit
    void ListenFailHandler() {
        pet.ListenFail();
    }

    void LiveTranscriptionHandler(string content) {

        if (listeningTranscription) {
            pet.DisplayThought(content);
        }

    }

}
