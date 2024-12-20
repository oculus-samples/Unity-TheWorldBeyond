// Copyright (c) Meta Platforms, Inc. and affiliates.

using TheWorldBeyond.GameManagement;
using TMPro;
using UnityEngine;

namespace TheWorldBeyond.VFX
{
    public class ThoughtBubble : MonoBehaviour
    {
        public TextMeshProUGUI ThoughtText;
        public Transform MainDisplay;
        public Transform Bubble1;
        public Transform Bubble2;
        private Vector3 m_b2Offset = Vector3.zero;
        private float m_countdownTimer = 0.0f;
        public Transform OppyHeadBone;
        public float ScaleMultipier = 0.7f;

        private void Awake()
        {
            ThoughtText.fontMaterial.renderQueue = 4501;
            m_b2Offset = Bubble2.localPosition;
        }

        private void Update()
        {
            ForceSizeUpdate();

            // animate things in code
            Bubble1.transform.localRotation = Quaternion.Euler(0, 0, Time.time * 10);
            Bubble2.transform.localRotation = Quaternion.Euler(0, 0, -Time.time * 15);

            Bubble1.transform.localPosition = Vector3.right * Mathf.Sin(Time.time * 1.6f) * 0.005f;
            Bubble2.transform.localPosition = m_b2Offset + Vector3.right * Mathf.Cos(Time.time * 1.5f) * 0.01f;

            MainDisplay.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time) * 5);

            m_countdownTimer -= Time.deltaTime;
            if (m_countdownTimer <= 0.0f)
            {
                gameObject.SetActive(false);
            }
        }

        public void ForceSizeUpdate()
        {
            if (OppyHeadBone)
            {
                transform.position = OppyHeadBone.position - OppyHeadBone.right * 0.3f;
            }

            var objFwd = transform.position - WorldBeyondManager.Instance.MainCamera.transform.position;

            // face camera
            transform.rotation = Quaternion.LookRotation(objFwd, Vector3.up);

            // keep uniform size on screen
            objFwd.y = 0;
            transform.localScale = Vector3.one * Mathf.Clamp(objFwd.magnitude * ScaleMultipier, 0.8f, 4.0f);
        }

        public void UpdateText(string message, float thoughtDuration = 2)
        {
            m_countdownTimer = thoughtDuration;
            ThoughtText.text = message;
        }

        public void ShowHint(float hintDuration = 5)
        {
            m_countdownTimer = hintDuration;
            ThoughtText.text = "<color=#000000>Ask me to <color=#FF0000>come<color=#000000>, <color=#FF0000>jump <color=#000000>or<br> say <color=#FF0000>hi<color=#000000> to me";
        }
    }
}
