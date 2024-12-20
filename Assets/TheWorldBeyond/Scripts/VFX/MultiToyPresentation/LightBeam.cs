// Copyright (c) Meta Platforms, Inc. and affiliates.

using TheWorldBeyond.Audio;
using TheWorldBeyond.Environment.RoomEnvironment;
using TheWorldBeyond.GameManagement;
using TheWorldBeyond.Toy;
using UnityEngine;

namespace TheWorldBeyond.VFX
{
    public class LightBeam : MonoBehaviour
    {
        public Light BeamLight;
        public Transform BeamCore;
        public Transform BeamBase;
        public ParticleSystem BeamParticles;

        // player has looked at hole, so play animation
        private bool m_observed = false;
        private float m_viewCone = 0.95f;
        private float m_observedTimer = 0.0f;
        private const float ANIM_TIME = 6.0f;
        private Vector3 m_floatingToyPosition = Vector3.up;
        private Vector3 m_hiddenToyPosition = Vector3.zero;
        private Vector3 m_beamCoreStartScale = Vector3.one;
        private Vector3 m_beamBaseStartScale = Vector3.one;

        [Header("Audio")]
        public SoundEntry BeamIntro;
        public SoundEntry BeamLoop;
        public SoundEntry BeamOutro;
        public float BeamLoopVolume_Small = 0.3f;
        public float BeamLoopPitch_Small = 2f;

        private void Update()
        {
            if (m_observed)
            {
                m_observedTimer += Time.deltaTime;
                var normTime = Mathf.Clamp01(m_observedTimer / ANIM_TIME);

                var finalCoreScale = new Vector3(1.0f, VirtualRoom.Instance.GetCeilingHeight(), 1.0f);
                var finalBaseScale = Vector3.one;

                // aperture opens just in the beginning
                var beamTimer = Mathf.Cos(Mathf.Clamp01(normTime * 5) * Mathf.PI) * 0.5f + 0.5f;
                beamTimer = 1 - beamTimer;
                BeamCore.localScale = Vector3.Lerp(m_beamCoreStartScale, finalCoreScale, beamTimer);
                var baseScale = Vector3.Lerp(m_beamBaseStartScale, finalBaseScale, beamTimer);
                BeamBase.localScale = baseScale;

                // toy elevates as aperture finishes opening
                var toyTimer = Mathf.Cos(Mathf.Clamp01((normTime - 0.25f) / 0.75f) * Mathf.PI) * 0.5f + 0.5f;
                toyTimer = 1 - toyTimer;
                var toyPosition = Vector3.Lerp(m_hiddenToyPosition, m_floatingToyPosition, toyTimer);
                MultiToy.Instance.gameObject.SetActive(toyTimer > 0.01f);
                var bobbleTimer = Mathf.Clamp01((normTime - 0.9f) / 0.1f);
                MultiToy.Instance.transform.position = toyPosition + Mathf.Sin(Time.time * 2) * Vector3.up * 0.05f * bobbleTimer;
                MultiToy.Instance.transform.Rotate(Vector3.up * 0.5f, Space.World);

                // apply "lit room" effect to Scene objects
                VirtualRoom.Instance.SetEffectPosition(MultiToy.Instance.transform.position, toyTimer);
            }
            else
            {
                var lookAt = (BeamBase.position - WorldBeyondManager.Instance.MainCamera.transform.position).normalized;
                if (Vector3.Dot(lookAt, WorldBeyondManager.Instance.MainCamera.transform.forward) >= m_viewCone)
                {
                    m_observed = true;
                    BeamParticles.Play();
                    BeamLoop.ResetPitch();
                    BeamLoop.ResetVolume();
                    BeamIntro.Play();
                }
                // enlarge the view cone requirement over time, in case player isn't looking directly at beam
                m_viewCone -= Time.deltaTime * 0.05f;
                m_viewCone = Mathf.Clamp01(m_viewCone);
            }
        }

        /// <summary>
        /// Position the light beam, prepare it for opening when the player looks at it.
        /// </summary>
        public void Prepare(Vector3 toyPos)
        {
            m_floatingToyPosition = toyPos;

            transform.position = new Vector3(toyPos.x, WorldBeyondManager.Instance.GetFloorHeight(), toyPos.z);
            m_hiddenToyPosition = transform.position - Vector3.up * 0.3f;

            BeamLight.gameObject.SetActive(true);
            m_observed = false;
            m_observedTimer = 0.0f;
            m_viewCone = 0.95f;

            var baseDiameter = 0.1f;
            m_beamCoreStartScale = new Vector3(baseDiameter, baseDiameter, baseDiameter);
            BeamCore.localScale = m_beamCoreStartScale;
            BeamCore.transform.localPosition = Vector3.zero;

            m_beamBaseStartScale = new Vector3(baseDiameter, 1.0f, baseDiameter);
            BeamBase.localScale = m_beamBaseStartScale;

            MultiToy.Instance.transform.position = m_hiddenToyPosition;
            MultiToy.Instance.transform.rotation = Quaternion.identity;
            MultiToy.Instance.gameObject.SetActive(false);

            BeamLoop.Play();
            BeamLoop.SetVolume(BeamLoopVolume_Small);
            BeamLoop.SetPitch(2f);
        }

        public void CloseBeam()
        {
            gameObject.SetActive(false);
        }
    }
}
