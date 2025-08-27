// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using System.Linq;
using Meta.XR.MRUtilityKit;
using TheWorldBeyond.Environment;
using UnityEngine;

namespace TheWorldBeyond.SampleScenes
{
    public class SamplePassthroughRoom : MonoBehaviour
    {
        // all virtual content is a Child of this Transform
        public Transform EnvRoot;
        // drop the virtual world this far below the floor anchor
        private const float GROUND_DELTA = 0.02f;

        private void Awake()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_ANDROID
            OVRManager.eyeFovPremultipliedAlphaModeEnabled = false;
#endif
        }

        public void InitializeRoom()
        {
            var sceneAnchors = MRUK.Instance.GetCurrentRoom().Anchors;
            if (sceneAnchors != null)
            {
                foreach (var anchor in sceneAnchors)
                {
                    if (anchor.Label is MRUKAnchor.SceneLabels.DOOR_FRAME or MRUKAnchor.SceneLabels.WALL_ART or MRUKAnchor.SceneLabels.CEILING or MRUKAnchor.SceneLabels.WINDOW_FRAME or MRUKAnchor.SceneLabels.WALL_FACE)
                    {
                        Destroy(anchor.gameObject);
                    }
                    else if (anchor.Label is MRUKAnchor.SceneLabels.FLOOR)
                    {
                        // move the world slightly below the ground floor, so the virtual floor doesn't Z-fight
                        if (EnvRoot)
                        {
                            var envPos = EnvRoot.transform.position;
                            var groundHeight = anchor.transform.position.y - GROUND_DELTA;
                            EnvRoot.transform.position = new Vector3(envPos.x, groundHeight, envPos.z);
                        }
                    }
                }
            }

            CullForegroundObjects();
        }

        /// <summary>
        /// If an object contains the ForegroundObject component and is inside the room, destroy it.
        /// </summary>
        private void CullForegroundObjects()
        {
            var foregroundObjects = EnvRoot.GetComponentsInChildren<ForegroundObject>();
            foreach (var obj in foregroundObjects)
            {
                if (MRUK.Instance.GetCurrentRoom().IsPositionInRoom(obj.transform.position))
                {
                    Destroy(obj.gameObject);
                }
            }
        }
    }
}
