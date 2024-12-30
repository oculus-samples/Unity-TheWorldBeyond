// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace TheWorldBeyond.Environment.RoomEnvironment
{
    public class WallEdge : MonoBehaviour
    {
        [HideInInspector]
        public WorldBeyondRoomObject ParentSurface;
        public ParticleSystem EdgePassthroughParticles;
        public ParticleSystem EdgeVirtualParticles;
        private ParticleSystemRenderer m_passthroughRenderer = null;
        private ParticleSystemRenderer m_virtualRenderer = null;
        [HideInInspector]
        public WallEdge SiblingEdge = null;

        // how much the edge particles emit per meter per second
        private const int EDGE_PARTICLE_RATE = 50;

        /// <summary>
        /// Because a wall edge can be an arbitrary size, we need to dynamically adjust the particle emitter.
        /// </summary>
        public void AdjustParticleSystemRateAndSize(float prtWidth)
        {
            if (!m_passthroughRenderer)
            {
                m_passthroughRenderer = EdgePassthroughParticles.gameObject.GetComponent<ParticleSystemRenderer>();
            }
            if (!m_virtualRenderer)
            {
                m_virtualRenderer = EdgeVirtualParticles.gameObject.GetComponent<ParticleSystemRenderer>();
            }
            SetParams(EdgePassthroughParticles, prtWidth);
            SetParams(EdgeVirtualParticles, prtWidth);
        }

        private void SetParams(ParticleSystem renderer, float prtWidth)
        {
            var prtBox = renderer.shape;
            prtBox.scale = new Vector3(prtWidth, prtBox.scale.y, prtBox.scale.z);
            var rate = renderer.emission;
            rate.rateOverTime = EDGE_PARTICLE_RATE * prtWidth;
        }

        /// <summary>
        /// When the wall is expanding/closing, pass values to the particle shader so the masking aligns.
        /// </summary>
        public void UpdateParticleMaterial(float effectTimer, Vector3 impactPosition, float invertedMask)
        {
            if (m_passthroughRenderer)
            {
                m_passthroughRenderer.material.SetFloat("_EffectTimer", effectTimer);
                m_passthroughRenderer.material.SetVector("_EffectPosition", impactPosition);
                m_passthroughRenderer.material.SetFloat("_InvertedMask", invertedMask);
            }
        }

        /// <summary>
        /// Gracefully stop or start the edge particles (instead of just SetActive).
        /// </summary>
        public void ShowEdge(bool doShow)
        {
            if (doShow)
            {
                EdgePassthroughParticles.Play();
                EdgeVirtualParticles.Play();
            }
            else
            {
                EdgePassthroughParticles.Stop();
                EdgeVirtualParticles.Stop();
            }
        }

        /// <summary>
        /// Force-clear all edge particles.
        /// </summary>
        public void ClearEdgeParticles()
        {
            EdgePassthroughParticles.Clear();
            EdgeVirtualParticles.Clear();
        }
    }
}
