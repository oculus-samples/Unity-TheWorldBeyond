// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using UnityEngine;
using UnityEngine.Android;
using Oculus.Voice;
using TMPro;

public class SampleVoice : MonoBehaviour
{
    [SerializeField]
    private AppVoiceExperience _voiceExperience;
    public TextMeshProUGUI _thoughtText;

    public GameObject[] _demoObjects;
    public GameObject _notifScreen;

    const string _defaultMessage = "(press to begin transcribing)";

    private void OnEnable()
    {
        _voiceExperience.VoiceEvents.OnStartListening.AddListener(StartListening);
        _voiceExperience.VoiceEvents.OnStoppedListening.AddListener(StopListening);
        _voiceExperience.VoiceEvents.OnPartialTranscription.AddListener(LiveTranscriptionHandler);

        _thoughtText.text = _defaultMessage;

        // ensure the experience is hidden by default, until user has agreed to the notif
        foreach (GameObject obj in _demoObjects)
        {
            obj.SetActive(false);
        }
    }

    private void OnDisable()
    {
        _voiceExperience.VoiceEvents.OnStartListening.RemoveListener(StartListening);
        _voiceExperience.VoiceEvents.OnStoppedListening.RemoveListener(StopListening);
        _voiceExperience.VoiceEvents.OnPartialTranscription.RemoveListener(LiveTranscriptionHandler);
    }

    public void BeginTranscription()
    {
        _voiceExperience.Activate();
    }

    void StartListening()
    {
        _thoughtText.text = "(begin talking)";
    }

    void StopListening()
    {
        StartCoroutine(ClearText(3));
    }

    void LiveTranscriptionHandler(string content)
    {
        _thoughtText.text = content;
    }

    IEnumerator ClearText(float countdown)
    {
        yield return new WaitForSeconds(countdown);
        _thoughtText.text = _defaultMessage;
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

    void StartExperience(string permissionName)
    {
        if (permissionName == Permission.Microphone)
        {
            foreach (GameObject obj in _demoObjects)
            {
                obj.SetActive(true);
            }

            if (_notifScreen)
            {
                _notifScreen.SetActive(false);
            }
        }
    }
}
