// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace TheWorldBeyond.SampleScenes
{
    public class SampleBoundaryDebris : MonoBehaviour
    {
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


        public void CreateDebris()
        {
            foreach (var anchor in MRUK.Instance.GetCurrentRoom().Anchors)
            {
                if (anchor.Label is MRUKAnchor.SceneLabels.FLOOR)
                {
                    m_cornerPoints.Clear();
                    for (var j = 0; j < anchor.PlaneBoundary2D.Count; j++)
                    {
                        var vertPos = new Vector3(-anchor.PlaneBoundary2D[j].x, anchor.PlaneBoundary2D[j].y, 0.0f);
                        // use world Position
                        m_cornerPoints.Add(anchor.transform.TransformPoint(vertPos));
                    }
                    CreateBoundaryDebris(anchor.transform, m_cornerPoints.ToArray());
                    CreateExteriorDebris(anchor.transform);
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
                    if (spawnDebris && !MRUK.Instance.GetCurrentRoom().IsPositionInRoom(desiredPosition))
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
    }
}
