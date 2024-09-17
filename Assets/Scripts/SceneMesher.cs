// Copyright (c) Meta Platforms, Inc. and affiliates.

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
    public Transform[] _debugQuads;

    void Awake()
    {
        if (_createDebugRoom && _debugSplinePoints.Length > 0)
        {
            List<Vector3> fakePoints = new List<Vector3>();
            for (int i = 0; i < _debugSplinePoints.Length; i++)
            {
                fakePoints.Add(_debugSplinePoints[i].position);
            }

            CreateSceneMesh(fakePoints, _debugFurnishings, _debugQuads, 1.5f);
        }
    }

    /// <summary>
    /// Create a single mesh of the Scene objects.
    /// Input points are required to be in clockwise order when viewed top-down.
    /// Each 4-edged wall has 8 vertices; 4 vertices for the corners, plus 4 for inset vertices (to make the border effect).
    /// The floor/ceiling mesh is similarly twice as many vertices; one set for the outline, another set for inset vertices.
    /// </summary>
    public MeshRenderer CreateSceneMesh(List<Vector3> cornerPoints, Transform[] sceneCubes, Transform[] sceneQuads, float ceiling)
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

        _cornerPoints = cornerPoints;
        _ceilingHeight = ceiling;

        // make sure the border width is no more than half the length of the shortest line
        _borderSize = Mathf.Min(_borderSize, _ceilingHeight * 0.5f);
        for (int i = 0; i < cornerPoints.Count; i++)
        {
            float lastEdge = (i == cornerPoints.Count - 1) ?
                Vector3.Distance(cornerPoints[i], cornerPoints[0]) : Vector3.Distance(cornerPoints[i], cornerPoints[i + 1]);
            _borderSize = Mathf.Min(_borderSize, lastEdge * 0.5f);
        }

        _furnishings = sceneCubes;

        // build meshes
        int capTriCount = cornerPoints.Count - 2;
        int wallVertCount = cornerPoints.Count * 8;
        int capVertCount = cornerPoints.Count * 2;
        int cubeVertCount = _furnishings.Length * 48;
        int quadVertCount = sceneQuads.Length * 8;

        int totalVertices = wallVertCount + capVertCount * 2 + cubeVertCount + quadVertCount;
        Vector3[] MeshVertices = new Vector3[totalVertices];
        Vector2[] MeshUVs = new Vector2[totalVertices];
        Color32[] MeshColors = new Color32[totalVertices];
        Vector3[] MeshNormals = new Vector3[totalVertices];
        Vector4[] MeshTangents = new Vector4[totalVertices];

        int wallIndexCount = cornerPoints.Count * 30;
        int capIndexCount = cornerPoints.Count * 6 + capTriCount * 3;
        int cubeIndexCount = _furnishings.Length * 180;
        int quadIndexCount = sceneQuads.Length * 30;
        int totalIndices = wallIndexCount + capIndexCount * 2 + cubeIndexCount + quadIndexCount;
        int[] MeshTriangles = new int[totalIndices];

        int vertCounter = 0;
        float Uspacing = 0.0f;
        int triCounter = 0;

        // create wall squares
        // each point has 8 vertices, forming 10 triangles
        for (int i = 0; i < cornerPoints.Count; i++)
        {
            Vector3 startPos = cornerPoints[i];
            Vector3 endPos = (i == cornerPoints.Count - 1) ? cornerPoints[0] : cornerPoints[i + 1];

            // direction to points
            Vector3 segmentDirection = (endPos - startPos).normalized;
            float ThisSegmentLength = (endPos - startPos).magnitude / _ceilingHeight;

            Vector3 wallNorm = Vector3.Cross(Vector3.up, -segmentDirection);
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
        for (int i = 0; i < cornerPoints.Count; i++)
        {
            Vector3 startPos = cornerPoints[i];
            Vector3 endPos = (i == cornerPoints.Count - 1) ? cornerPoints[0] : cornerPoints[i + 1];
            Vector3 lastPos = (i == 0) ? cornerPoints[cornerPoints.Count - 1] : cornerPoints[i - 1];

            // direction to points
            Vector3 thisSegmentDirection = (endPos - startPos).normalized;
            Vector3 lastSegmentDirection = (lastPos - startPos).normalized;

            // outer points
            MeshVertices[vertCounter] = cornerPoints[i];
            MeshUVs[vertCounter] = (_mappingInWorldUnits ? _ceilingHeight : 1.0f) * new Vector2(MeshVertices[vertCounter].x, MeshVertices[vertCounter].z) / _ceilingHeight;
            MeshColors[vertCounter] = Color.black;
            MeshNormals[vertCounter] = Vector3.up;
            MeshTangents[vertCounter] = floorTangent;

            // inner points
            int newID = vertCounter + cornerPoints.Count;
            Vector3 insetDirection = GetInsetDirection(lastPos, startPos, endPos);
            // ensure that the border is the same width regardless of angle between walls
            float angle = Vector3.Angle(thisSegmentDirection, insetDirection);
            float adjacent = _borderSize / Mathf.Tan(angle * Mathf.Deg2Rad);
            float adustedBorderSize = Mathf.Sqrt(adjacent * adjacent + _borderSize * _borderSize);
            Vector3 insetPoint = cornerPoints[i] + insetDirection * adustedBorderSize;
            insetPoints.Add(insetPoint);
            MeshVertices[newID] = insetPoint;
            MeshUVs[newID] = (_mappingInWorldUnits ? _ceilingHeight : 1.0f) * new Vector2(MeshVertices[newID].x, MeshVertices[newID].z) / _ceilingHeight;
            MeshColors[newID] = Color.white;
            MeshNormals[newID] = Vector3.up;
            MeshTangents[newID] = floorTangent;

            vertCounter++;
        }
        CreateBorderedPolygon(ref MeshTriangles, ref triCounter, wallVertCount, cornerPoints.Count, cornerPoints, false, insetPoints);

        // because we do unique counting for the caps, need to offset it
        vertCounter += cornerPoints.Count;

        // ceiling
        insetPoints.Clear();
        for (int i = 0; i < cornerPoints.Count; i++)
        {
            Vector3 startPos = cornerPoints[i];
            Vector3 endPos = (i == cornerPoints.Count - 1) ? cornerPoints[0] : cornerPoints[i + 1];
            Vector3 lastPos = (i == 0) ? cornerPoints[cornerPoints.Count - 1] : cornerPoints[i - 1];

            // direction to points
            Vector3 thisSegmentDirection = (endPos - startPos).normalized;
            Vector3 lastSegmentDirection = (lastPos - startPos).normalized;

            // outer points
            MeshVertices[vertCounter] = cornerPoints[i] + Vector3.up * _ceilingHeight;
            MeshUVs[vertCounter] = (_mappingInWorldUnits ? _ceilingHeight : 1.0f) * new Vector2(MeshVertices[vertCounter].x, MeshVertices[vertCounter].z) / _ceilingHeight;
            MeshColors[vertCounter] = Color.black;
            MeshNormals[vertCounter] = Vector3.down;
            MeshTangents[vertCounter] = ceilingTangent;

            // inner points
            int newID = vertCounter + cornerPoints.Count;
            Vector3 insetDirection = GetInsetDirection(lastPos, startPos, endPos);
            // ensure that the border is the same width regardless of angle between walls
            float angle = Vector3.Angle(thisSegmentDirection, insetDirection);
            float adjacent = _borderSize / Mathf.Tan(angle * Mathf.Deg2Rad);
            float adustedBorderSize = Mathf.Sqrt(adjacent * adjacent + _borderSize * _borderSize);
            Vector3 insetPoint = cornerPoints[i] + Vector3.up * _ceilingHeight + insetDirection * adustedBorderSize;
            insetPoints.Add(insetPoint);
            MeshVertices[newID] = insetPoint;
            MeshUVs[newID] = (_mappingInWorldUnits ? _ceilingHeight : 1.0f) * new Vector2(MeshVertices[newID].x, MeshVertices[newID].z) / _ceilingHeight;
            MeshColors[newID] = Color.white;
            MeshNormals[newID] = Vector3.down;
            MeshTangents[newID] = ceilingTangent;

            vertCounter++;
        }
        CreateBorderedPolygon(ref MeshTriangles, ref triCounter, wallVertCount + capVertCount, cornerPoints.Count, cornerPoints, true, insetPoints);
        vertCounter += cornerPoints.Count;

        // furnishings
        for (int i = 0; i < _furnishings.Length; i++)
        {
            Transform cube = _furnishings[i];
            Vector3 dim = cube.localScale;
            Vector3 cubeCenter = cube.position;

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
                    Vector3 basePoint = cubeCenter + fwd * 0.5f + right * 0.5f - up * 0.5f;
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
                    MeshVertices[vertCounter] = basePoint - cube.forward * dim.z * 0.5f;
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
                    MeshVertices[vertCounter] = MeshVertices[vertCounter - 4] + offset;
                    MeshUVs[vertCounter] = new Vector2(0, 0);
                    MeshColors[vertCounter] = Color.white;
                    MeshNormals[vertCounter] = cube.forward;
                    MeshTangents[vertCounter] = cube.right;
                    vertCounter++;
                }

                int baseVert = (wallVertCount + capVertCount * 2) + (i * 48) + (j * 8);
                CreateBorderedPolygon(ref MeshTriangles, ref triCounter, baseVert, 4);
            }
        }

        // doors and windows
        for (int i = 0; i < sceneQuads.Length; i++)
        {
            Vector3 quadNorm = -sceneQuads[i].forward;
            Vector4 quadTan = sceneQuads[i].right;

            Vector2 localScale = new Vector2(sceneQuads[i].localScale.x, sceneQuads[i].localScale.y);

            Vector3 xDim = localScale.x * sceneQuads[i].right;
            Vector3 yDim = localScale.y * sceneQuads[i].up;
            Vector3 leftBottom = sceneQuads[i].position - xDim * 0.5f - yDim * 0.5f;

            // outer vertices of quad
            Vector3 vert = leftBottom;
            for (int j = 0; j < 4; j++)
            {
                // CW loop order, starting from bottom left
                switch (j)
                {
                    case 1:
                        vert += yDim;
                        break;
                    case 2:
                        vert += xDim;
                        break;
                    case 3:
                        vert -= yDim;
                        break;
                }
                float UVx = (j == 0 || j == 1) ? 0.0f : 1.0f;
                float UVy = (j == 1 || j == 2) ? 1.0f : 0.0f;
                MeshVertices[vertCounter] = vert + quadNorm * 0.01f;
                MeshUVs[vertCounter] = new Vector2(UVx, UVy);
                MeshColors[vertCounter] = Color.black;
                MeshNormals[vertCounter] = quadNorm;
                MeshTangents[vertCounter] = quadTan;
                vertCounter++;
            }
            // inner vertices of quad
            for (int j = 0; j < 4; j++)
            {
                Vector3 offset = sceneQuads[i].up * _borderSize + sceneQuads[i].right * _borderSize;
                switch (j)
                {
                    case 1:
                        offset = -sceneQuads[i].up * _borderSize + sceneQuads[i].right * _borderSize;
                        break;
                    case 2:
                        offset = -sceneQuads[i].up * _borderSize - sceneQuads[i].right * _borderSize;
                        break;
                    case 3:
                        offset = sceneQuads[i].up * _borderSize - sceneQuads[i].right * _borderSize;
                        break;
                }
                MeshVertices[vertCounter] = MeshVertices[vertCounter - 4] + offset;
                MeshUVs[vertCounter] = new Vector2(0, 0);
                MeshColors[vertCounter] = Color.white;
                MeshNormals[vertCounter] = quadNorm;
                MeshTangents[vertCounter] = quadTan;
                vertCounter++;
            }

            int baseIndex = wallVertCount + capVertCount * 2 + cubeVertCount;
            CreateBorderedPolygon(ref MeshTriangles, ref triCounter, baseIndex + i * 8, 4);
        }

        // after calculating all data for the mesh, assign it
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

    /// <summary>
    /// For 2 walls defined by 3 corner points, get the inset direction from the inside corner.
    /// It will always point to the "inside" of the room
    /// </summary>
    public Vector3 GetInsetDirection(Vector3 point1, Vector3 point2, Vector3 point3)
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

    /// <summary>
    /// Given a clockwise set of points (outer then inner), set up triangle indices accordingly
    /// </summary>
    void CreateBorderedPolygon(ref int[] indexArray, ref int indexCounter, int baseCount, int pointsInLoop, List<Vector3> loopPoints = null, bool flipNormal = false, List<Vector3> insetPoints = null)
    {
        try
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
        catch (IndexOutOfRangeException exception)
        {
            Debug.LogError("Error parsing walls, are the walls intersecting? " + exception.Message);
            WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.ERROR_INTERSECTING_WALLS);
        }
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
}
