// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using Meta.XR.MRUtilityKit;
using TheWorldBeyond.Audio;
using TheWorldBeyond.GameManagement;
using TheWorldBeyond.Toy;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace TheWorldBeyond.Environment.RoomEnvironment
{
    public class VirtualRoom : MonoBehaviour
    {
        [SerializeField] private EffectMesh EffectMeshForIntroEffect;
        [SerializeField] protected internal EffectMesh EffectMeshForFloorCeiling;
        public static VirtualRoom Instance = null;

        public GameObject EdgePrefab;
        private bool m_roomOpen = false;
        private List<WorldBeyondRoomObject> m_roomboxFurnishings = new();
        private List<WorldBeyondRoomObject> m_roomboxWalls = new();
        public SceneMesher SceneMesher;
        private bool m_sceneMeshCreated = false;

        // if an anchor is within this angle tolerance, snap it to be Gravity-aligned
        private float m_alignmentAngleThreshold = 5.0f;

        // drop the virtual world this far below the floor anchor
        private const float GROUND_DELTA = 0.01f;

        private List<GameObject> m_roomDebris = new();
        private MeshRenderer m_sceneMesh = null;
        private float m_effectRadius = 10.0f;
        public AnimationCurve EdgeIntensity;
        private MRUKAnchor m_floorMRUKAnchor = null;
        private MRUKAnchor m_ceilingMRUKAnchor = null;
        private WorldBeyondRoomObject m_floorWBRO = null;
        private WorldBeyondRoomObject m_ceilingWBRO = null;

        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
            }
        }

        public Bounds GetObjBounds(GameObject prefab)
        {
            Bounds bounds = new Bounds();
            foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>())
            {
                bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }

        public bool IsPositionInPolygon(Vector2 position, Vector2 bounds, List<Vector2> polygon)
        {
            // Calculate the corners of the bounding box
            Vector2 topLeft = position - bounds / 2;
            Vector2 topRight = new Vector2(position.x + bounds.x / 2, position.y - bounds.y / 2);
            Vector2 bottomLeft = new Vector2(position.x - bounds.x / 2, position.y + bounds.y / 2);
            Vector2 bottomRight = position + bounds / 2;

            // Check if any corner of the bounding box is inside the polygon
            return IsPointInPolygon(topLeft, polygon) ||
                   IsPointInPolygon(topRight, polygon) ||
                   IsPointInPolygon(bottomLeft, polygon) ||
                   IsPointInPolygon(bottomRight, polygon) ||
                   IsEdgeIntersectingPolygon(topLeft, topRight, polygon) ||
                   IsEdgeIntersectingPolygon(topRight, bottomRight, polygon) ||
                   IsEdgeIntersectingPolygon(bottomRight, bottomLeft, polygon) ||
                   IsEdgeIntersectingPolygon(bottomLeft, topLeft, polygon);
        }

        private static bool IsPointInPolygon(Vector2 point, List<Vector2> polygon)
        {
            int lineCrosses = 0;
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 p1 = polygon[i];
                Vector2 p2 = polygon[(i + 1) % polygon.Count];

                if (point.y > Mathf.Min(p1.y, p2.y) && point.y <= Mathf.Max(p1.y, p2.y))
                {
                    if (point.x <= Mathf.Max(p1.x, p2.x))
                    {
                        if (p1.y != p2.y)
                        {
                            var frac = (point.y - p1.y) / (p2.y - p1.y);
                            var xIntersection = p1.x + frac * (p2.x - p1.x);
                            if (p1.x == p2.x || point.x <= xIntersection)
                            {
                                lineCrosses++;
                            }
                        }
                    }
                }
            }

            return (lineCrosses % 2) == 1;
        }

        private static bool IsEdgeIntersectingPolygon(Vector2 p1, Vector2 p2, List<Vector2> polygon)
        {
            foreach (var edge in GetEdges(polygon))
            {
                if (AreEdgesIntersecting(p1, p2, edge.Item1, edge.Item2))
                {
                    return true;
                }
            }

            return false;
        }

        private static Tuple<Vector2, Vector2>[] GetEdges(List<Vector2> polygon)
        {
            Tuple<Vector2, Vector2>[] edges = new Tuple<Vector2, Vector2>[polygon.Count];
            for (int i = 0; i < polygon.Count; i++)
            {
                edges[i] = new Tuple<Vector2, Vector2>(polygon[i], polygon[(i + 1) % polygon.Count]);
            }

            return edges;
        }

        private static bool AreEdgesIntersecting(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        {
            float denominator = (p1.x - p2.x) * (p3.y - p4.y) - (p1.y - p2.y) * (p3.x - p4.x);
            if (denominator == 0)
            {
                return false; // lines are parallel
            }
            float t = ((p1.x - p3.x) * (p3.y - p4.y) - (p1.y - p3.y) * (p3.x - p4.x)) / denominator;
            float u = -((p1.x - p2.x) * (p1.y - p3.y) - (p1.y - p2.y) * (p1.x - p3.x)) / denominator;
            return t > 0 && t < 1 && u > 0 && u < 1;
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

        public void InitializeMRUK()
        {
            var anchorsAsWBRO = new List<WorldBeyondRoomObject>();
            var doorsAndWindows = new List<GameObject>();

            m_floorMRUKAnchor = MRUK.Instance.GetCurrentRoom().FloorAnchor;
            m_ceilingMRUKAnchor = MRUK.Instance.GetCurrentRoom().CeilingAnchor;
            var anchors = MRUK.Instance.GetCurrentRoom().Anchors;

            for (var i = 0; i < anchors.Count; i++)
            {
                var anchor = anchors[i];
                var wbro = anchor.gameObject.GetComponentInChildren<WorldBeyondRoomObject>();
                if (wbro == null)
                {
                    //unsupported, new type for TWB (e.g. global mesh or hi-fi-stuff)
                    continue;
                }
                anchorsAsWBRO.Add(wbro);

                if (anchor.Label is MRUKAnchor.SceneLabels.WALL_FACE or MRUKAnchor.SceneLabels.INVISIBLE_WALL_FACE or MRUKAnchor.SceneLabels.FLOOR or MRUKAnchor.SceneLabels.CEILING)
                {
                    wbro.SurfaceID = m_roomboxWalls.Count;
                    m_roomboxWalls.Add(wbro);

                    if (anchor.Label is MRUKAnchor.SceneLabels.WALL_FACE or MRUKAnchor.SceneLabels.INVISIBLE_WALL_FACE)
                    {
                        wbro.IsWall = true;
                        CreateWallBorderEffects(wbro);
                        CreateWallDebris(wbro);
                        CreateNavMeshObstacle(wbro);
                    }
                    if (wbro.GetComponent<BoxCollider>())
                    {
                        wbro.GetComponent<BoxCollider>().size = new Vector3(transform.localScale.x, transform.localScale.y, 0.01f);
                    }
                }
                else
                {
                    if (anchor.Label is MRUKAnchor.SceneLabels.DOOR_FRAME or MRUKAnchor.SceneLabels.WINDOW_FRAME
                        or MRUKAnchor.SceneLabels.WALL_ART)
                    {
                        doorsAndWindows.Add(anchor.gameObject);
                    }
                    else
                    {
                        wbro.IsFurniture = true;
                        m_roomboxFurnishings.Add(wbro);

                        var bc = wbro.PassthroughMesh.GetComponent<BoxCollider>();
                        if (bc && bc.gameObject.GetComponent<NavMeshObstacle>() == null)
                        {
                            var obstacle = bc.gameObject.AddComponent<NavMeshObstacle>();
                            obstacle.carving = true;
                            obstacle.shape = NavMeshObstacleShape.Box;
                            obstacle.size = Vector3.one;
                            obstacle.center = bc.center;
                        }

                        CreateFurnitureDebrisMRUK(anchor.transform, anchor.transform.localScale);
                    }
                }
                switch (anchor.Label)
                {
                    case MRUKAnchor.SceneLabels.CEILING:
                        m_ceilingWBRO = wbro;
                        break;
                    case MRUKAnchor.SceneLabels.FLOOR:
                        m_floorWBRO = wbro;
                        WorldBeyondManager.Instance.MoveGroundFloor(m_floorWBRO.transform.position.y - GROUND_DELTA);
                        break;
                }
            }

            if (m_roomboxWalls.Count < 5)
            {
                WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.ERROR_NOT_ENOUGH_WALLS);
            }


            m_ceilingWBRO.PassthroughMesh.gameObject.GetComponent<MeshFilter>().mesh =
                EffectMeshForFloorCeiling.EffectMeshObjects[m_ceilingMRUKAnchor].mesh;
            m_floorWBRO.PassthroughMesh.gameObject.GetComponent<MeshFilter>().mesh =
                EffectMeshForFloorCeiling.EffectMeshObjects[m_floorMRUKAnchor].mesh;
            var rotation = new Quaternion(0, 0, 0, 0);
            m_ceilingWBRO.PassthroughMesh.transform.localRotation = rotation;
            m_floorWBRO.PassthroughMesh.transform.localRotation = rotation;

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

            CreateSpecialEffectMesh();

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
        public void CreateSpecialEffectMesh()
        {
            // create special effect mesh
            if (SceneMesher)
            {
                try
                {
                    m_sceneMesh = SceneMesher.CreateSceneMesh(EffectMeshForIntroEffect, GetCeilingHeight());
                    // attach it to an anchor object so it sticks to the real world
                    m_sceneMesh.transform.parent = m_floorMRUKAnchor.transform;
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
                var refTransform = m_roomboxWalls[i].transform;
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
                floorEdge.ParentSurface = m_floorWBRO;
                floorEdge.AdjustParticleSystemRateAndSize(refTransform.localScale.x);
                floorEdge.transform.parent = m_floorWBRO.transform;
                floorEdges.Add(floorEdge);

                var ceilingEdge = ceilingFrame.GetComponent<WallEdge>();
                ceilingEdge.ParentSurface = m_ceilingWBRO;
                ceilingEdge.AdjustParticleSystemRateAndSize(refTransform.localScale.x);
                ceilingEdge.transform.parent = m_ceilingWBRO.transform;
                ceilingEdges.Add(ceilingEdge);
            }

            m_ceilingWBRO.WallEdges.AddRange(ceilingEdges);
            m_floorWBRO.WallEdges.AddRange(floorEdges);
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
        private void CreateFurnitureDebrisMRUK(Transform child, Vector3 scl)
        {
            var basePos = new Vector3(child.position.x, m_floorMRUKAnchor.transform.position.y, child.position.z);
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
            var floor = m_floorMRUKAnchor;
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
            return m_ceilingMRUKAnchor.transform.position.y - m_floorMRUKAnchor.transform.position.y;
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
        /// Point-in-polygon test to see if Position is in room
        /// </summary>
        public bool IsPlayerInRoom()
        {
            var cameraPos = WorldBeyondManager.Instance.MainCamera.transform.position;
            return MRUK.Instance.GetCurrentRoom().IsPositionInRoom(cameraPos);
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
