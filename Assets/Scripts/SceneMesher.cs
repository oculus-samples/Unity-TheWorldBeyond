// Copyright(c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using UnityEngine;

public class SceneMesher : MonoBehaviour
{
    public Material _MeshMaterial;
    GameObject _sceneMeshGameObject;
    Transform[] _furnishings;
    Mesh _sceneMesh;
    MeshRenderer _meshRend;
    public float _ceilingHeight { get; private set; }
    List<Vector3> _cornerPoints = new List<Vector3>();
    bool _initialized = false;
    public bool _createDebugRoom = false;

    public float _borderSize = 0.1f;
    [Tooltip("If true, UV is in meters.  If false, UV scale is wall height.")]
    public bool _mappingInWorldUnits = false;
    public Transform[] _debugSplinePoints;
    public Transform[] _debugFurnishings;

    void Awake()
    {
        if (_createDebugRoom && _debugSplinePoints.Length > 0)
        {
            List<Vector3> fakePoints = new List<Vector3>();
            for (int i = 0; i < _debugSplinePoints.Length; i++)
            {
                fakePoints.Add(_debugSplinePoints[i].position);
            }

            CreateSceneMesh(fakePoints, _debugFurnishings, 1.5f);
        }
    }

    public MeshRenderer CreateSceneMesh(List<Transform> roomFaces, List<Transform> furnitureTransforms, float ceiling)
    {
        _cornerPoints.Clear();
        for (int i = 0; i < roomFaces.Count; i++)
        {
            Transform wallXform = roomFaces[i];
            Vector3 bottomLeftCorner = wallXform.position + (wallXform.up * wallXform.localScale.y * 0.5f) - (wallXform.right * wallXform.localScale.x * 0.5f);
            _cornerPoints.Add(bottomLeftCorner);
        }

        return CreateSceneMesh(_cornerPoints, furnitureTransforms.ToArray(), ceiling);
    }

    public MeshRenderer CreateSceneMesh(List<Vector3> points, Transform[] sceneObjects, float ceiling)
    {
        if (!_initialized)
        {
            _sceneMesh = new Mesh();
            _sceneMeshGameObject = new GameObject("SceneMesh");
            _sceneMeshGameObject.AddComponent<MeshFilter>();
            _sceneMeshGameObject.GetComponent<MeshFilter>().mesh = _sceneMesh;
            _meshRend = _sceneMeshGameObject.AddComponent<MeshRenderer>();
            _meshRend.material = _MeshMaterial;
            _initialized = true;
        }

        _ceilingHeight = ceiling;

        // list requires points being in clockwise order
        if (!IsListCW(points))
        {
            points.Reverse();
        }

        // make sure the border width is no more than half the length of the shortest line
        _borderSize = Mathf.Min(_borderSize, _ceilingHeight * 0.5f);
        for (int i = 0; i < points.Count; i++)
        {
            float lastEdge = (i == points.Count - 1) ?
                Vector3.Distance(points[i], points[0]) : Vector3.Distance(points[i], points[i + 1]);
            _borderSize = Mathf.Min(_borderSize, lastEdge * 0.5f);
        }

        _furnishings = sceneObjects;

        // build meshes
        int capTriCount = points.Count - 2;
        int wallVertCount = points.Count * 8;
        int capVertCount = points.Count * 2;
        int cubeVertCount = _furnishings.Length * 48;

        int totalVertices = wallVertCount + capVertCount * 2 + cubeVertCount;
        Vector3[] MeshVertices = new Vector3[totalVertices];
        Vector2[] MeshUVs = new Vector2[totalVertices];
        Color32[] MeshColors = new Color32[totalVertices];
        Vector3[] MeshNormals = new Vector3[totalVertices];
        Vector4[] MeshTangents = new Vector4[totalVertices];
        int wallIndexCount = points.Count * 30;
        int capIndexCount = points.Count * 6 + capTriCount * 3;
        int cubeIndexCount = _furnishings.Length * 180;

        int totalIndices = wallIndexCount + capIndexCount * 2 + cubeIndexCount;
        int[] MeshTriangles = new int[totalIndices];

        int vertCounter = 0;
        float Uspacing = 0.0f;
        int triCounter = 0;

        // create wall squares
        // each point has 8 vertices, forming 10 triangles
        for (int i = 0; i < points.Count; i++)
        {
            Vector3 startPos = points[i];
            Vector3 endPos = (i == points.Count - 1) ? points[0] : points[i + 1];

            // direction to points
            Vector3 segmentDirection = (endPos - startPos).normalized;
            float ThisSegmentLength = (endPos - startPos).magnitude / _ceilingHeight;

            Vector3 wallNorm = Vector3.Cross(Vector3.up, segmentDirection);
            Vector4 wallTan = new Vector4(segmentDirection.x, segmentDirection.y, segmentDirection.z, 1);

            // outer vertices of wall
            for (int j = 0; j < 4; j++)
            {
                Vector3 basePos = (j / 2 == 0) ? startPos : endPos;
                float ceilingVert = (j == 1 || j == 2) ? 1.0f : 0.0f;
                MeshVertices[vertCounter] = basePos + Vector3.up * _ceilingHeight * ceilingVert;
                float UVx = (j / 2 == 0) ? Uspacing : Uspacing + ThisSegmentLength;
                MeshUVs[vertCounter] = new Vector2(UVx, ceilingVert) * (_mappingInWorldUnits ? _ceilingHeight : 1.0f);
                MeshColors[vertCounter] = Color.black;
                MeshNormals[vertCounter] = wallNorm;
                MeshTangents[vertCounter] = wallTan;
                vertCounter++;
            }
            // inner vertices of wall
            for (int j = 0; j < 4; j++)
            {
                float ceilingVert = (j == 1 || j == 2) ? 1.0f : 0.0f;
                Vector3 basePos = (j / 2 == 0) ? startPos : endPos;
                basePos += (j / 2 == 0) ? segmentDirection * _borderSize : -segmentDirection * _borderSize;
                basePos += Vector3.up * _ceilingHeight * ceilingVert;
                basePos -= Vector3.up * _borderSize * Mathf.Sign(ceilingVert - 0.5f);
                MeshVertices[vertCounter] = basePos;
                float worldScaleBorder = _borderSize / _ceilingHeight;
                float UVx = (j / 2 == 0) ? Uspacing : Uspacing + ThisSegmentLength;
                UVx -= worldScaleBorder * Mathf.Sign((j / 2) - 0.5f);
                float UVy = ceilingVert - worldScaleBorder * Mathf.Sign(ceilingVert - 0.5f);
                MeshUVs[vertCounter] = new Vector2(UVx, UVy) * (_mappingInWorldUnits ? _ceilingHeight : 1.0f);
                MeshColors[vertCounter] = Color.white;
                MeshNormals[vertCounter] = wallNorm;
                MeshTangents[vertCounter] = wallTan;
                vertCounter++;
            }
            Uspacing += ThisSegmentLength;

            CreateBorderedPolygon(ref MeshTriangles, ref triCounter, i * 8, 4);
        }
        // top down mapping means tangent is X-axis
        Vector4 floorTangent = new Vector4(1, 0, 0, 1);
        Vector4 ceilingTangent = new Vector4(-1, 0, 0, 1);
        List<Vector3> insetPoints = new List<Vector3>();
        
        // create floor
        for (int i = 0; i < points.Count; i++)
        {
            Vector3 startPos = points[i];
            Vector3 endPos = (i == points.Count - 1) ? points[0] : points[i + 1];
            Vector3 lastPos = (i == 0) ? points[points.Count - 1] : points[i - 1];

            // direction to points
            Vector3 thisSegmentDirection = (endPos - startPos).normalized;
            Vector3 lastSegmentDirection = (lastPos - startPos).normalized;

            // outer points
            MeshVertices[vertCounter] = points[i];
            MeshUVs[vertCounter] = (_mappingInWorldUnits ? _ceilingHeight : 1.0f) * new Vector2(MeshVertices[vertCounter].x, MeshVertices[vertCounter].z) / _ceilingHeight;
            MeshColors[vertCounter] = Color.black;
            MeshNormals[vertCounter] = Vector3.up;
            MeshTangents[vertCounter] = floorTangent;

            // inner points
            int newID = vertCounter + points.Count;
            Vector3 insetDirection = GetInsetDirection(lastPos, startPos, endPos);
            // ensure that the border is the same width regardless of angle between walls
            float angle = Vector3.Angle(thisSegmentDirection, insetDirection);
            float adjacent = _borderSize / Mathf.Tan(angle * Mathf.Deg2Rad);
            float adustedBorderSize = Mathf.Sqrt(adjacent * adjacent + _borderSize * _borderSize);
            Vector3 insetPoint = points[i] + insetDirection * adustedBorderSize;
            insetPoints.Add(insetPoint);
            MeshVertices[newID] = insetPoint;
            MeshUVs[newID] = (_mappingInWorldUnits ? _ceilingHeight : 1.0f) * new Vector2(MeshVertices[newID].x, MeshVertices[newID].z) / _ceilingHeight;
            MeshColors[newID] = Color.white;
            MeshNormals[newID] = Vector3.up;
            MeshTangents[newID] = floorTangent;

            vertCounter++;
        }
        CreateBorderedPolygon(ref MeshTriangles, ref triCounter, wallVertCount, points.Count, points, false, insetPoints);

        // because we do unique counting for the caps, need to offset it
        vertCounter += points.Count;

        // ceiling
        insetPoints.Clear();
        for (int i = 0; i < points.Count; i++)
        {
            Vector3 startPos = points[i];
            Vector3 endPos = (i == points.Count - 1) ? points[0] : points[i + 1];
            Vector3 lastPos = (i == 0) ? points[points.Count - 1] : points[i - 1];
        
            // direction to points
            Vector3 thisSegmentDirection = (endPos - startPos).normalized;
            Vector3 lastSegmentDirection = (lastPos - startPos).normalized;

            // outer points
            MeshVertices[vertCounter] = points[i] + Vector3.up * _ceilingHeight;
            MeshUVs[vertCounter] = (_mappingInWorldUnits ? _ceilingHeight : 1.0f) * new Vector2(MeshVertices[vertCounter].x, MeshVertices[vertCounter].z) / _ceilingHeight;
            MeshColors[vertCounter] = Color.black;
            MeshNormals[vertCounter] = Vector3.down;
            MeshTangents[vertCounter] = ceilingTangent;
        
            // inner points
            int newID = vertCounter + points.Count;
            Vector3 insetDirection = GetInsetDirection(lastPos, startPos, endPos);
            // ensure that the border is the same width regardless of angle between walls
            float angle = Vector3.Angle(thisSegmentDirection, insetDirection);
            float adjacent = _borderSize / Mathf.Tan(angle * Mathf.Deg2Rad);
            float adustedBorderSize = Mathf.Sqrt(adjacent * adjacent + _borderSize * _borderSize);
            Vector3 insetPoint = points[i] + Vector3.up * _ceilingHeight + insetDirection * adustedBorderSize;
            insetPoints.Add(insetPoint);
            MeshVertices[newID] = insetPoint;
            MeshUVs[newID] = (_mappingInWorldUnits ? _ceilingHeight : 1.0f) * new Vector2(MeshVertices[newID].x, MeshVertices[newID].z) / _ceilingHeight;
            MeshColors[newID] = Color.white;
            MeshNormals[newID] = Vector3.down;
            MeshTangents[newID] = ceilingTangent;
        
            vertCounter++;
        }
        CreateBorderedPolygon(ref MeshTriangles, ref triCounter, wallVertCount + capVertCount, points.Count, points, true, insetPoints);
        vertCounter += points.Count;
        
        // furnishings
        for (int i = 0; i < _furnishings.Length; i++)
        {
            Transform cube = _furnishings[i];
            Vector3 dim = cube.localScale;
            // each cube face gets an 8-vertex mesh
            for (int j = 0; j < 6; j++)
            {
                Vector3 right = cube.right * dim.x;
                Vector3 up = cube.up * dim.y;
                Vector3 fwd = cube.forward * dim.z;
                switch (j)
                {
                    case 1:
                        right = cube.right * dim.x;
                        up = -cube.forward * dim.z;
                        fwd = cube.up * dim.y;
                        break;
                    case 2:
                        right = cube.right * dim.x;
                        up = -cube.up * dim.y;
                        fwd = -cube.forward * dim.z;
                        break;
                    case 3:
                        right = cube.right * dim.x;
                        up = cube.forward * dim.z;
                        fwd = -cube.up * dim.y;
                        break;
                    case 4:
                        right = -cube.forward * dim.z;
                        up = cube.up * dim.y;
                        fwd = cube.right * dim.x;
                        break;
                    case 5:
                        right = cube.forward * dim.z;
                        up = cube.up * dim.y;
                        fwd = -cube.right * dim.x;
                        break;
                }

                // outer verts of face
                for (int k = 0; k < 4; k++)
                {
                    Vector3 basePoint = cube.position + fwd * 0.5f + right * 0.5f - up * 0.5f;
                    switch (k)
                    {
                        case 1:
                            basePoint += up;
                            break;
                        case 2:
                            basePoint += up - right;
                            break;
                        case 3:
                            basePoint -= right;
                            break;
                    }
                    MeshVertices[vertCounter] = basePoint;
                    MeshUVs[vertCounter] = new Vector2(0, 0);
                    MeshColors[vertCounter] = Color.black;
                    MeshNormals[vertCounter] = cube.forward;
                    MeshTangents[vertCounter] = cube.right;
                    vertCounter++;
                }
                // inner vertices of face
                for (int k = 0; k < 4; k++)
                {
                    Vector3 offset = up.normalized * _borderSize - right.normalized * _borderSize;
                    switch (k)
                    {
                        case 1:
                            offset = -up.normalized * _borderSize - right.normalized * _borderSize;
                            break;
                        case 2:
                            offset = -up.normalized * _borderSize + right.normalized * _borderSize;
                            break;
                        case 3:
                            offset = up.normalized * _borderSize + right.normalized * _borderSize;
                            break;
                    }
                    MeshVertices[vertCounter] = MeshVertices[vertCounter-4] + offset;
                    MeshUVs[vertCounter] = new Vector2(0, 0);
                    MeshColors[vertCounter] = Color.white;
                    MeshNormals[vertCounter] = cube.forward;
                    MeshTangents[vertCounter] = cube.right;
                    vertCounter++;
                }

                int baseVert = (wallVertCount + capVertCount * 2) + (i * 48) + (j * 8);
                CreateBorderedPolygon(ref MeshTriangles, ref triCounter, baseVert, 4);
            }
            cube.gameObject.SetActive(false);
        }

        _sceneMesh.Clear();
        _sceneMesh.name = "SceneMesh";
        _sceneMesh.vertices = MeshVertices;
        _sceneMesh.uv = MeshUVs;
        _sceneMesh.colors32 = MeshColors;
        _sceneMesh.triangles = MeshTriangles;
        _sceneMesh.normals = MeshNormals;
        _sceneMesh.tangents = MeshTangents;

        return _meshRend;
    }

    Vector3 GetInsetDirection(Vector3 point1, Vector3 point2, Vector3 point3)
    {
        Vector3 vec1 = (point2 - point1).normalized;
        Vector3 vec2 = (point3 - point2).normalized;
        Vector3 insetDir = Vector3.Normalize((vec2 - vec1) * 0.5f);
        Vector3 wall1Normal = Vector3.Cross(Vector3.up, vec1);
        Vector3 wall2Normal = Vector3.Cross(Vector3.up, vec2);
        insetDir *= Vector3.Cross(vec1, vec2).y > 0 ? 1.0f : -1.0f;
        if (insetDir.magnitude == Mathf.Epsilon)
        {
            insetDir = Vector3.forward;
        }

        return insetDir;
    }

    // given a clockwise set of points (outer then inner), set up triangle indices accordingly
    void CreateBorderedPolygon(ref int[] indexArray, ref int indexCounter, int baseCount, int pointsInLoop, List<Vector3> loopPoints = null, bool flipNormal = false, List<Vector3> insetPoints = null)
    {
        //int baseCount = baseIndex * 8; // 8 because each wall always has 8 vertices
        for (int j = 0; j < pointsInLoop; j++)
        {
            int id1 = ((j + 1) % pointsInLoop);
            int id2 = pointsInLoop + j;

            indexArray[indexCounter++] = baseCount + j;
            indexArray[indexCounter++] = baseCount + (flipNormal ? id2 : id1);
            indexArray[indexCounter++] = baseCount + (flipNormal ? id1 : id2);

            indexArray[indexCounter++] = baseCount + pointsInLoop + ((j + 1) % pointsInLoop);
            indexArray[indexCounter++] = baseCount + (flipNormal ? id1 : id2);
            indexArray[indexCounter++] = baseCount + (flipNormal ? id2 : id1);
        }

        int capTriCount = pointsInLoop - 2;

        if (loopPoints != null)
        {
            //use triangulator
            // WARNING: triangulator fails if any points are perfectly co-linear
            // in practice this is rare due to floating point imprecision
            List<Vector2> points2d = new List<Vector2>(loopPoints.Count);
            for (int i = 0; i < pointsInLoop; i++)
            {
                Vector3 refP = insetPoints != null ? insetPoints[i] : loopPoints[i];
                points2d.Add(new Vector2(refP.x, refP.z));
            }

            Triangulator triangulator = new Triangulator(points2d.ToArray());
            int[] indices = triangulator.Triangulate();
            for (int j = 0; j < capTriCount; j++)
            {
                int id0 = pointsInLoop + indices[j * 3];
                int id1 = pointsInLoop + indices[j * 3 + 1];
                int id2 = pointsInLoop + indices[j * 3 + 2];

                indexArray[indexCounter++] = baseCount + id0;
                indexArray[indexCounter++] = baseCount + (flipNormal ? id2 : id1);
                indexArray[indexCounter++] = baseCount + (flipNormal ? id1 : id2);
            }
        }
        else
        {
            //use simple triangle fan
            for (int j = 0; j < capTriCount; j++)
            {
                int id1 = pointsInLoop + j + 1;
                int id2 = pointsInLoop + j + 2;
                indexArray[indexCounter++] = baseCount + pointsInLoop;
                indexArray[indexCounter++] = baseCount + (flipNormal ? id2 : id1);
                indexArray[indexCounter++] = baseCount + (flipNormal ? id1 : id2);
            }
        }
    }

    public bool IsListCW(List<Vector3> pointList)
    {
        bool isClockwise = true;
        int mainId = 0;
        for (int i = 0; i < pointList.Count - 2; i++)
        {
            // find lowest Z
            if (pointList[i].z < pointList[mainId].z)
            {
                mainId = i;
            }
            else if (pointList[i].z == pointList[mainId].z)
            {
                mainId = (pointList[i].x > pointList[mainId].x) ? i : mainId;
            }
        }
        Vector3 mainPos = pointList[mainId];
        Vector3 pos1 = (mainId == pointList.Count - 1) ? pointList[0] : pointList[mainId + 1];
        Vector3 pos2 = (mainId == 0) ? pointList[pointList.Count - 1] : pointList[mainId - 1];

        Vector3 vec1 = pos1 - mainPos;
        Vector3 vec2 = pos2 - mainPos;

        isClockwise = Vector3.Cross(vec1, vec2).y > 0;

        return isClockwise;
    }

    public float GetFarthestCorner(Vector3 refPos)
    {
        float farthest = 0.0f;
        for (int i = 0; i < _cornerPoints.Count; i++)
        {
            float currentDist = Vector3.Distance(refPos, _cornerPoints[i]);
            if (currentDist > farthest)
            {
                farthest = currentDist;
            }
        }
        return farthest;
    }

    public float GetRoomDiameter()
    {
        if (!_initialized)
        {
            Debug.Log("SceneMesher: room not initialized");
            return 0.0f;
        }
        // get the hypotenuse of the bounding box
        float highestX = 0.0f;
        float lowestX = 0.0f;
        float highestZ = 0.0f;
        float lowestZ = 0.0f;
        for (int i = 0; i < _cornerPoints.Count; i++)
        {
            highestX = Math.Max(highestX, _cornerPoints[i].x);
            lowestX = Math.Min(lowestX, _cornerPoints[i].x);
            highestZ = Math.Max(highestZ, _cornerPoints[i].z);
            lowestZ = Math.Min(lowestZ, _cornerPoints[i].z);
        }

        float diameter = Mathf.Sqrt(Mathf.Pow((highestX - lowestX), 2) + Mathf.Pow((highestZ - lowestZ), 2) + Mathf.Pow(_ceilingHeight, 2));
        return diameter;
    }

    public Vector3 GetEffectCorner(Transform camTransform)
    {
        if (!_initialized)
        {
            Debug.Log("SceneMesher: room not initialized");
            return Vector3.zero;
        }
        // find the farthest corner in front of the player
        Vector3 effectCorner = Vector3.one;
        float farthestDistance = 0.0f;
        for (int i = 0; i < _cornerPoints.Count; i++)
        {
            Vector3 toCam = _cornerPoints[i] - camTransform.position;
            float currentDot = Vector3.Dot(camTransform.forward, toCam.normalized);
            if (currentDot > 0.0f && toCam.magnitude > farthestDistance)
            {
                farthestDistance = toCam.magnitude;
                effectCorner = _cornerPoints[i] + _ceilingHeight * Vector3.up;
            }
        }

        return effectCorner;
    }
}
