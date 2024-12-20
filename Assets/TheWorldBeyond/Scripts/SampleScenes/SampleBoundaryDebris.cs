// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;

namespace TheWorldBeyond.SampleScenes
{
    public class SampleBoundaryDebris : MonoBehaviour
    {
        public OVRSceneManager SceneManager;
        public GameObject[] DebrisPrefabs;

        // roughly how far apart the debris are from each other
        public float AverageSpacing = 0.7f;

        // debris objects are scattered in a noisy-grid pattern
        // this is the percent chance of a cell getting an object
        public float DebrisDensity = 0.5f;

        // add noise to positions so debris objects aren't perfectly aligned
        public float BoundaryNoiseDistance = 0.1f;
        private int m_cellCount = 20;
        private List<Vector3> m_cornerPoints = new();

        private void Awake()
        {
            SceneManager.SceneModelLoadedSuccessfully += CreateDebris;
        }

        private void CreateDebris()
        {
            var sceneAnchors = FindObjectsOfType<OVRSceneAnchor>();
            if (sceneAnchors != null)
            {
                for (var i = 0; i < sceneAnchors.Length; i++)
                {
                    var instance = sceneAnchors[i];
                    var classification = instance.GetComponent<OVRSemanticClassification>();

                    if (classification.Contains(OVRSceneManager.Classification.Floor))
                    {
                        if (OVRPlugin.GetSpaceBoundary2D(instance.Space, out var boundaryVertices))
                        {
                            m_cornerPoints.Clear();
                            for (var j = 0; j < boundaryVertices.Length; j++)
                            {
                                var vertPos = new Vector3(-boundaryVertices[j].x, boundaryVertices[j].y, 0.0f);
                                // use world Position
                                m_cornerPoints.Add(instance.transform.TransformPoint(vertPos));
                            }
                            CreateBoundaryDebris(instance.transform, m_cornerPoints.ToArray());
                            CreateExteriorDebris(instance.transform);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Scatter debris along the floor perimeter.
        /// </summary>
        private void CreateBoundaryDebris(Transform floorTransform, Vector3[] boundaryVertices)
        {
            // "walk" around the room perimeter, creating debris along the way
            var accumulatedLength = 0.0f;

            var boundarydebris = new GameObject("BoundaryDebris");
            boundarydebris.transform.SetParent(floorTransform, false);
            for (var i = 0; i < boundaryVertices.Length; i++)
            {
                var nextId = (i == boundaryVertices.Length - 1) ? 0 : i + 1;
                var vecToNext = boundaryVertices[nextId] - boundaryVertices[i];

                while (accumulatedLength < vecToNext.magnitude)
                {
                    var debrisPos = boundaryVertices[i] + vecToNext.normalized * accumulatedLength;

                    // add noise
                    boundaryVertices[i] += new Vector3(Random.Range(-1.0f, 1.0f), 0, Random.Range(-1.0f, 1.0f)).normalized * BoundaryNoiseDistance;

                    _ = CreateDebris(debrisPos, floorTransform, Random.Range(0.5f, 1.0f));

                    accumulatedLength += AverageSpacing + AverageSpacing * Random.Range(-0.5f, 0.5f);
                    if (accumulatedLength >= vecToNext.magnitude)
                    {
                        accumulatedLength = 0.0f;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Scatter debris on the floor in a noisy grid pattern, outside of the room.
        /// </summary>
        private void CreateExteriorDebris(Transform floorTransform)
        {
            var exteriorDebris = new GameObject("ExteriorDebris");
            exteriorDebris.transform.SetParent(floorTransform, false);

            var debrisObjects = new GameObject[m_cellCount, m_cellCount];
            var cellSize = AverageSpacing;
            var mapSize = m_cellCount * cellSize;
            var cHalf = cellSize * 0.5f;
            var roomCenter = floorTransform.position;
            var mapOffset = new Vector3(-mapSize * 0.5f, 0, -mapSize * 0.5f);
            var cellOffset = new Vector3(cellSize * 0.5f, 0, cellSize * 0.5f);
            var centerOffset = roomCenter + mapOffset + cellOffset;
            for (var x = 0; x < m_cellCount; x++)
            {
                for (var y = 0; y < m_cellCount; y++)
                {
                    // % chance of this cell having an object
                    var spawnDebris = Random.Range(0.0f, 1.0f) <= DebrisDensity;

                    // offset the grid point with random noise
                    var cellCenter = centerOffset + new Vector3(x * cellSize, 0, y * cellSize);
                    var randomOffset = new Vector3(Random.Range(-cHalf, cHalf), 0, Random.Range(-cHalf, cHalf));
                    var desiredPosition = cellCenter + randomOffset;

                    // only spawn an object if the Position is outside of the room
                    if (spawnDebris && !IsPositionInRoom(desiredPosition))
                    {
                        // shrink object, based on distance from grid center
                        var distanceSize = Mathf.Abs(Vector3.Distance(roomCenter, desiredPosition));
                        distanceSize /= mapSize * 0.5f;
                        distanceSize = Mathf.Clamp01(distanceSize);
                        // remap 0...1 to 1.5...1
                        distanceSize = (1 - distanceSize) * 0.5f + 1;

                        debrisObjects[x, y] = CreateDebris(desiredPosition, floorTransform, Random.Range(0.5f, 1.0f) * distanceSize);
                    }
                    else
                    {
                        debrisObjects[x, y] = null;
                    }
                }
            }
        }

        /// <summary>
        /// Instantiate a random debris object with a random rotation and provided scale.
        /// </summary>
        private GameObject CreateDebris(Vector3 worldPosition, Transform parent, float uniformScale)
        {
            var newObj = Instantiate(DebrisPrefabs[Random.Range(0, DebrisPrefabs.Length)], parent);
            newObj.transform.position = worldPosition;
            newObj.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360.0f), 0);
            newObj.transform.localScale = Vector3.one * uniformScale;
            return newObj;
        }

        /// <summary>
        /// Given a world Position, test if it is within the floor outline (along horizontal dimensions)
        /// </summary>
        private bool IsPositionInRoom(Vector3 worldPosition)
        {
            // Shooting a ray from worldPosition to the right (X+), count how many walls it intersects.
            // If the count is odd, the Position is in the room
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
                if (worldPosition.z <= zMax &&
                    worldPosition.z >= zMin)
                {
                    if (worldPosition.x <= xMin)
                    {
                        // it's completely to the left of this line segment's bounds, so it must intersect
                        lineCrosses++;
                    }
                    else if (worldPosition.x < xMax)
                    {
                        // it's within the bounds, so further calculation is needed
                        var lineVec = (highestPoint - lowestPoint).normalized;
                        var camVec = (worldPosition - lowestPoint).normalized;
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
