// Copyright (c) Meta Platforms, Inc. and affiliates.

using TheWorldBeyond.GameManagement;
using TMPro;
using UnityEngine;

namespace TheWorldBeyond.Title
{
    public class IntroScreen : MonoBehaviour
    {
        public TextMeshProUGUI DebugText;

        private void Start()
        {
            var buildInfo = Application.version;
            buildInfo += WorldBeyondManager.Instance.IsGreyPassthrough() ? " for Quest" : " in color";
            DebugText.text = buildInfo;
            Debug.Log("TheWorldBeyond: running version " + Application.version + " on " + OVRPlugin.GetSystemHeadsetType());
        }

        private void Update()
        {
            // only show the debug text if a thumbstick is pressed
            var thumbstickPressed = OVRInput.Get(OVRInput.RawButton.RThumbstick) || OVRInput.Get(OVRInput.RawButton.LThumbstick);
            DebugText.enabled = thumbstickPressed;
        }
    }
}
