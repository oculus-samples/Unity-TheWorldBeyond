// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using TheWorldBeyond.GameManagement;
using UnityEngine;

namespace TheWorldBeyond.Toy
{
    public class VirtualFlashlight : WorldBeyondToy
    {
        public GameObject Flashlight;
        public GameObject LightVolume;
        private List<MeshRenderer> m_lightQuads = new();
        public AnimationCurve FlickerStrength;
        public MeshRenderer MagicEffectCone;
        public Light Spotlight;
        private float m_spotlightBaseIntensity = 1.0f;
        private float m_effectTimer = 0.0f;
        private float m_effectAccel = 0.0f;

        [HideInInspector]
        public bool AbsorbingBall = false;
        public AudioSource BallAbsorbAudioSource;

        public Transform LightBone;

        private void Start()
        {
            var refQuad = LightVolume.transform.GetChild(0).gameObject;
            var sliceCount = 4;
            var basePos = 0.3f;
            var baseScale = 0.4f;
            for (var i = 0; i < sliceCount; i++)
            {
                var newQuad = refQuad;
                if (i > 0)
                {
                    newQuad = Instantiate(refQuad, LightVolume.transform);
                }
                var normPos = i / (float)(sliceCount - 1);
                var dist = Mathf.Pow(normPos, 1.5f);
                newQuad.transform.localPosition = Vector3.up * (basePos + dist) * 0.6f;
                newQuad.transform.localScale = Vector3.one * (baseScale + dist * 1.5f);
                var nqm = newQuad.GetComponent<MeshRenderer>();

                m_lightQuads.Add(nqm);
            }

            Flashlight.SetActive(false);
            if (Spotlight) m_spotlightBaseIntensity = Spotlight.intensity;
        }

        private void LateUpdate()
        {
            // ensure all the light volume quads are camera-facing
            for (var i = 0; i < m_lightQuads.Count; i++)
            {
                var quadLook = Quaternion.LookRotation(m_lightQuads[i].transform.position - WorldBeyondManager.Instance.MainCamera.transform.position);
                m_lightQuads[i].transform.rotation = quadLook;
            }

            m_effectAccel += AbsorbingBall ? -0.1f : 0.05f;
            // when using the mesh, it's attached to the spinning piece, so adjust a bit
            var rotationOffset = WorldBeyondManager.Instance.UsingHands ? 0.0f : -0.03f;
            m_effectAccel = Mathf.Clamp(m_effectAccel, -0.2f, 0.05f) + rotationOffset;
            m_effectTimer += Time.deltaTime * m_effectAccel;
            MagicEffectCone.material.SetFloat("_ScrollAmount", m_effectTimer);

            if (IsActivated && AbsorbingBall)
            {
                WorldBeyondManager.Instance.AffectDebris(transform.position, false);
            }
        }

        public void SetLightStrength(float strength)
        {
            MultiToy.Instance.FlashlightLoop_1.SetVolume(strength);
            if (WorldBeyondManager.Instance.UsingHands)
            {
                MultiToy.Instance.FlashlightAbsorbLoop_1.SetVolume(0f);
            }

            Spotlight.intensity = m_spotlightBaseIntensity * strength;
            MagicEffectCone.material.SetFloat("_Intensity", strength);

            for (var i = 0; i < m_lightQuads.Count; i++)
            {
                m_lightQuads[i].material.SetFloat("_Intensity", strength);
            }
        }

        public override void Activate()
        {
            base.Activate();
            Flashlight.SetActive(true);
            MultiToy.Instance.FlashlightLoop_1.Play();
        }

        public override void Deactivate()
        {
            base.Deactivate();
            Flashlight.SetActive(false);
            MultiToy.Instance.StopLoopingSound();
            AbsorbingBall = false;
            MultiToy.Instance.FlashlightAbsorbLoop_1.Stop();
            if (!WorldBeyondManager.Instance.UsingHands)
            {
                OVRInput.SetControllerVibration(1, 0, WorldBeyondManager.Instance.GameController);
            }
        }

        public override void ActionDown()
        {
            AbsorbingBall = true;
            MultiToy.Instance.FlashlightAbsorbLoop_1.Play();
        }

        public override void Action()
        {
            if (!WorldBeyondManager.Instance.UsingHands)
            {
                OVRInput.SetControllerVibration(1, 0.5f, WorldBeyondManager.Instance.GameController);
            }
        }

        public override void ActionUp()
        {
            AbsorbingBall = false;
            MultiToy.Instance.FlashlightAbsorbLoop_1.Stop();
            if (!WorldBeyondManager.Instance.UsingHands)
            {
                OVRInput.SetControllerVibration(1, 0, WorldBeyondManager.Instance.GameController);
            }
        }
    }
}
