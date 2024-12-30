// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;

public class SampleBoundaryDebris : MonoBehaviour
{
    public OVRSceneManager _sceneManager;
    public GameObject[] _debrisPrefabs;

    // roughly how far apart the debris are from each other
    public float _averageSpacing = 0.7f;

    // debris objects are scattered in a noisy-grid pattern
    // this is the percent chance of a cell getting an object
    public float _debrisDensity = 0.5f;

    // add noise to positions so debris objects aren't perfectly aligned
    public float _boundaryNoiseDistance = 0.1f;
    int _cellCount = 20;

    List<Vector3> _cornerPoints = new List<Vector3>();

    void Awake()
    {
        _sceneManager.SceneModelLoadedSuccessfully += CreateDebris;
    }

    void CreateDebris()
    {
        OVRSceneAnchor[] sceneAnchors = FindObjectsOfType<OVRSceneAnchor>();
        if (sceneAnchors != null)
        {
            for (int i = 0; i < sceneAnchors.Length; i++)
            {
                OVRSceneAnchor instance = sceneAnchors[i];
                OVRSemanticClassification classification = instance.GetComponent<OVRSemanticClassification>();

                if (classification.Contains(OVRSceneManager.Classification.Floor))
                {
                    if (OVRPlugin.GetSpaceBoundary2D(instance.Space, out Vector2[] boundaryVertices))
                    {
                        _cornerPoints.Clear();
                        for (int j = 0; j < boundaryVertices.Length; j++)
                        {
                            Vector3 vertPos = new Vector3(-boundaryVertices[j].x, boundaryVertices[j].y, 0.0f);
                            // use world position
                            _cornerPoints.Add(instance.transform.TransformPoint(vertPos));
                        }
                        CreateBoundaryDebris(instance.transform, _cornerPoints.ToArray());
                        CreateExteriorDebris(instance.transform);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Scatter debris along the floor perimeter.
    /// </summary>
    void CreateBoundaryDebris(Transform floorTransform, Vector3[] boundaryVertices)
    {
        // "walk" around the room perimeter, creating debris along the way
        float accumulatedLength = 0.0f;

        GameObject boundarydebris = new GameObject("BoundaryDebris");
        boundarydebris.transform.SetParent(floorTransform, false);
        for (int i = 0; i < boundaryVertices.Length; i++)
        {
            int nextId = (i == boundaryVertices.Length - 1) ? 0 : i + 1;
            Vector3 vecToNext = boundaryVertices[nextId] - boundaryVertices[i];

            while (accumulatedLength < vecToNext.magnitude)
            {
                Vector3 debrisPos = boundaryVertices[i] + vecToNext.normalized * accumulatedLength;

                // add noise
                boundaryVertices[i] += (new Vector3(Random.Range(-1.0f, 1.0f), 0, Random.Range(-1.0f, 1.0f)).normalized * _boundaryNoiseDistance);

                CreateDebris(debrisPos, floorTransform, Random.Range(0.5f, 1.0f));

                accumulatedLength += (_averageSpacing + _averageSpacing * Random.Range(-0.5f, 0.5f));
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
    void CreateExteriorDebris(Transform floorTransform)
    {
        GameObject exteriorDebris = new GameObject("ExteriorDebris");
        exteriorDebris.transform.SetParent(floorTransform, false);

        GameObject[,] _debrisObjects = new GameObject[_cellCount, _cellCount];
        float cellSize = _averageSpacing;
        float mapSize = _cellCount * cellSize;
        float cHalf = cellSize * 0.5f;
        Vector3 roomCenter = floorTransform.position;
        Vector3 mapOffset = new Vector3(-mapSize * 0.5f, 0, -mapSize * 0.5f);
        Vector3 cellOffset = new Vector3(cellSize * 0.5f, 0, cellSize * 0.5f);
        Vector3 centerOffset = roomCenter + mapOffset + cellOffset;
        for (int x = 0; x < _cellCount; x++)
        {
            for (int y = 0; y < _cellCount; y++)
            {
                // % chance of this cell having an object
                bool spawnDebris = Random.Range(0.0f, 1.0f) <= _debrisDensity;

                // offset the grid point with random noise
                Vector3 cellCenter = centerOffset + new Vector3(x * cellSize, 0, y * cellSize);
                Vector3 randomOffset = new Vector3(Random.Range(-cHalf, cHalf), 0, Random.Range(-cHalf, cHalf));
                Vector3 desiredPosition = cellCenter + randomOffset;

                // only spawn an object if the position is outside of the room
                if (spawnDebris && !IsPositionInRoom(desiredPosition))
                {
                    // shrink object, based on distance from grid center
                    float distanceSize = Mathf.Abs(Vector3.Distance(roomCenter, desiredPosition));
                    distanceSize /= (mapSize * 0.5f);
                    distanceSize = Mathf.Clamp01(distanceSize);
                    // remap 0...1 to 1.5...1
                    distanceSize = (1 - distanceSize) * 0.5f + 1;

                    _debrisObjects[x, y] = CreateDebris(desiredPosition, floorTransform, Random.Range(0.5f, 1.0f) * distanceSize);
                }
                else
                {
                    _debrisObjects[x, y] = null;
                }
            }
        }
    }

    /// <summary>
    /// Instantiate a random debris object with a random rotation and provided scale.
    /// </summary>
    GameObject CreateDebris(Vector3 worldPosition, Transform parent, float uniformScale)
    {
        GameObject newObj = Instantiate(_debrisPrefabs[Random.Range(0, _debrisPrefabs.Length)], parent);
        newObj.transform.position = worldPosition;
        newObj.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360.0f), 0);
        newObj.transform.localScale = Vector3.one * uniformScale;
        return newObj;
    }

    /// <summary>
    /// Given a world position, test if it is within the floor outline (along horizontal dimensions)
    /// </summary>
    bool IsPositionInRoom(Vector3 worldPosition)
    {
        // Shooting a ray from worldPosition to the right (X+), count how many walls it intersects.
        // If the count is odd, the position is in the room
        // Unfortunately we can't use Physics.RaycastAll, because the collision may not match the mesh, resulting in wrong counts
        int lineCrosses = 0;
        for (int i = 0; i < _cornerPoints.Count; i++)
        {
            Vector3 startPos = _cornerPoints[i];
            Vector3 endPos = (i == _cornerPoints.Count - 1) ? _cornerPoints[0] : _cornerPoints[i + 1];

            // get bounding box of line segment
            float xMin = startPos.x < endPos.x ? startPos.x : endPos.x;
            float xMax = startPos.x > endPos.x ? startPos.x : endPos.x;
            float zMin = startPos.z < endPos.z ? startPos.z : endPos.z;
            float zMax = startPos.z > endPos.z ? startPos.z : endPos.z;
            Vector3 lowestPoint = startPos.z < endPos.z ? startPos : endPos;
            Vector3 highestPoint = startPos.z > endPos.z ? startPos : endPos;

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
                    Vector3 lineVec = (highestPoint - lowestPoint).normalized;
                    Vector3 camVec = (worldPosition - lowestPoint).normalized;
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
