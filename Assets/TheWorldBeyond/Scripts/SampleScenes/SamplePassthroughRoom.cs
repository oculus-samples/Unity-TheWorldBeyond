// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SamplePassthroughRoom : MonoBehaviour
{
    public OVRSceneManager _sceneManager;

    // all virtual content is a child of this Transform
    public Transform _envRoot;

    // the corners of the room; for checking if a position is in the room's boundaries
    List<Vector3> _cornerPoints = new List<Vector3>();

    // drop the virtual world this far below the floor anchor
    const float _groundDelta = 0.02f;

    void Awake()
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_ANDROID
        OVRManager.eyeFovPremultipliedAlphaModeEnabled = false;
#endif
        _sceneManager.SceneModelLoadedSuccessfully += InitializeRoom;
    }

    void InitializeRoom()
    {
        OVRSceneAnchor[] sceneAnchors = FindObjectsOfType<OVRSceneAnchor>();
        OVRSceneAnchor floorAnchor = null;
        if (sceneAnchors != null)
        {
            for (int i = 0; i < sceneAnchors.Length; i++)
            {
                OVRSceneAnchor instance = sceneAnchors[i];
                OVRSemanticClassification classification = instance.GetComponent<OVRSemanticClassification>();

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
                    if (_envRoot)
                    {
                        Vector3 envPos = _envRoot.transform.position;
                        float groundHeight = instance.transform.position.y - _groundDelta;
                        _envRoot.transform.position = new Vector3(envPos.x, groundHeight, envPos.z);
                        if (OVRPlugin.GetSpaceBoundary2D(instance.Space, out Vector2[] boundary))
                        {
                            // Use the Scence API and floor scene anchor to get the corner of the floor, and convert Vector2 to Vector3
                            _cornerPoints = boundary.ToList()
                                .ConvertAll<Vector3>(corner => new Vector3(-corner.x, corner.y, 0.0f));

                            // GetSpaceBoundary2D is in anchor-space
                            _cornerPoints.Reverse();
                            for (int j = 0; j < _cornerPoints.Count; j++)
                            {
                                _cornerPoints[j] = instance.transform.TransformPoint(_cornerPoints[j]);
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
    void CullForegroundObjects()
    {
        ForegroundObject[] foregroundObjects = _envRoot.GetComponentsInChildren<ForegroundObject>();
        foreach (ForegroundObject obj in foregroundObjects)
        {
            if (_cornerPoints != null && IsPositionInRoom(obj.transform.position))
            {
                Destroy(obj.gameObject);
            }
        }
    }

    /// <summary>
    /// Given a world position, test if it is within the floor outline (along horizontal dimensions)
    /// </summary>
    public bool IsPositionInRoom(Vector3 pos)
    {
        Vector3 floorPos = new Vector3(pos.x, _cornerPoints[0].y, pos.z);
        // Shooting a ray from point to the right (X+), count how many walls it intersects.
        // If the count is odd, the point is in the room
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
                    Vector3 lineVec = (highestPoint - lowestPoint).normalized;
                    Vector3 camVec = (floorPos - lowestPoint).normalized;
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
