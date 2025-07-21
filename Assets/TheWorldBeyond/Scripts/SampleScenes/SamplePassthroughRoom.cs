// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using System.Linq;
using TheWorldBeyond.Environment;
using UnityEngine;
#pragma warning disable CS0618 // Type or member is obsolete

namespace TheWorldBeyond.SampleScenes
{
    public class SamplePassthroughRoom : MonoBehaviour
    {
        public OVRSceneManager SceneManager;

        // all virtual content is a Child of this Transform
        public Transform EnvRoot;

        // the corners of the room; for checking if a Position is in the room's boundaries
        private List<Vector3> m_cornerPoints = new();

        // drop the virtual world this far below the floor anchor
        private const float GROUND_DELTA = 0.02f;

        private void Awake()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_ANDROID
            OVRManager.eyeFovPremultipliedAlphaModeEnabled = false;
#endif
            SceneManager.SceneModelLoadedSuccessfully += InitializeRoom;
        }

        private void InitializeRoom()
        {
            var sceneAnchors = FindObjectsOfType<OVRSceneAnchor>();
            OVRSceneAnchor floorAnchor = null;
            if (sceneAnchors != null)
            {
                for (var i = 0; i < sceneAnchors.Length; i++)
                {
                    var instance = sceneAnchors[i];
                    var classification = instance.GetComponent<OVRSemanticClassification>();

                    if (classification.Contains(OVRSceneManager.Classification.WallFace) ||
                        classification.Contains(OVRSceneManager.Classification.Ceiling) ||
                        classification.Contains(OVRSceneManager.Classification.DoorFrame) ||
                        classification.Contains(OVRSceneManager.Classification.WindowFrame))
                    {
                        Destroy(instance.gameObject);
                    }
                    else if (classification.Contains(OVRSceneManager.Classification.Floor))
                    {
                        floorAnchor = instance;
                        // move the world slightly below the ground floor, so the virtual floor doesn't Z-fight
                        if (EnvRoot)
                        {
                            var envPos = EnvRoot.transform.position;
                            var groundHeight = instance.transform.position.y - GROUND_DELTA;
                            EnvRoot.transform.position = new Vector3(envPos.x, groundHeight, envPos.z);
                            if (OVRPlugin.GetSpaceBoundary2D(instance.Space, out var boundary))
                            {
                                // Use the Scence API and floor scene anchor to get the corner of the floor, and convert Vector2 to Vector3
                                m_cornerPoints = boundary.ToList()
                                    .ConvertAll(corner => new Vector3(-corner.x, corner.y, 0.0f));

                                // GetSpaceBoundary2D is in anchor-space
                                m_cornerPoints.Reverse();
                                for (var j = 0; j < m_cornerPoints.Count; j++)
                                {
                                    m_cornerPoints[j] = instance.transform.TransformPoint(m_cornerPoints[j]);
                                }
                            }
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
                if (m_cornerPoints != null && IsPositionInRoom(obj.transform.position))
                {
                    Destroy(obj.gameObject);
                }
            }
        }

        /// <summary>
        /// Given a world Position, test if it is within the floor outline (along horizontal dimensions)
        /// </summary>
        public bool IsPositionInRoom(Vector3 pos)
        {
            var floorPos = new Vector3(pos.x, m_cornerPoints[0].y, pos.z);
            // Shooting a ray from point to the right (X+), count how many walls it intersects.
            // If the count is odd, the point is in the room
            // Unfortunately we can't use Physics.RaycastAll, because the collision may not match the mesh, resulting in wrong counts
            var lineCrosses = 0;
            for (var i = 0; i < m_cornerPoints.Count; i++)
            {
                var startPos = m_cornerPoints[i];
                var endPos = (i == m_cornerPoints.Count - 1) ? m_cornerPoints[0] : m_cornerPoints[i + 1];

                // get bounding box of line segment
                var xMin = startPos.x < endPos.x ? startPos.x : endPos.x;
                var xMax = startPos.x > endPos.x ? startPos.x : endPos.x;
                var zMin = startPos.z < endPos.z ? startPos.z : endPos.z;
                var zMax = startPos.z > endPos.z ? startPos.z : endPos.z;
                var lowestPoint = startPos.z < endPos.z ? startPos : endPos;
                var highestPoint = startPos.z > endPos.z ? startPos : endPos;

                // it's vertically within the bounds, so it might cross
                if (floorPos.z <= zMax &&
                    floorPos.z >= zMin)
                {
                    if (floorPos.x <= xMin)
                    {
                        // it's completely to the left of this line segment's bounds, so must intersect
                        lineCrosses++;
                    }
                    else if (floorPos.x < xMax)
                    {
                        // it's within the bounds, so further calculation is needed
                        var lineVec = (highestPoint - lowestPoint).normalized;
                        var camVec = (floorPos - lowestPoint).normalized;
                        // polarity of cross product defines which side the point is on
                        if (Vector3.Cross(lineVec, camVec).y < 0)
                        {
                            lineCrosses++;
                        }
                    }
                    // else it's completely to the right of the bounds, so it definitely doesn't cross
                }
            }
            return (lineCrosses % 2) == 1;
        }
    }
}
