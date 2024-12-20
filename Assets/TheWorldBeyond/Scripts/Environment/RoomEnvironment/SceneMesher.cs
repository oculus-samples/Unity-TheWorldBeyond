// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using TheWorldBeyond.Toy;
using TheWorldBeyond.Utils;
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

        private List<Vector3> m_cornerPoints = new();
        private bool m_initialized = false;
        public bool CreateDebugRoom = false;

        public float BorderSize = 0.1f;
        [Tooltip("If true, UV is in meters.  If false, UV scale is wall height.")]
        public bool MappingInWorldUnits = false;
        public Transform[] DebugSplinePoints;
        public Transform[] DebugFurnishings;
        public Transform[] DebugQuads;

        private void Awake()
        {
            if (CreateDebugRoom && DebugSplinePoints.Length > 0)
            {
                var fakePoints = new List<Vector3>();
                for (var i = 0; i < DebugSplinePoints.Length; i++)
                {
                    fakePoints.Add(DebugSplinePoints[i].position);
                }

                _ = CreateSceneMesh(fakePoints, DebugFurnishings, DebugQuads, 1.5f);
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
            if (!m_initialized)
            {
                m_sceneMesh = new Mesh();
                m_sceneMeshGameObject = new GameObject("SceneMesh");
                _ = m_sceneMeshGameObject.AddComponent<MeshFilter>();
                m_sceneMeshGameObject.GetComponent<MeshFilter>().mesh = m_sceneMesh;
                m_meshRend = m_sceneMeshGameObject.AddComponent<MeshRenderer>();
                m_meshRend.material = MeshMaterial;
                m_initialized = true;
            }

            m_cornerPoints = cornerPoints;
            CeilingHeight = ceiling;

            // make sure the border width is no more than half the length of the shortest line
            BorderSize = Mathf.Min(BorderSize, CeilingHeight * 0.5f);
            for (var i = 0; i < cornerPoints.Count; i++)
            {
                var lastEdge = (i == cornerPoints.Count - 1) ?
                    Vector3.Distance(cornerPoints[i], cornerPoints[0]) : Vector3.Distance(cornerPoints[i], cornerPoints[i + 1]);
                BorderSize = Mathf.Min(BorderSize, lastEdge * 0.5f);
            }

            m_furnishings = sceneCubes;

            // build meshes
            var capTriCount = cornerPoints.Count - 2;
            var wallVertCount = cornerPoints.Count * 8;
            var capVertCount = cornerPoints.Count * 2;
            var cubeVertCount = m_furnishings.Length * 48;
            var quadVertCount = sceneQuads.Length * 8;

            var totalVertices = wallVertCount + capVertCount * 2 + cubeVertCount + quadVertCount;
            var meshVertices = new Vector3[totalVertices];
            var meshUVs = new Vector2[totalVertices];
            var meshColors = new Color32[totalVertices];
            var meshNormals = new Vector3[totalVertices];
            var meshTangents = new Vector4[totalVertices];

            var wallIndexCount = cornerPoints.Count * 30;
            var capIndexCount = cornerPoints.Count * 6 + capTriCount * 3;
            var cubeIndexCount = m_furnishings.Length * 180;
            var quadIndexCount = sceneQuads.Length * 30;
            var totalIndices = wallIndexCount + capIndexCount * 2 + cubeIndexCount + quadIndexCount;
            var meshTriangles = new int[totalIndices];

            var vertCounter = 0;
            var uspacing = 0.0f;
            var triCounter = 0;

            // create wall squares
            // each point has 8 vertices, forming 10 triangles
            for (var i = 0; i < cornerPoints.Count; i++)
            {
                var startPos = cornerPoints[i];
                var endPos = (i == cornerPoints.Count - 1) ? cornerPoints[0] : cornerPoints[i + 1];

                // direction to points
                var segmentDirection = (endPos - startPos).normalized;
                var thisSegmentLength = (endPos - startPos).magnitude / CeilingHeight;

                var wallNorm = Vector3.Cross(Vector3.up, -segmentDirection);
                var wallTan = new Vector4(segmentDirection.x, segmentDirection.y, segmentDirection.z, 1);

                // outer vertices of wall
                for (var j = 0; j < 4; j++)
                {
                    var basePos = (j / 2 == 0) ? startPos : endPos;
                    var ceilingVert = (j is 1 or 2) ? 1.0f : 0.0f;
                    meshVertices[vertCounter] = basePos + Vector3.up * CeilingHeight * ceilingVert;
                    var uvX = (j / 2 == 0) ? uspacing : uspacing + thisSegmentLength;
                    meshUVs[vertCounter] = new Vector2(uvX, ceilingVert) * (MappingInWorldUnits ? CeilingHeight : 1.0f);
                    meshColors[vertCounter] = Color.black;
                    meshNormals[vertCounter] = wallNorm;
                    meshTangents[vertCounter] = wallTan;
                    vertCounter++;
                }
                // inner vertices of wall
                for (var j = 0; j < 4; j++)
                {
                    var ceilingVert = (j is 1 or 2) ? 1.0f : 0.0f;
                    var basePos = (j / 2 == 0) ? startPos : endPos;
                    basePos += (j / 2 == 0) ? segmentDirection * BorderSize : -segmentDirection * BorderSize;
                    basePos += Vector3.up * CeilingHeight * ceilingVert;
                    basePos -= Vector3.up * BorderSize * Mathf.Sign(ceilingVert - 0.5f);
                    meshVertices[vertCounter] = basePos;
                    var worldScaleBorder = BorderSize / CeilingHeight;
                    var uvX = (j / 2 == 0) ? uspacing : uspacing + thisSegmentLength;
                    uvX -= worldScaleBorder * Mathf.Sign(j / 2 - 0.5f);
                    var uvY = ceilingVert - worldScaleBorder * Mathf.Sign(ceilingVert - 0.5f);
                    meshUVs[vertCounter] = new Vector2(uvX, uvY) * (MappingInWorldUnits ? CeilingHeight : 1.0f);
                    meshColors[vertCounter] = Color.white;
                    meshNormals[vertCounter] = wallNorm;
                    meshTangents[vertCounter] = wallTan;
                    vertCounter++;
                }
                uspacing += thisSegmentLength;

                CreateBorderedPolygon(ref meshTriangles, ref triCounter, i * 8, 4);
            }

            // top down mapping means tangent is X-axis
            var floorTangent = new Vector4(1, 0, 0, 1);
            var ceilingTangent = new Vector4(-1, 0, 0, 1);
            var insetPoints = new List<Vector3>();

            // create floor
            for (var i = 0; i < cornerPoints.Count; i++)
            {
                var startPos = cornerPoints[i];
                var endPos = (i == cornerPoints.Count - 1) ? cornerPoints[0] : cornerPoints[i + 1];
                var lastPos = (i == 0) ? cornerPoints[^1] : cornerPoints[i - 1];

                // direction to points
                var thisSegmentDirection = (endPos - startPos).normalized;
                _ = (lastPos - startPos).normalized;

                // outer points
                meshVertices[vertCounter] = cornerPoints[i];
                meshUVs[vertCounter] = (MappingInWorldUnits ? CeilingHeight : 1.0f) * new Vector2(meshVertices[vertCounter].x, meshVertices[vertCounter].z) / CeilingHeight;
                meshColors[vertCounter] = Color.black;
                meshNormals[vertCounter] = Vector3.up;
                meshTangents[vertCounter] = floorTangent;

                // inner points
                var newID = vertCounter + cornerPoints.Count;
                var insetDirection = GetInsetDirection(lastPos, startPos, endPos);
                // ensure that the border is the same width regardless of angle between walls
                var angle = Vector3.Angle(thisSegmentDirection, insetDirection);
                var adjacent = BorderSize / Mathf.Tan(angle * Mathf.Deg2Rad);
                var adustedBorderSize = Mathf.Sqrt(adjacent * adjacent + BorderSize * BorderSize);
                var insetPoint = cornerPoints[i] + insetDirection * adustedBorderSize;
                insetPoints.Add(insetPoint);
                meshVertices[newID] = insetPoint;
                meshUVs[newID] = (MappingInWorldUnits ? CeilingHeight : 1.0f) * new Vector2(meshVertices[newID].x, meshVertices[newID].z) / CeilingHeight;
                meshColors[newID] = Color.white;
                meshNormals[newID] = Vector3.up;
                meshTangents[newID] = floorTangent;

                vertCounter++;
            }
            CreateBorderedPolygon(ref meshTriangles, ref triCounter, wallVertCount, cornerPoints.Count, cornerPoints, false, insetPoints);

            // because we do unique counting for the caps, need to offset it
            vertCounter += cornerPoints.Count;

            // ceiling
            insetPoints.Clear();
            for (var i = 0; i < cornerPoints.Count; i++)
            {
                var startPos = cornerPoints[i];
                var endPos = (i == cornerPoints.Count - 1) ? cornerPoints[0] : cornerPoints[i + 1];
                var lastPos = (i == 0) ? cornerPoints[^1] : cornerPoints[i - 1];

                // direction to points
                var thisSegmentDirection = (endPos - startPos).normalized;
                _ = (lastPos - startPos).normalized;

                // outer points
                meshVertices[vertCounter] = cornerPoints[i] + Vector3.up * CeilingHeight;
                meshUVs[vertCounter] = (MappingInWorldUnits ? CeilingHeight : 1.0f) * new Vector2(meshVertices[vertCounter].x, meshVertices[vertCounter].z) / CeilingHeight;
                meshColors[vertCounter] = Color.black;
                meshNormals[vertCounter] = Vector3.down;
                meshTangents[vertCounter] = ceilingTangent;

                // inner points
                var newID = vertCounter + cornerPoints.Count;
                var insetDirection = GetInsetDirection(lastPos, startPos, endPos);
                // ensure that the border is the same width regardless of angle between walls
                var angle = Vector3.Angle(thisSegmentDirection, insetDirection);
                var adjacent = BorderSize / Mathf.Tan(angle * Mathf.Deg2Rad);
                var adustedBorderSize = Mathf.Sqrt(adjacent * adjacent + BorderSize * BorderSize);
                var insetPoint = cornerPoints[i] + Vector3.up * CeilingHeight + insetDirection * adustedBorderSize;
                insetPoints.Add(insetPoint);
                meshVertices[newID] = insetPoint;
                meshUVs[newID] = (MappingInWorldUnits ? CeilingHeight : 1.0f) * new Vector2(meshVertices[newID].x, meshVertices[newID].z) / CeilingHeight;
                meshColors[newID] = Color.white;
                meshNormals[newID] = Vector3.down;
                meshTangents[newID] = ceilingTangent;

                vertCounter++;
            }
            CreateBorderedPolygon(ref meshTriangles, ref triCounter, wallVertCount + capVertCount, cornerPoints.Count, cornerPoints, true, insetPoints);
            vertCounter += cornerPoints.Count;

            // furnishings
            for (var i = 0; i < m_furnishings.Length; i++)
            {
                var cube = m_furnishings[i];
                var dim = cube.localScale;
                var cubeCenter = cube.position;

                // each cube face gets an 8-vertex mesh
                for (var j = 0; j < 6; j++)
                {
                    var right = cube.right * dim.x;
                    var up = cube.up * dim.y;
                    var fwd = cube.forward * dim.z;
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
                    for (var k = 0; k < 4; k++)
                    {
                        var basePoint = cubeCenter + fwd * 0.5f + right * 0.5f - up * 0.5f;
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
                        meshVertices[vertCounter] = basePoint - cube.forward * dim.z * 0.5f;
                        meshUVs[vertCounter] = new Vector2(0, 0);
                        meshColors[vertCounter] = Color.black;
                        meshNormals[vertCounter] = cube.forward;
                        meshTangents[vertCounter] = cube.right;
                        vertCounter++;
                    }
                    // inner vertices of face
                    for (var k = 0; k < 4; k++)
                    {
                        var offset = up.normalized * BorderSize - right.normalized * BorderSize;
                        switch (k)
                        {
                            case 1:
                                offset = -up.normalized * BorderSize - right.normalized * BorderSize;
                                break;
                            case 2:
                                offset = -up.normalized * BorderSize + right.normalized * BorderSize;
                                break;
                            case 3:
                                offset = up.normalized * BorderSize + right.normalized * BorderSize;
                                break;
                        }
                        meshVertices[vertCounter] = meshVertices[vertCounter - 4] + offset;
                        meshUVs[vertCounter] = new Vector2(0, 0);
                        meshColors[vertCounter] = Color.white;
                        meshNormals[vertCounter] = cube.forward;
                        meshTangents[vertCounter] = cube.right;
                        vertCounter++;
                    }

                    var baseVert = wallVertCount + capVertCount * 2 + i * 48 + j * 8;
                    CreateBorderedPolygon(ref meshTriangles, ref triCounter, baseVert, 4);
                }
            }

            // doors and windows
            for (var i = 0; i < sceneQuads.Length; i++)
            {
                var quadNorm = -sceneQuads[i].forward;
                Vector4 quadTan = sceneQuads[i].right;

                var localScale = new Vector2(sceneQuads[i].localScale.x, sceneQuads[i].localScale.y);

                var xDim = localScale.x * sceneQuads[i].right;
                var yDim = localScale.y * sceneQuads[i].up;
                var leftBottom = sceneQuads[i].position - xDim * 0.5f - yDim * 0.5f;

                // outer vertices of quad
                var vert = leftBottom;
                for (var j = 0; j < 4; j++)
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
                    var uvX = (j is 0 or 1) ? 0.0f : 1.0f;
                    var uvY = (j is 1 or 2) ? 1.0f : 0.0f;
                    meshVertices[vertCounter] = vert + quadNorm * 0.01f;
                    meshUVs[vertCounter] = new Vector2(uvX, uvY);
                    meshColors[vertCounter] = Color.black;
                    meshNormals[vertCounter] = quadNorm;
                    meshTangents[vertCounter] = quadTan;
                    vertCounter++;
                }
                // inner vertices of quad
                for (var j = 0; j < 4; j++)
                {
                    var offset = sceneQuads[i].up * BorderSize + sceneQuads[i].right * BorderSize;
                    switch (j)
                    {
                        case 1:
                            offset = -sceneQuads[i].up * BorderSize + sceneQuads[i].right * BorderSize;
                            break;
                        case 2:
                            offset = -sceneQuads[i].up * BorderSize - sceneQuads[i].right * BorderSize;
                            break;
                        case 3:
                            offset = sceneQuads[i].up * BorderSize - sceneQuads[i].right * BorderSize;
                            break;
                    }
                    meshVertices[vertCounter] = meshVertices[vertCounter - 4] + offset;
                    meshUVs[vertCounter] = new Vector2(0, 0);
                    meshColors[vertCounter] = Color.white;
                    meshNormals[vertCounter] = quadNorm;
                    meshTangents[vertCounter] = quadTan;
                    vertCounter++;
                }

                var baseIndex = wallVertCount + capVertCount * 2 + cubeVertCount;
                CreateBorderedPolygon(ref meshTriangles, ref triCounter, baseIndex + i * 8, 4);
            }

            // after calculating all data for the mesh, assign it
            m_sceneMesh.Clear();
            m_sceneMesh.name = "SceneMesh";
            m_sceneMesh.vertices = meshVertices;
            m_sceneMesh.uv = meshUVs;
            m_sceneMesh.colors32 = meshColors;
            m_sceneMesh.triangles = meshTriangles;
            m_sceneMesh.normals = meshNormals;
            m_sceneMesh.tangents = meshTangents;

            return m_meshRend;
        }

        /// <summary>
        /// For 2 walls defined by 3 corner points, get the inset direction from the inside corner.
        /// It will always point to the "inside" of the room
        /// </summary>
        public Vector3 GetInsetDirection(Vector3 point1, Vector3 point2, Vector3 point3)
        {
            var vec1 = (point2 - point1).normalized;
            var vec2 = (point3 - point2).normalized;
            var insetDir = Vector3.Normalize((vec2 - vec1) * 0.5f);
            _ = Vector3.Cross(Vector3.up, vec1);
            _ = Vector3.Cross(Vector3.up, vec2);
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
        private void CreateBorderedPolygon(ref int[] indexArray, ref int indexCounter, int baseCount, int pointsInLoop, List<Vector3> loopPoints = null, bool flipNormal = false, List<Vector3> insetPoints = null)
        {
            try
            {
                //int baseCount = baseIndex * 8; // 8 because each wall always has 8 vertices
                for (var j = 0; j < pointsInLoop; j++)
                {
                    var id1 = (j + 1) % pointsInLoop;
                    var id2 = pointsInLoop + j;

                    indexArray[indexCounter++] = baseCount + j;
                    indexArray[indexCounter++] = baseCount + (flipNormal ? id2 : id1);
                    indexArray[indexCounter++] = baseCount + (flipNormal ? id1 : id2);

                    indexArray[indexCounter++] = baseCount + pointsInLoop + (j + 1) % pointsInLoop;
                    indexArray[indexCounter++] = baseCount + (flipNormal ? id1 : id2);
                    indexArray[indexCounter++] = baseCount + (flipNormal ? id2 : id1);
                }

                var capTriCount = pointsInLoop - 2;

                if (loopPoints != null)
                {
                    //use triangulator
                    // WARNING: triangulator fails if any points are perfectly co-linear
                    // in practice this is rare due to floating point imprecision
                    var points2d = new List<Vector2>(loopPoints.Count);
                    for (var i = 0; i < pointsInLoop; i++)
                    {
                        var refP = insetPoints != null ? insetPoints[i] : loopPoints[i];
                        points2d.Add(new Vector2(refP.x, refP.z));
                    }

                    var triangulator = new Triangulator(points2d.ToArray());
                    var indices = triangulator.Triangulate();
                    for (var j = 0; j < capTriCount; j++)
                    {
                        var id0 = pointsInLoop + indices[j * 3];
                        var id1 = pointsInLoop + indices[j * 3 + 1];
                        var id2 = pointsInLoop + indices[j * 3 + 2];

                        indexArray[indexCounter++] = baseCount + id0;
                        indexArray[indexCounter++] = baseCount + (flipNormal ? id2 : id1);
                        indexArray[indexCounter++] = baseCount + (flipNormal ? id1 : id2);
                    }
                }
                else
                {
                    //use simple triangle fan
                    for (var j = 0; j < capTriCount; j++)
                    {
                        var id1 = pointsInLoop + j + 1;
                        var id2 = pointsInLoop + j + 2;
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
            if (!m_initialized)
            {
                Debug.Log("SceneMesher: room not initialized");
                return 0.0f;
            }
            // get the hypotenuse of the bounding box
            var highestX = 0.0f;
            var lowestX = 0.0f;
            var highestZ = 0.0f;
            var lowestZ = 0.0f;
            for (var i = 0; i < m_cornerPoints.Count; i++)
            {
                highestX = Math.Max(highestX, m_cornerPoints[i].x);
                lowestX = Math.Min(lowestX, m_cornerPoints[i].x);
                highestZ = Math.Max(highestZ, m_cornerPoints[i].z);
                lowestZ = Math.Min(lowestZ, m_cornerPoints[i].z);
            }

            var diameter = Mathf.Sqrt(Mathf.Pow(highestX - lowestX, 2) + Mathf.Pow(highestZ - lowestZ, 2) + Mathf.Pow(CeilingHeight, 2));
            return diameter;
        }
    }
}
