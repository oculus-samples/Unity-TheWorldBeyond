// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TheWorldBeyond.Audio;
using TheWorldBeyond.GameManagement;
using TheWorldBeyond.Toy;
using TheWorldBeyond.Utils;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;
#pragma warning disable CS0618 // Type or member is obsolete

namespace TheWorldBeyond.Environment.RoomEnvironment
{
    public class VirtualRoom : MonoBehaviour
    {
        public static VirtualRoom Instance = null;

        public GameObject AnchorPrefab;
        public GameObject EdgePrefab;
        private bool m_roomOpen = false;
        private List<WorldBeyondRoomObject> m_roomboxFurnishings = new();
        private List<WorldBeyondRoomObject> m_roomboxWalls = new();
        public SceneMesher SceneMesher;
        private bool m_sceneMeshCreated = false;
        private List<Vector3> m_cornerPoints = new();
        private int m_roomFloorID;
        private int m_roomCeilingID;
        private float m_floorHeight = 0.0f;
        private float m_wallHeight = 3.0f;

        // if an anchor is within this angle tolerance, snap it to be Gravity-aligned
        private float m_alignmentAngleThreshold = 5.0f;

        // drop the virtual world this far below the floor anchor
        private const float GROUND_DELTA = 0.01f;

        // because of anchor imprecision, the room isn't watertight.
        // this extends each surface to hide the cracks.
        private const float EDGE_BUFFER = 0.02f;
        private List<GameObject> m_roomDebris = new();
        private MeshRenderer m_sceneMesh = null;
        private float m_effectRadius = 10.0f;
        public AnimationCurve EdgeIntensity;
        private OVRSceneAnchor m_floorSceneAnchor = null;

        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
            }
        }

        /// <summary>
        /// Manage the particle effect borders when toggling a wall.
        /// </summary>
        public void CloseWall(int wallID, bool doClose)
        {
            _ = StartCoroutine(CloseWallDelayed(wallID, doClose));
        }

        private IEnumerator CloseWallDelayed(int wallID, bool doClose)
        {
            var waitTime = doClose ? 0.0f : 1.0f;
            // if opening a wall, show the neighboring edges immediately
            if (!doClose)
            {
                for (var j = 0; j < m_roomboxWalls[wallID].WallEdges.Count; j++)
                {
                    var edge = m_roomboxWalls[wallID].WallEdges[j];
                    if (edge.SiblingEdge.ParentSurface.PassthroughWallActive)
                    {
                        edge.SiblingEdge.ShowEdge(true);
                    }
                }
            }
            yield return new WaitForSeconds(waitTime);

            // if closing a wall, delay hiding the neighboring edges
            m_roomboxWalls[wallID].PassthroughMesh.gameObject.SetActive(doClose);

            for (var j = 0; j < m_roomboxWalls[wallID].WallEdges.Count; j++)
            {
                var edge = m_roomboxWalls[wallID].WallEdges[j];
                if (doClose)
                {
                    if (!edge.SiblingEdge.ParentSurface.PassthroughWallActive)
                    {
                        // only show edge if neighboring wall is open
                        edge.ShowEdge(true);
                    }
                    else
                    {
                        // if neighboring wall is closed, turn off both edges
                        edge.SiblingEdge.ShowEdge(false);
                        edge.ShowEdge(false);
                    }
                }
                else
                {
                    // hide all edges for this wall
                    edge.ShowEdge(false);
                }
            }

            CheckForClosedWalls();
            AudioManager.SetRoomOpenness(Instance.GetRoomOpenAmount());
        }

        /// <summary>
        /// If the room is completely closed, hide the grass around furniture and walls.
        /// </summary>
        private void CheckForClosedWalls()
        {
            // if all walls are closed, hide room grass
            var wallOpen = false;
            for (var i = 0; i < m_roomboxWalls.Count; i++)
            {
                if (m_roomboxWalls[i].IsWall && !m_roomboxWalls[i].PassthroughWallActive)
                {
                    wallOpen = true;
                }
            }

            if (m_roomOpen != wallOpen)
            {
                _ = StartCoroutine(GrowGrass(wallOpen));
                m_roomOpen = wallOpen;
            }
        }

        /// <summary>
        /// For grass inside the room, grow/shrink it if any walls are open/closed.
        /// </summary>
        private IEnumerator GrowGrass(bool doGrow)
        {
            var timer = 0.0f;
            var growTime = 1.0f;
            while (timer <= growTime)
            {
                timer += Time.deltaTime;
                var smoothTimer = Mathf.Cos(Mathf.PI * Mathf.Clamp01(timer / growTime)) * 0.5f + 0.5f;
                smoothTimer = doGrow ? (1 - smoothTimer) : smoothTimer;
                foreach (var obj in m_roomDebris)
                {
                    obj.transform.localScale = Vector3.one * smoothTimer * 0.5f;
                }
                yield return null;
            }
        }

        /// <summary>
        /// "Reset" the Passthrough room, including hiding border effects and furniture/wall grass.
        /// </summary>
        public void ShowAllWalls(bool doShow)
        {
            for (var i = 0; i < m_roomboxFurnishings.Count; i++)
            {
                m_roomboxFurnishings[i].PassthroughMesh.gameObject.SetActive(doShow);
            }

            for (var i = 0; i < m_roomboxWalls.Count; i++)
            {
                m_roomboxWalls[i].PassthroughMesh.gameObject.SetActive(doShow);

                var sro = m_roomboxWalls[i];
                for (var j = 0; j < sro.WallEdges.Count; j++)
                {
                    sro.WallEdges[j].ShowEdge(false);
                    if (!doShow)
                    {
                        sro.WallEdges[j].ClearEdgeParticles();
                    }
                }

                for (var j = 0; j < m_roomboxWalls[i].WallDebris.Count; j++)
                {
                    m_roomboxWalls[i].WallDebris[j].transform.localScale = Vector3.zero;
                }

                if (doShow)
                {
                    m_roomboxWalls[i].ForcePassthroughMaterial();
                }
            }

            foreach (var child in m_roomDebris)
            {
                child.transform.localScale = Vector3.zero;
            }

            m_roomOpen = !doShow;
            AudioManager.SetRoomOpenness(Instance.GetRoomOpenAmount());
        }

        private void SetChildrenScale(Transform parentTransform, Vector3 childScale)
        {
            for (var j = 0; j < parentTransform.childCount; j++)
            {
                parentTransform.GetChild(j).localScale = childScale;
            }
        }

        /// <summary>
        /// Create the actual room surfaces, since the OVRSceneObject array contains just metadata.
        /// Attach the instantiated walls/furniture to the anchors, to ensure they're fixed to the real world.
        /// </summary>
        public void Initialize(OVRSceneAnchor[] sceneAnchors)
        {
            m_roomboxWalls.Clear();

            var anchorsAsWBRO = new List<WorldBeyondRoomObject>();
            var doorsAndWindows = new List<GameObject>();
            for (var i = 0; i < sceneAnchors.Length; i++)
            {
                var instance = sceneAnchors[i];
                if (instance.GetComponent<OVRSceneRoom>())
                {
                    continue;
                }
                var classification = instance.GetComponent<OVRSemanticClassification>();
                if (classification) Debug.Log(string.Format("TWB Anchor {0}: {1}", i, classification.Labels[0]));
                var wbro = instance.GetComponent<WorldBeyondRoomObject>();
                anchorsAsWBRO.Add(wbro);
                wbro.Dimensions = instance.transform.GetChild(0).localScale;
                if (classification.Contains(OVRSceneManager.Classification.WallFace) ||
                    classification.Contains(OVRSceneManager.Classification.Floor) ||
                    classification.Contains(OVRSceneManager.Classification.Ceiling))
                {
                    // m_force-flatten it so there's no virtual/passthrough intersections
                    instance.transform.rotation = GravityAligner.GetAlignedOrientation(instance.transform.rotation, m_alignmentAngleThreshold);

                    wbro.SurfaceID = m_roomboxWalls.Count;

                    if (classification.Contains(OVRSceneManager.Classification.Floor))
                    {
                        m_roomFloorID = m_roomboxWalls.Count;

                        // move the world slightly below the ground floor, so the virtual floor doesn't Z-fight
                        WorldBeyondManager.Instance.MoveGroundFloor(instance.transform.position.y - GROUND_DELTA);
                        m_floorHeight = instance.transform.position.y;
                        m_floorSceneAnchor = instance;
                    }
                    else if (classification.Contains(OVRSceneManager.Classification.Ceiling))
                    {
                        m_roomCeilingID = m_roomboxWalls.Count;
                    }
                    m_roomboxWalls.Add(wbro);

                    if (classification.Contains(OVRSceneManager.Classification.WallFace))
                    {
                        wbro.IsWall = true;

                        CreateWallBorderEffects(wbro);
                        CreateWallDebris(wbro);
                        CreateNavMeshObstacle(wbro);
                    }

                    // the collider on the root anchor object is only used by the wall toy
                    if (wbro.GetComponent<BoxCollider>())
                    {
                        wbro.GetComponent<BoxCollider>().size = new Vector3(wbro.Dimensions.x, wbro.Dimensions.y, 0.01f);
                    }
                }
                else if (instance.GetComponent<OVRSceneVolume>())
                {
                    wbro.IsFurniture = true;
                    m_roomboxFurnishings.Add(wbro);

                    // add as navmesh obstacles
                    var bc = wbro.PassthroughMesh.GetComponent<BoxCollider>();
                    if (bc && bc.gameObject.GetComponent<NavMeshObstacle>() == null)
                    {
                        var obstacle = bc.gameObject.AddComponent<NavMeshObstacle>();
                        obstacle.carving = true;
                        obstacle.shape = NavMeshObstacleShape.Box;
                        obstacle.size = Vector3.one;
                        obstacle.center = bc.center;
                    }

                    // the collider on the root anchor object is only used by the wall toy
                    if (wbro.GetComponent<BoxCollider>() && wbro.transform.childCount > 0)
                    {
                        var objLocalScale = wbro.transform.GetChild(0).localScale;
                        wbro.GetComponent<BoxCollider>().size = objLocalScale;
                        wbro.GetComponent<BoxCollider>().center = -Vector3.forward * objLocalScale.z * 0.5f;

                        CreateFurnitureDebris(instance.transform, objLocalScale);
                    }
                }
                else if (classification.Contains(OVRSceneManager.Classification.DoorFrame) ||
                   classification.Contains(OVRSceneManager.Classification.WindowFrame) ||
                   classification.Contains(OVRSceneManager.Classification.WallArt))
                {
                    doorsAndWindows.Add(instance.gameObject);
                }
            }

            if (m_roomboxWalls.Count < 5)
            {
                WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.ERROR_NOT_ENOUGH_WALLS);
            }

            m_cornerPoints = GetClockwiseFloorOutline(anchorsAsWBRO.ToArray());

            CreatePolygonMesh(m_roomboxWalls, m_roomboxWalls[m_roomFloorID], false);
            CreatePolygonMesh(m_roomboxWalls, m_roomboxWalls[m_roomCeilingID], true);

            m_wallHeight = m_roomboxWalls[m_roomCeilingID].transform.position.y - m_roomboxWalls[m_roomFloorID].transform.position.y;

            CreateFloorCeilingBorderEffects();

            // pair all the edges up, making it easier to hide/unhide them
            // while searching, also make sure they align with neighboring walls
            for (var i = 0; i < m_roomboxWalls.Count; i++)
            {
                for (var j = 0; j < m_roomboxWalls[i].WallEdges.Count; j++)
                {
                    FindSiblingEdge(m_roomboxWalls[i].WallEdges[j]);
                }
            }

            AudioManager.SetRoomOpenness(GetRoomOpenAmount());

            CreateSpecialEffectMesh(anchorsAsWBRO.ToArray());

            // doors and windows aren't used in the experience, because they would add complexity
            // they are however used in the special effect mesh, so must be deleted after that is created
            foreach (var obj in doorsAndWindows)
            {
                Destroy(obj);
            }
        }

        /// <summary>
        /// When toggling walls, pre-pairing edges makes them easier to coordinate. While searching, also orient them to match the "open" space.
        /// </summary>
        private void FindSiblingEdge(WallEdge edge)
        {
            // skip if edge already has a sibling (assigned from a prior search)
            if (edge.SiblingEdge != null)
            {
                return;
            }

            // Get closest edge by distance, since ordering of wall indices isn't guaranteed
            // The sibling edge should be at the exact same Position, but check for closest to avoid precision errors
            var closestDistance = 100.0f;
            WallEdge siblingEdge = null;
            for (var i = 0; i < m_roomboxWalls.Count; i++)
            {
                // skip wall if edge belongs to it
                if (i == edge.ParentSurface.SurfaceID)
                {
                    continue;
                }
                for (var j = 0; j < m_roomboxWalls[i].WallEdges.Count; j++)
                {
                    var thisEdge = m_roomboxWalls[i].WallEdges[j];
                    var thisDist = Vector3.Distance(edge.transform.position, thisEdge.transform.position);
                    if (thisDist < closestDistance)
                    {
                        closestDistance = thisDist;
                        siblingEdge = thisEdge;
                    }
                }
            }
            // pair them up
            edge.SiblingEdge = siblingEdge;
            siblingEdge.SiblingEdge = edge;

            // align the vertical ones better, since wall angles are variable
            var wallSide = Vector3.Dot(edge.transform.right, Vector3.up);
            if (Mathf.Abs(wallSide) > 0.9f)
            {
                _ = Mathf.Sign(wallSide);
                edge.transform.rotation = Quaternion.LookRotation(-siblingEdge.ParentSurface.transform.forward, -siblingEdge.ParentSurface.transform.right * wallSide);
                siblingEdge.transform.rotation = Quaternion.LookRotation(-edge.ParentSurface.transform.forward, edge.ParentSurface.transform.right * wallSide);
            }
        }

        /// <summary>
        /// This creates the watertight single mesh, from all walls and furniture.
        /// </summary>
        public void CreateSpecialEffectMesh(WorldBeyondRoomObject[] worldBeyondObjects)
        {
            // for simplicity, doors/windows have been excluded from the experience
            // however, we'd still like them to render in this special effect mesh
            var cubeFurniture = new List<Transform>();
            var quadFurniture = new List<Transform>();
            var doorsAndWindows = new List<GameObject>();
            var ceilingHeight = 1.0f;
            for (var i = 0; i < worldBeyondObjects.Length; i++)
            {
                var classification = worldBeyondObjects[i].GetComponent<OVRSemanticClassification>();
                if (worldBeyondObjects[i].GetComponent<OVRSceneVolume>())
                {
                    // any Child will have correct scale
                    cubeFurniture.Add(worldBeyondObjects[i].transform.GetChild(0));
                }
                else if (classification.Contains(OVRSceneManager.Classification.DoorFrame) ||
                    classification.Contains(OVRSceneManager.Classification.WindowFrame))
                {
                    quadFurniture.Add(worldBeyondObjects[i].transform.GetChild(0));
                    doorsAndWindows.Add(worldBeyondObjects[i].gameObject);
                }
                else if (classification.Contains(OVRSceneManager.Classification.Ceiling))
                {
                    ceilingHeight = worldBeyondObjects[i].transform.position.y;
                }
            }

            // create special effect mesh
            if (SceneMesher)
            {
                try
                {
                    m_sceneMesh = SceneMesher.CreateSceneMesh(m_cornerPoints, cubeFurniture.ToArray(), quadFurniture.ToArray(), ceilingHeight);
                    // attach it to an anchor object so it sticks to the real world
                    m_sceneMesh.transform.parent = m_floorSceneAnchor.transform;
                    m_sceneMeshCreated = true;
                }
                catch
                {
                    Debug.Log($"[{nameof(VirtualRoom)}]: Scene meshing failed.");
                    m_sceneMeshCreated = false;
                }
            }
        }

        /// <summary>
        /// The saved walls may not be clockwise, and they may not be in order.
        /// </summary>
        private List<Vector3> GetClockwiseFloorOutline(WorldBeyondRoomObject[] allFurniture)
        {
            var cornerPoints = new List<Vector3>();

            if (null == m_floorSceneAnchor || !OVRPlugin.GetSpaceBoundary2D(m_floorSceneAnchor.Space, out var boundary))
            {
                // fall back to wall corner method
                var justWalls = new List<WorldBeyondRoomObject>();
                for (var i = 0; i < allFurniture.Length; i++)
                {
                    if (allFurniture[i].IsWall)
                    {
                        justWalls.Add(allFurniture[i]);
                    }
                }
                var seedWall = 0;
                for (var i = 0; i < justWalls.Count; i++)
                {
                    cornerPoints.Add(GetNextSplinePoint(ref seedWall, justWalls));
                }

                // Somehow, the number of walls in the floor outline is less than total walls. Data likely corrupt; prompt user to redo room.
                if (cornerPoints.Count < justWalls.Count)
                {
                    WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.ERROR_TOO_MANY_WALLS);
                }
            }
            else
            {
                // Use the Scence API and floor scene anchor to get the cornor of the floor, and convert Vector2 to Vector3
                cornerPoints = boundary.ToList()
                    .ConvertAll(corner => new Vector3(-corner.x, corner.y, 0.0f));

                // GetSpaceBoundary2D is in anchor-space
                cornerPoints.Reverse();
                for (var i = 0; i < cornerPoints.Count; i++)
                {
                    cornerPoints[i] = m_floorSceneAnchor.transform.TransformPoint(cornerPoints[i]);
                }
            }

            return cornerPoints;
        }

        /// <summary>
        /// Search for the next corner point for the clockwise room spline, using the wall transforms.
        /// </summary>
        private Vector3 GetNextSplinePoint(ref int thisID, List<WorldBeyondRoomObject> randomWalls)
        {
            var nextScale = randomWalls[thisID].GetComponent<WorldBeyondRoomObject>().Dimensions;

            var halfScale = nextScale * 0.5f;
            var bottomRight = randomWalls[thisID].transform.position - randomWalls[thisID].transform.up * halfScale.y - randomWalls[thisID].transform.right * halfScale.x;
            var closestCornerDistance = 100.0f;
            // When searching for a matching corner, the correct one should match positions. If they don't, assume there's a crack in the room.
            // This should be an impossible scenario and likely means broken data from Room Setup.
            const float DISTANCE_TOLERANCE = 0.1f;
            var nextCorner = Vector3.zero;
            var nextWallID = 0;
            for (var i = 0; i < randomWalls.Count; i++)
            {
                // compare to bottom left point of other walls
                if (i != thisID)
                {
                    var thisWallHalfScale = randomWalls[i].GetComponent<WorldBeyondRoomObject>().Dimensions * 0.5f;
                    var bottomLeft = randomWalls[i].transform.position - randomWalls[i].transform.up * thisWallHalfScale.y + randomWalls[i].transform.right * thisWallHalfScale.x;
                    var thisCornerDistance = Vector3.Distance(bottomLeft, bottomRight);
                    if (thisCornerDistance < closestCornerDistance)
                    {
                        closestCornerDistance = thisCornerDistance;
                        nextCorner = bottomLeft;
                        nextWallID = i;
                    }
                }
            }

            if (closestCornerDistance > DISTANCE_TOLERANCE)
            {
                WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.ERROR_ROOM_IS_OPEN);
            }

            thisID = nextWallID;
            return nextCorner;
        }

        /// <summary>
        /// Animate/reveal the glowing edges in the room.
        /// </summary>
        public void AnimateEffectMesh()
        {
            m_effectRadius = 2.0f;
            if (m_sceneMeshCreated)
            {
                m_sceneMesh.enabled = true;
                m_effectRadius = SceneMesher.GetRoomDiameter() * 0.4f;
                m_sceneMesh.material.SetVector("_EffectPosition", Vector3.up * 1000.0f);
                m_sceneMesh.material.SetFloat("_EffectRadius", m_effectRadius);
                m_sceneMesh.material.SetFloat("_EffectWidth", 0.1f);
                m_sceneMesh.material.SetFloat("_CeilingHeight", SceneMesher.CeilingHeight);
            }
            else
            {
                for (var i = 0; i < m_roomboxFurnishings.Count; i++)
                {
                    m_roomboxFurnishings[i].PassthroughMesh.material.SetVector("_EffectPosition", Vector3.up * 1000.0f);
                    m_roomboxFurnishings[i].PassthroughMesh.material.SetFloat("_EffectRadius", m_effectRadius);
                    m_roomboxFurnishings[i].PassthroughMesh.material.SetFloat("_EffectWidth", 0.1f);
                }

                for (var i = 0; i < m_roomboxWalls.Count; i++)
                {
                    m_roomboxWalls[i].PassthroughMesh.material.SetVector("_EffectPosition", Vector3.up * 1000.0f);
                    m_roomboxWalls[i].PassthroughMesh.material.SetFloat("_EffectRadius", m_effectRadius);
                    m_roomboxWalls[i].PassthroughMesh.material.SetFloat("_EffectWidth", 0.1f);
                }
            }
        }

        /// <summary>
        /// After the intro, we no longer need the effect mesh.
        /// </summary>
        public void HideEffectMesh()
        {
            if (m_sceneMeshCreated)
            {
                m_sceneMesh.enabled = false;
            }
        }

        /// <summary>
        /// Replace the prefab's quad with a custom polygon, for an exact floor outline.
        /// </summary>
        private void CreatePolygonMesh(List<WorldBeyondRoomObject> roomSurfaces, WorldBeyondRoomObject srObject, bool flipNormal)
        {
            try
            {
                // to avoid precision issues resulting in tiny cracks along the floor/ceiling edges, we want to extend out the polygon by a small amount
                for (var i = 0; i < m_cornerPoints.Count; i++)
                {
                    var startPos = m_cornerPoints[i];
                    var endPos = (i == m_cornerPoints.Count - 1) ? m_cornerPoints[0] : m_cornerPoints[i + 1];
                    var lastPos = (i == 0) ? m_cornerPoints[^1] : m_cornerPoints[i - 1];

                    var insetDirection = SceneMesher.GetInsetDirection(lastPos, startPos, endPos);
                    m_cornerPoints[i] = m_cornerPoints[i] - insetDirection * EDGE_BUFFER;
                }

                var polygonMesh = new Mesh();
                var vertices = new Vector3[m_cornerPoints.Count];
                var uvs = new Vector2[vertices.Length];
                var colors = new Color32[vertices.Length];
                var normals = new Vector3[vertices.Length];
                var tangents = new Vector4[vertices.Length];
                var triangles = new int[(m_cornerPoints.Count - 2) * 3];

                for (var i = 0; i < m_cornerPoints.Count; i++)
                {
                    // transform vertex positions first
                    var offsetPoint = new Vector3(m_cornerPoints[i].x, srObject.PassthroughMesh.transform.position.y, m_cornerPoints[i].z);

                    vertices[i] = srObject.PassthroughMesh.transform.InverseTransformPoint(offsetPoint);
                    uvs[i] = new Vector2(m_cornerPoints[i].x, m_cornerPoints[i].z);
                    colors[i] = Color.white;
                    normals[i] = -Vector3.forward;
                    tangents[i] = new Vector4(1, 0, 0, 1);
                }

                // triangulate
                var points2d = new List<Vector2>(m_cornerPoints.Count);
                for (var i = 0; i < m_cornerPoints.Count; i++)
                {
                    points2d.Add(new Vector2(m_cornerPoints[i].x, m_cornerPoints[i].z));
                }

                var triangulator = new Triangulator(points2d.ToArray());
                var indices = triangulator.Triangulate();
                var indexCounter = 0;
                for (var j = 0; j < m_cornerPoints.Count - 2; j++)
                {
                    var id0 = indices[j * 3];
                    var id1 = indices[j * 3 + 1];
                    var id2 = indices[j * 3 + 2];

                    triangles[indexCounter++] = id0;
                    triangles[indexCounter++] = flipNormal ? id2 : id1;
                    triangles[indexCounter++] = flipNormal ? id1 : id2;
                }


                // assign
                polygonMesh.Clear();
                polygonMesh.name = "polygonMesh";
                polygonMesh.vertices = vertices;
                polygonMesh.uv = uvs;
                polygonMesh.colors32 = colors;
                polygonMesh.normals = normals;
                polygonMesh.tangents = tangents;
                polygonMesh.triangles = triangles;
                if (srObject.PassthroughMesh.gameObject.GetComponent<MeshFilter>())
                {
                    srObject.PassthroughMesh.gameObject.GetComponent<MeshFilter>().mesh = polygonMesh;
                }
            }
            catch (IndexOutOfRangeException exception)
            {
                Debug.LogError("Error parsing walls, are the walls intersecting? " + exception.Message);
                WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.ERROR_INTERSECTING_WALLS);
            }
        }

        /// <summary>
        /// Create and initialize the 4 particle/border effects for each wall.
        /// </summary>
        private void CreateWallBorderEffects(WorldBeyondRoomObject wallObject)
        {
            for (var j = 0; j < 4; j++)
            {
                var frame = Instantiate(EdgePrefab);
                // rotate each edge 90 degrees around the wall, starting from the top
                // even is Y axis, odd is X axis
                frame.transform.rotation = wallObject.transform.rotation * Quaternion.Euler(0, 0, 90 * j);
                var scl = wallObject.Dimensions;
                var extent = (j % 2 == 0) ? scl.y * 0.5f : scl.x * 0.5f;
                frame.transform.position = wallObject.transform.position + frame.transform.up * extent;
                var width = (j % 2 == 1) ? scl.y : scl.x;
                frame.transform.localScale = new Vector3(1, 1.0f / scl.y, 1.0f / scl.z);

                var thisWallEdge = frame.GetComponent<WallEdge>();
                thisWallEdge.ParentSurface = wallObject;
                thisWallEdge.AdjustParticleSystemRateAndSize(width);
                thisWallEdge.transform.parent = wallObject.transform;
                wallObject.WallEdges.Add(thisWallEdge);
            }
        }

        /// <summary>
        /// Create and initialize the N particle/border effects for the floor/ceiling.
        /// </summary>
        private void CreateFloorCeilingBorderEffects()
        {
            // for ceiling & floor, still use walls to calculate edge transform, but ensure edge fades "belong" to ceiling/floor
            var ceilingEdges = new List<WallEdge>();
            var floorEdges = new List<WallEdge>();

            // make the edge frills for the floor/ceiling
            // different than the walls because floorplan isn't always a quad
            for (var i = 0; i < m_roomboxWalls.Count; i++)
            {
                if (!m_roomboxWalls[i].IsWall)
                {
                    continue;
                }
                var floorFrame = Instantiate(EdgePrefab);
                var ceilingFrame = Instantiate(EdgePrefab);
                var refTransform = m_roomboxWalls[i].PassthroughMesh.transform;
                var wallOut = -m_roomboxWalls[i].transform.forward;
                var worldUp = Vector3.up;
                Vector3.OrthoNormalize(ref wallOut, ref worldUp);
                floorFrame.transform.rotation = Quaternion.LookRotation(worldUp, wallOut);
                ceilingFrame.transform.rotation = Quaternion.LookRotation(-worldUp, wallOut);

                floorFrame.transform.localScale = new Vector3(refTransform.localScale.x, floorFrame.transform.localScale.y, floorFrame.transform.localScale.z);
                ceilingFrame.transform.localScale = floorFrame.transform.localScale;

                floorFrame.transform.position = refTransform.position - refTransform.up * refTransform.localScale.y * 0.5f;
                ceilingFrame.transform.position = refTransform.position + refTransform.up * refTransform.localScale.y * 0.5f;

                var floorEdge = floorFrame.GetComponent<WallEdge>();
                floorEdge.ParentSurface = m_roomboxWalls[m_roomFloorID];
                floorEdge.AdjustParticleSystemRateAndSize(refTransform.localScale.x);
                floorEdge.transform.parent = m_roomboxWalls[m_roomFloorID].transform;
                floorEdges.Add(floorEdge);

                var ceilingEdge = ceilingFrame.GetComponent<WallEdge>();
                ceilingEdge.ParentSurface = m_roomboxWalls[m_roomCeilingID];
                ceilingEdge.AdjustParticleSystemRateAndSize(refTransform.localScale.x);
                ceilingEdge.transform.parent = m_roomboxWalls[m_roomCeilingID].transform;
                ceilingEdges.Add(ceilingEdge);
            }

            m_roomboxWalls[m_roomCeilingID].WallEdges.AddRange(ceilingEdges);
            m_roomboxWalls[m_roomFloorID].WallEdges.AddRange(floorEdges);
        }

        /// <summary>
        /// Grass shrubs around the base of the wall that appear when it's removed.
        /// </summary>
        private void CreateWallDebris(WorldBeyondRoomObject wallObject)
        {
            // debris along the ground
            var debrisCount = Random.Range(6, 10);
            for (var i = 0; i < debrisCount; i++)
            {
                var scl = wallObject.Dimensions;
                var debris = Instantiate(WorldBeyondEnvironment.Instance.GrassPrefab);
                var randomPosition = wallObject.transform.position - Vector3.up * scl.y * 0.5f;
                var posToEdge = Random.Range(-0.5f, 0.5f);
                if (i <= 1)
                {
                    // put the first two towards the edge, for effect
                    posToEdge = Random.Range(0.4f, 0.5f) * (i - 0.5f) * 2;
                }
                randomPosition += wallObject.transform.right * scl.x * posToEdge;
                randomPosition += wallObject.transform.forward * Random.Range(-0.2f, 0.1f);
                _ = Vector3.one * Random.Range(0.8f, 1.2f) * (Mathf.Abs(posToEdge) + 0.5f);
                debris.transform.position = randomPosition;
                debris.transform.localScale = Vector3.zero;
                debris.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360.0f), 0);
                wallObject.WallDebris.Add(debris);
            }
        }

        /// <summary>
        /// Given a wall, set it up to work with Navmesh
        /// </summary>
        private void CreateNavMeshObstacle(WorldBeyondRoomObject wallObject)
        {
            if (wallObject.PassthroughMesh && wallObject.PassthroughMesh.gameObject.GetComponent<NavMeshObstacle>() == null)
            {
                var obstacle = wallObject.PassthroughMesh.gameObject.AddComponent<NavMeshObstacle>();
                obstacle.carving = true;
                obstacle.shape = NavMeshObstacleShape.Box;
                obstacle.size = new Vector3(1, 1, 0.1f);
                obstacle.center = Vector3.zero;
            }
        }

        /// <summary>
        /// Grass shrubs that surround the bases of furniture, that hide if all walls are closed.
        /// </summary>
        private void CreateFurnitureDebris(Transform child, Vector3 scl)
        {
            var basePos = new Vector3(child.position.x, m_floorHeight, child.position.z);
            for (var j = 0; j < 12; j++)
            {
                var debris = Instantiate(WorldBeyondEnvironment.Instance.GrassPrefab);
                var side = j % 2 - 0.5f;
                var debrisDiameter = 0.15f;
                var randomPos = Random.Range(-0.5f, 0.5f);

                var rndXsign = Mathf.Sign(Random.Range(0, 2) - 1);
                var rndYsign = Mathf.Sign(Random.Range(0, 2) - 1);

                debris.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360.0f), 0);
                debris.transform.position = basePos;
                var boxRot = child.rotation;
                var localOffset1 = new Vector3(side * scl.x * rndXsign - side * debrisDiameter, randomPos * scl.y * rndYsign, 0);
                var localOffset2 = new Vector3(randomPos * scl.x * rndXsign, side * scl.y * rndYsign - side * debrisDiameter, 0);
                var offset = (side < 0) ? boxRot * localOffset1 : boxRot * localOffset2;
                debris.transform.position += offset;
                debris.transform.localScale = Vector3.one * Random.Range(0.3f, 0.7f);
                m_roomDebris.Add(debris);
            }
        }

        /// <summary>
        /// Assign parameters to the room's starry material, hinting to the player where they should shine the flashlight.
        /// </summary>
        public void SetWallEffectParams(Vector3 objPosition, float effectStrength, float ballMaskStrength)
        {
            for (var i = 0; i < m_roomboxWalls.Count; i++)
            {
                m_roomboxWalls[i].PassthroughMesh.material.SetVector("_OppyPosition", objPosition);
                m_roomboxWalls[i].PassthroughMesh.material.SetFloat("_OppyRippleStrength", effectStrength);
                m_roomboxWalls[i].PassthroughMesh.material.SetFloat("_MaskRippleStrength", ballMaskStrength);
            }
            for (var i = 0; i < m_roomboxFurnishings.Count; i++)
            {
                m_roomboxFurnishings[i].PassthroughMesh.material.SetVector("_OppyPosition", objPosition);
                m_roomboxFurnishings[i].PassthroughMesh.material.SetFloat("_OppyRippleStrength", effectStrength);
                m_roomboxFurnishings[i].PassthroughMesh.material.SetFloat("_MaskRippleStrength", ballMaskStrength);
            }
        }

        /// <summary>
        /// As Oppy is discovered, the world gets more and more saturated.
        /// </summary>
        public void SetRoomSaturation(float roomSat)
        {
            for (var i = 0; i < m_roomboxWalls.Count; i++)
            {
                m_roomboxWalls[i].PassthroughMesh.material.SetFloat("_SaturationAmount", roomSat);
            }
            for (var i = 0; i < m_roomboxFurnishings.Count; i++)
            {
                m_roomboxFurnishings[i].PassthroughMesh.material.SetFloat("_SaturationAmount", roomSat);
            }
        }

        /// <summary>
        /// A fallback Position to spawn things in the room, in case raycasting fails.
        /// </summary>
        public Vector3 GetSimpleFloorPosition()
        {
            if (m_roomboxWalls == null) return Vector3.zero;
            if (m_roomboxWalls.Count < 2) return Vector3.zero;

            // to be extra safe, spawn it wherever the player is
            var floor = m_roomboxWalls[m_roomFloorID];
            if (!floor) return Vector3.zero;
            var floorPos = new Vector3(WorldBeyondManager.Instance.MainCamera.transform.position.x, floor.transform.position.y, WorldBeyondManager.Instance.MainCamera.transform.position.z);
            return floorPos;
        }

        public bool GetOutdoorAudioPosition(ref Vector3 audioPos)
        {
            var wallOpen = false;
            var averageOpenWallPosition = Vector3.up;
            for (var i = 0; i < m_roomboxWalls.Count; i++)
            {
                if (!m_roomboxWalls[i].PassthroughWallActive)
                {
                    averageOpenWallPosition += m_roomboxWalls[i].transform.position;
                    wallOpen = true;
                }
            }
            audioPos = averageOpenWallPosition.normalized * 10.0f;
            return wallOpen;
        }

        /// <summary>
        /// For audio, test if anything is blocking the sound,
        /// </summary>
        public bool IsBlockedByWall(Ray directionToSound, float distance)
        {
            var isBlocked = false;
            LayerMask soundObstructionLayer = LayerMask.GetMask("Default");
            var roomboxHit = Physics.RaycastAll(directionToSound, distance, soundObstructionLayer);
            // just testing for yes/no, so there's no need for additional data
            if (roomboxHit != null && roomboxHit.Length > 0)
            {
                isBlocked = true;
            }
            return isBlocked;
        }

        /// <summary>
        /// Same as getting the height of any wall.
        /// </summary>
        public float GetCeilingHeight()
        {
            return m_wallHeight;
        }

        /// <summary>
        /// Mute the audio depending on how "open" the room is (how many passthrough walls are off)
        /// </summary>
        public float GetRoomOpenAmount()
        {
            if (m_roomboxWalls == null || m_roomboxWalls.Count == 0)
            {
                return 1f;
            }

            // give the ceiling 50% Weight
            var totalAmount = m_roomboxWalls[^1].PassthroughWallActive ? 0.0f : 0.5f;

            var wallsOpen = 0;
            for (var i = 0; i < m_roomboxWalls.Count - 2; i++) // ignore last two walls (floor and ceiling)
            {
                if (!m_roomboxWalls[i].PassthroughWallActive)
                {
                    wallsOpen++;
                }
            }
            // the other 50% is a percentage of the remaining walls
            totalAmount += 0.5f * (wallsOpen / (float)(m_roomboxWalls.Count - 2));

            return totalAmount;
        }

        /// <summary>
        /// For the intro room box glowing lines.
        /// </summary>
        public void SetEdgeEffectIntensity(float normValue)
        {
            if (m_sceneMeshCreated)
            {
                var intensity = EdgeIntensity.Evaluate(normValue);
                m_sceneMesh.material.SetFloat("_EffectIntensity", intensity);
                m_sceneMesh.material.SetFloat("_EdgeTimeline", normValue);
            }
        }

        /// <summary>
        /// When the roombox mesh is getting illuminated by the Multitoy, assign them to the material.
        /// </summary>
        public void SetEffectPosition(Vector3 worldPos, float timer)
        {
            if (m_sceneMeshCreated)
            {
                m_sceneMesh.material.SetVector("_EffectPosition", worldPos);
                m_sceneMesh.material.SetFloat("_EffectRadius", timer * m_effectRadius);
            }
            else
            {
                for (var i = 0; i < m_roomboxFurnishings.Count; i++)
                {
                    m_roomboxFurnishings[i].PassthroughMesh.material.SetVector("_EffectPosition", worldPos);
                    m_roomboxFurnishings[i].PassthroughMesh.material.SetFloat("_EffectRadius", timer * m_effectRadius);
                }

                for (var i = 0; i < m_roomboxWalls.Count; i++)
                {
                    m_roomboxWalls[i].PassthroughMesh.material.SetVector("_EffectPosition", worldPos);
                    m_roomboxWalls[i].PassthroughMesh.material.SetFloat("_EffectRadius", timer * m_effectRadius);
                }
            }
        }

        /// <summary>
        /// Simple bounding-box test to see if a Position (with buffer radius) is in the room. Ignores height.
        /// </summary>
        public bool IsPositionInRoom(Vector3 pos, float positionBuffer, bool alsoCheckVertical = false)
        {
            var xMin = 0.0f;
            var xMax = 0.0f;
            var zMin = 0.0f;
            var zMax = 0.0f;
            var anyWall = 0;
            for (var i = 0; i < m_roomboxWalls.Count; i++)
            {
                if (!m_roomboxWalls[i].IsWall)
                {
                    continue;
                }
                anyWall = i;
                var wallRight = -m_roomboxWalls[i].transform.right;
                var wallScale = m_roomboxWalls[i].PassthroughMesh.transform.localScale;
                var pos1 = m_roomboxWalls[i].transform.position - wallRight * wallScale.x * 0.5f;
                _ = m_roomboxWalls[i].transform.position + wallRight * wallScale.x * 0.5f;
                if (pos1.x < xMin) xMin = pos1.x;
                if (pos1.x > xMax) xMax = pos1.x;
                if (pos1.z < zMin) zMin = pos1.z;
                if (pos1.z > zMax) zMax = pos1.z;
            }
            var inRoom = pos.x > xMin - positionBuffer && pos.x < xMax + positionBuffer && pos.z > zMin - positionBuffer && pos.z < zMax + positionBuffer;
            if (alsoCheckVertical)
            {
                var floorPos = (m_roomboxWalls[anyWall].transform.position - Vector3.up * m_roomboxWalls[anyWall].PassthroughMesh.transform.localScale.y * 0.5f).y;
                var ceilingPos = (m_roomboxWalls[anyWall].transform.position + Vector3.up * m_roomboxWalls[anyWall].PassthroughMesh.transform.localScale.y * 0.5f).y;
                inRoom &= pos.y > floorPos + positionBuffer;
                inRoom &= pos.y < ceilingPos - positionBuffer;
            }
            return inRoom;
        }

        /// <summary>
        /// Point-in-polygon test to see if Position is in room
        /// </summary>
        public bool IsPlayerInRoom()
        {
            var cameraPos = WorldBeyondManager.Instance.MainCamera.transform.position;
            cameraPos = new Vector3(cameraPos.x, m_cornerPoints[0].y, cameraPos.z);
            // Shooting a ray from player to the right (X+), count how many walls it intersects.
            // If the count is odd, the player is in the room
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
                if (cameraPos.z <= zMax &&
                    cameraPos.z >= zMin)
                {
                    if (cameraPos.x <= xMin)
                    {
                        // it's completely to the left of this line segment's bounds, so must intersect
                        lineCrosses++;
                    }
                    else if (cameraPos.x < xMax)
                    {
                        // it's within the bounds, so further calculation is needed
                        var lineVec = (highestPoint - lowestPoint).normalized;
                        var camVec = (cameraPos - lowestPoint).normalized;
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

        /// <summary>
        /// Check if this surface is the floor.
        /// </summary>
        public bool IsFloor(int surfaceID)
        {
            return surfaceID == m_roomFloorID;
        }

        /// <summary>
        /// During the intro, apply a different material to the room
        /// </summary>
        public void ShowDarkRoom(bool doShow)
        {
            // if using the sceneMesh, this function is unnecessary
            if (m_sceneMeshCreated)
            {
                return;
            }
            if (doShow)
            {
                ShowAllWalls(true);
            }

            for (var i = 0; i < m_roomboxFurnishings.Count; i++)
            {
                m_roomboxFurnishings[i].ShowDarkRoomMaterial(doShow);
            }

            for (var i = 0; i < m_roomboxWalls.Count; i++)
            {
                m_roomboxWalls[i].ShowDarkRoomMaterial(doShow);
            }
        }
    }
}
