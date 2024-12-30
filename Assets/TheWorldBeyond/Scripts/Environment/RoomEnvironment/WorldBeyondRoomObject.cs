// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;

namespace TheWorldBeyond.Environment.RoomEnvironment
{
    public class WorldBeyondRoomObject : MonoBehaviour
    {
        public MeshRenderer PassthroughMesh;
        private Material m_defaultMaterial;
        public Material DarkRoomMaterial;
        public int SurfaceID = 0;
        public Vector3 Dimensions = Vector3.one;

        [HideInInspector]
        public bool IsWall = false;
        [HideInInspector]
        public bool IsFurniture = false;

        [Header("Wall Fading")]
        private bool m_animating = false;
        [HideInInspector]
        public float EffectTimer = 0.0f;
        private const float EFFECT_TIME = 1.0f;
        public bool PassthroughWallActive = true;
        public Vector3 ImpactPosition = new(0, 1000, 0);
        public List<WallEdge> WallEdges = new();
        public List<GameObject> WallDebris = new();

        private void Start()
        {
            m_defaultMaterial = PassthroughMesh.material;
        }

        private void Update()
        {
            if (!m_animating)
            {
                return;
            }

            EffectTimer += Time.deltaTime;
            if (EffectTimer >= EFFECT_TIME)
            {
                m_animating = false;
            }
            EffectTimer = Mathf.Clamp01(EffectTimer);
            if (PassthroughMesh)
            {
                PassthroughMesh.material.SetFloat("_EffectTimer", EffectTimer);
                PassthroughMesh.material.SetVector("_EffectPosition", ImpactPosition);
                PassthroughMesh.material.SetFloat("_InvertedMask", PassthroughWallActive ? 1.0f : 0.0f);
            }
            foreach (var edge in WallEdges)
            {
                edge.UpdateParticleMaterial(EffectTimer, ImpactPosition, PassthroughWallActive ? 1.0f : 0.0f);
            }

            var smoothTimer = Mathf.Cos(Mathf.PI * EffectTimer / EFFECT_TIME) * 0.5f + 0.5f;
            foreach (var obj in WallDebris)
            {
                obj.transform.localScale = Vector3.one * (PassthroughWallActive ? smoothTimer : (1.0f - smoothTimer));
            }
        }

        /// <summary>
        /// The toggle rate of the animation effect needs to be limited for it to work properly.
        /// </summary>
        public bool CanBeToggled()
        {
            return !m_animating;
        }

        /// <summary>
        /// Trigger the particles and shader effect on the wall material, as well as the start Position for it.
        /// </summary>
        public bool ToggleWall(Vector3 hitPoint)
        {
            ImpactPosition = hitPoint;
            PassthroughWallActive = !PassthroughWallActive;
            EffectTimer = 0.0f;
            m_animating = true;
            return PassthroughWallActive;
        }

        /// <summary>
        /// "Reset" the wall to full Passthrough.
        /// </summary>
        public void ForcePassthroughMaterial()
        {
            PassthroughWallActive = true;

            if (PassthroughMesh)
            {
                PassthroughMesh.material.SetFloat("_EffectTimer", 0.0f);
                PassthroughMesh.material.SetVector("_EffectPosition", Vector3.up * 1000);
                PassthroughMesh.material.SetFloat("_InvertedMask", 0.0f);
            }

            foreach (var edge in WallEdges)
            {
                edge.UpdateParticleMaterial(0.0f, Vector3.up * 1000, 0.0f);
            }
        }

        /// <summary>
        /// During the intro sequence when the MultiToy appears, the room is a different material
        /// </summary>
        public void ShowDarkRoomMaterial(bool showDarkRoom)
        {
            PassthroughMesh.material = showDarkRoom ? DarkRoomMaterial : m_defaultMaterial;
        }
    }
}
