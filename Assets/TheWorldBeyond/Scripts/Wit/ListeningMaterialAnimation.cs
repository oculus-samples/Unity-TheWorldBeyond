// Copyright (c) Meta Platforms, Inc. and affiliates.

using TheWorldBeyond.GameManagement;
using UnityEngine;

namespace TheWorldBeyond.Wit
{
    public class ListeningMaterialAnimation : MonoBehaviour
    {
        public float ScrollSpeed = -0.05F;
        public Color Color = Color.red;
        public float Intensity = 0.7f;
        private float m_intensity = 0.7f;
        private Renderer m_rend;

        private void Start()
        {
            m_rend = GetComponent<Renderer>();
            m_rend.material.color = Color;
        }

        private void Update()
        {
            // It might be good to only do this when in listening state
            var offset = Time.time * ScrollSpeed;
            m_rend.material.SetFloat("_ScrollAmount", offset);

            var objFwd = transform.position - WorldBeyondManager.Instance.MainCamera.transform.position;
            objFwd.y = 0;
            m_intensity = Mathf.Clamp(Intensity * objFwd.magnitude, 0.5f, 3);
            m_rend.material.SetFloat("_Intensity", m_intensity);
        }
    }
}
