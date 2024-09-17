// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using TMPro;

public class IntroScreen : MonoBehaviour
{
    public TextMeshProUGUI debugText;

    private void Start()
    {
        string buildInfo = Application.version;
        buildInfo += WorldBeyondManager.Instance.IsGreyPassthrough() ? " for Quest" : " in color";
        debugText.text = buildInfo;
        Debug.Log("TheWorldBeyond: running version " + Application.version + " on " + OVRPlugin.GetSystemHeadsetType());
    }

    private void Update()
    {
        // only show the debug text if a thumbstick is pressed
        bool thumbstickPressed = OVRInput.Get(OVRInput.RawButton.RThumbstick) || OVRInput.Get(OVRInput.RawButton.LThumbstick);
        debugText.enabled = thumbstickPressed;
    }
}
