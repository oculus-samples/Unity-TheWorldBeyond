// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace TheWorldBeyond.Environment.RoomEnvironment
{
    public class SceneMesher : MonoBehaviour
    {
        public Material MeshMaterial;
        private GameObject m_sceneMeshGameObject;
        private Transform[] m_furnishings;
        private Mesh m_sceneMesh;
        private MeshRenderer m_meshRend;
        public float CeilingHeight { get; private set; }

        private bool m_initialized = false;

        /// <summary>
        /// Create a single mesh of the Scene objects.
        /// </summary>
        public MeshRenderer CreateSceneMesh(EffectMesh sceneObjects, float ceiling)
        {
            m_sceneMesh = new Mesh();
            m_sceneMeshGameObject = new GameObject("SceneMesh");
            _ = m_sceneMeshGameObject.AddComponent<MeshFilter>();
            m_sceneMeshGameObject.GetComponent<MeshFilter>().mesh = m_sceneMesh;
            m_meshRend = m_sceneMeshGameObject.AddComponent<MeshRenderer>();
            m_meshRend.material = MeshMaterial;
            m_initialized = true;

            CeilingHeight = ceiling;

            m_sceneMesh.subMeshCount = sceneObjects.EffectMeshObjects.Count;

            var meshFilters = new List<MeshFilter>();
            foreach (var kv in sceneObjects.EffectMeshObjects)
            {
                meshFilters.Add(kv.Value.effectMeshGO.GetComponent<MeshFilter>());
            }
            var combine = new CombineInstance[meshFilters.Count];

            var i = 0;
            while (i < meshFilters.Count)
            {
                combine[i].mesh = meshFilters[i].sharedMesh;
                combine[i].transform = meshFilters[i].transform.localToWorldMatrix;

                meshFilters[i].gameObject.SetActive(false);
                i++;
            }

            m_sceneMesh.CombineMeshes(combine);
            return m_meshRend;
        }

        public float GetRoomDiameter()
        {
            if (!m_initialized || m_sceneMesh == null)
            {
                Debug.Log("SceneMesher: room not initialized");
                return 0.0f;
            }

            var bounds = m_sceneMesh.bounds;
            var diameter = Mathf.Sqrt(Mathf.Pow(bounds.size.x, 2) + Mathf.Pow(bounds.size.z, 2) + Mathf.Pow(bounds.size.y, 2));
            return diameter;
        }
    }
}
