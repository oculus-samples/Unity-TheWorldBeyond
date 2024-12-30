// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace TheWorldBeyond.SamplePrefabs
{
    // replicate the system dialog's behavior (Gravity aligned, re-orient if out of view)
    public class SampleDialog : MonoBehaviour
    {
        private Vector3 m_currentFacingDirection = Vector3.forward;
        private Vector3 m_averageFacingDirection = Vector3.forward;

        private void Update()
        {
            var cam = Camera.main.transform;
            var currentLook = new Vector3(cam.forward.x, 0.0f, cam.forward.z).normalized;
            if (Vector3.Dot(m_currentFacingDirection, currentLook) < 0.5f)
            {
                m_currentFacingDirection = currentLook;
            }

            m_averageFacingDirection = Vector3.Slerp(m_averageFacingDirection, m_currentFacingDirection, 0.05f);
            transform.position = cam.position;
            transform.rotation = Quaternion.LookRotation(m_averageFacingDirection, Vector3.up);
        }

        public void QuitApp()
        {
            Application.Quit();
        }
    }
}
