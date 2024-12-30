// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class VirtualRoom : MonoBehaviour
{
    static public VirtualRoom Instance = null;

    public GameObject _anchorPrefab;
    public GameObject _edgePrefab;
    bool _roomOpen = false;
    List<WorldBeyondRoomObject> _roomboxFurnishings = new List<WorldBeyondRoomObject>();
    List<WorldBeyondRoomObject> _roomboxWalls = new List<WorldBeyondRoomObject>();
    public SceneMesher _sceneMesher;
    bool _sceneMeshCreated = false;
    List<Vector3> _cornerPoints = new List<Vector3>();
    int _roomFloorID;
    int _roomCeilingID;
    float _floorHeight = 0.0f;
    float _wallHeight = 3.0f;

    // if an anchor is within this angle tolerance, snap it to be gravity-aligned
    float _alignmentAngleThreshold = 5.0f;

    // drop the virtual world this far below the floor anchor
    const float _groundDelta = 0.01f;

    // because of anchor imprecision, the room isn't watertight.
    // this extends each surface to hide the cracks.
    const float _edgeBuffer = 0.02f;

    List<GameObject> _roomDebris = new List<GameObject>();

    MeshRenderer _sceneMesh = null;
    float _effectRadius = 10.0f;
    public AnimationCurve _edgeIntensity;
    OVRSceneAnchor _floorSceneAnchor = null;

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
        StartCoroutine(CloseWallDelayed(wallID, doClose));
    }

    IEnumerator CloseWallDelayed(int wallID, bool doClose)
    {
        float waitTime = doClose ? 0.0f : 1.0f;
        // if opening a wall, show the neighboring edges immediately
        if (!doClose)
        {
            for (int j = 0; j < _roomboxWalls[wallID].wallEdges.Count; j++)
            {
                WallEdge edge = _roomboxWalls[wallID].wallEdges[j];
                if (edge._siblingEdge._parentSurface._passthroughWallActive)
                {
                    edge._siblingEdge.ShowEdge(true);
                }
            }
        }
        yield return new WaitForSeconds(waitTime);

        // if closing a wall, delay hiding the neighboring edges
        _roomboxWalls[wallID]._passthroughMesh.gameObject.SetActive(doClose);

        for (int j = 0; j < _roomboxWalls[wallID].wallEdges.Count; j++)
        {
            WallEdge edge = _roomboxWalls[wallID].wallEdges[j];
            if (doClose)
            {
                if (!edge._siblingEdge._parentSurface._passthroughWallActive)
                {
                    // only show edge if neighboring wall is open
                    edge.ShowEdge(true);
                }
                else
                {
                    // if neighboring wall is closed, turn off both edges
                    edge._siblingEdge.ShowEdge(false);
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
        AudioManager.SetRoomOpenness(VirtualRoom.Instance.GetRoomOpenAmount());
    }

    /// <summary>
    /// If the room is completely closed, hide the grass around furniture and walls.
    /// </summary>
    void CheckForClosedWalls()
    {
        // if all walls are closed, hide room grass
        bool wallOpen = false;
        for (int i = 0; i < _roomboxWalls.Count; i++)
        {
            if (_roomboxWalls[i]._isWall && !_roomboxWalls[i]._passthroughWallActive)
            {
                wallOpen = true;
            }
        }

        if (_roomOpen != wallOpen)
        {
            StartCoroutine(GrowGrass(wallOpen));
            _roomOpen = wallOpen;
        }
    }

    /// <summary>
    /// For grass inside the room, grow/shrink it if any walls are open/closed.
    /// </summary>
    IEnumerator GrowGrass(bool doGrow)
    {
        float timer = 0.0f;
        float growTime = 1.0f;
        while (timer <= growTime)
        {
            timer += Time.deltaTime;
            float smoothTimer = Mathf.Cos(Mathf.PI * Mathf.Clamp01(timer / growTime)) * 0.5f + 0.5f;
            smoothTimer = doGrow ? (1 - smoothTimer) : smoothTimer;
            foreach (GameObject obj in _roomDebris)
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
        for (int i = 0; i < _roomboxFurnishings.Count; i++)
        {
            _roomboxFurnishings[i]._passthroughMesh.gameObject.SetActive(doShow);
        }

        for (int i = 0; i < _roomboxWalls.Count; i++)
        {
            _roomboxWalls[i]._passthroughMesh.gameObject.SetActive(doShow);

            WorldBeyondRoomObject sro = _roomboxWalls[i];
            for (int j = 0; j < sro.wallEdges.Count; j++)
            {
                sro.wallEdges[j].ShowEdge(false);
                if (!doShow)
                {
                    sro.wallEdges[j].ClearEdgeParticles();
                }
            }

            for (int j = 0; j < _roomboxWalls[i].wallDebris.Count; j++)
            {
                _roomboxWalls[i].wallDebris[j].transform.localScale = Vector3.zero;
            }

            if (doShow)
            {
                _roomboxWalls[i].ForcePassthroughMaterial();
            }
        }

        foreach (GameObject child in _roomDebris)
        {
            child.transform.localScale = Vector3.zero;
        }

        _roomOpen = !doShow;
        AudioManager.SetRoomOpenness(VirtualRoom.Instance.GetRoomOpenAmount());
    }

    void SetChildrenScale(Transform parentTransform, Vector3 childScale)
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
        _roomboxWalls.Clear();

        List<WorldBeyondRoomObject> anchorsAsWBRO = new List<WorldBeyondRoomObject>();
        List<GameObject> doorsAndWindows = new List<GameObject>();
        for (int i = 0; i < sceneAnchors.Length; i++)
        {
            OVRSceneAnchor instance = sceneAnchors[i];
            if (instance.GetComponent<OVRSceneRoom>())
            {
                continue;
            }
            OVRSemanticClassification classification = instance.GetComponent<OVRSemanticClassification>();
            if (classification) Debug.Log(string.Format("TWB Anchor {0}: {1}", i, classification.Labels[0]));
            WorldBeyondRoomObject wbro = instance.GetComponent<WorldBeyondRoomObject>();
            anchorsAsWBRO.Add(wbro);
            wbro._dimensions = instance.transform.GetChild(0).localScale;
            if (classification.Contains(OVRSceneManager.Classification.WallFace) ||
                classification.Contains(OVRSceneManager.Classification.Floor) ||
                classification.Contains(OVRSceneManager.Classification.Ceiling))
            {
                // force-flatten it so there's no virtual/passthrough intersections
                instance.transform.rotation = GravityAligner.GetAlignedOrientation(instance.transform.rotation, _alignmentAngleThreshold);

                wbro._surfaceID = _roomboxWalls.Count;

                if (classification.Contains(OVRSceneManager.Classification.Floor))
                {
                    _roomFloorID = _roomboxWalls.Count;

                    // move the world slightly below the ground floor, so the virtual floor doesn't Z-fight
                    WorldBeyondManager.Instance.MoveGroundFloor(instance.transform.position.y - _groundDelta);
                    _floorHeight = instance.transform.position.y;
                    _floorSceneAnchor = instance;
                }
                else if (classification.Contains(OVRSceneManager.Classification.Ceiling))
                {
                    _roomCeilingID = _roomboxWalls.Count;
                }
                _roomboxWalls.Add(wbro);

                if (classification.Contains(OVRSceneManager.Classification.WallFace))
                {
                    wbro._isWall = true;

                    CreateWallBorderEffects(wbro);
                    CreateWallDebris(wbro);
                    CreateNavMeshObstacle(wbro);
                }

                // the collider on the root anchor object is only used by the wall toy
                if (wbro.GetComponent<BoxCollider>())
                {
                    wbro.GetComponent<BoxCollider>().size = new Vector3(wbro._dimensions.x, wbro._dimensions.y, 0.01f);
                }
            }
            else if (instance.GetComponent<OVRSceneVolume>())
            {
                wbro._isFurniture = true;
                _roomboxFurnishings.Add(wbro);

                // add as navmesh obstacles
                BoxCollider bc = wbro._passthroughMesh.GetComponent<BoxCollider>();
                if (bc && bc.gameObject.GetComponent<NavMeshObstacle>() == null)
                {
                    NavMeshObstacle obstacle = bc.gameObject.AddComponent<NavMeshObstacle>();
                    obstacle.carving = true;
                    obstacle.shape = NavMeshObstacleShape.Box;
                    obstacle.size = Vector3.one;
                    obstacle.center = bc.center;
                }

                // the collider on the root anchor object is only used by the wall toy
                if (wbro.GetComponent<BoxCollider>() && wbro.transform.childCount > 0)
                {
                    Vector3 objLocalScale = wbro.transform.GetChild(0).localScale;
                    wbro.GetComponent<BoxCollider>().size = objLocalScale;
                    wbro.GetComponent<BoxCollider>().center = -Vector3.forward * objLocalScale.z * 0.5f;

                    CreateFurnitureDebris(instance.transform, objLocalScale);
                }
            }
            else if (classification.Contains(OVRSceneManager.Classification.DoorFrame) ||
               classification.Contains(OVRSceneManager.Classification.WindowFrame))
            {
                doorsAndWindows.Add(instance.gameObject);
            }
        }

        if (_roomboxWalls.Count < 5)
        {
            WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.ERROR_NOT_ENOUGH_WALLS);
        }

        _cornerPoints = GetClockwiseFloorOutline(anchorsAsWBRO.ToArray());

        CreatePolygonMesh(_roomboxWalls, _roomboxWalls[_roomFloorID], false);
        CreatePolygonMesh(_roomboxWalls, _roomboxWalls[_roomCeilingID], true);

        _wallHeight = _roomboxWalls[_roomCeilingID].transform.position.y - _roomboxWalls[_roomFloorID].transform.position.y;

        CreateFloorCeilingBorderEffects();

        // pair all the edges up, making it easier to hide/unhide them
        // while searching, also make sure they align with neighboring walls
        for (int i = 0; i < _roomboxWalls.Count; i++)
        {
            for (int j = 0; j < _roomboxWalls[i].wallEdges.Count; j++)
            {
                FindSiblingEdge(_roomboxWalls[i].wallEdges[j]);
            }
        }

        AudioManager.SetRoomOpenness(GetRoomOpenAmount());

        CreateSpecialEffectMesh(anchorsAsWBRO.ToArray());

        // doors and windows aren't used in the experience, because they would add complexity
        // they are however used in the special effect mesh, so must be deleted after that is created
        foreach (GameObject obj in doorsAndWindows)
        {
            Destroy(obj);
        }
    }

    /// <summary>
    /// When toggling walls, pre-pairing edges makes them easier to coordinate. While searching, also orient them to match the "open" space.
    /// </summary>
    void FindSiblingEdge(WallEdge edge)
    {
        // skip if edge already has a sibling (assigned from a prior search)
        if (edge._siblingEdge != null)
        {
            return;
        }

        // Get closest edge by distance, since ordering of wall indices isn't guaranteed
        // The sibling edge should be at the exact same position, but check for closest to avoid precision errors
        float closestDistance = 100.0f;
        WallEdge siblingEdge = null;
        for (int i = 0; i < _roomboxWalls.Count; i++)
        {
            // skip wall if edge belongs to it
            if (i == edge._parentSurface._surfaceID)
            {
                continue;
            }
            for (int j = 0; j < _roomboxWalls[i].wallEdges.Count; j++)
            {
                WallEdge thisEdge = _roomboxWalls[i].wallEdges[j];
                float thisDist = Vector3.Distance(edge.transform.position, thisEdge.transform.position);
                if (thisDist < closestDistance)
                {
                    closestDistance = thisDist;
                    siblingEdge = thisEdge;
                }
            }
        }
        // pair them up
        edge._siblingEdge = siblingEdge;
        siblingEdge._siblingEdge = edge;

        // align the vertical ones better, since wall angles are variable
        float wallSide = Vector3.Dot(edge.transform.right, Vector3.up);
        if (Mathf.Abs(wallSide) > 0.9f)
        {
            float rotated = Mathf.Sign(wallSide);
            edge.transform.rotation = Quaternion.LookRotation(-siblingEdge._parentSurface.transform.forward, -siblingEdge._parentSurface.transform.right * wallSide);
            siblingEdge.transform.rotation = Quaternion.LookRotation(-edge._parentSurface.transform.forward, edge._parentSurface.transform.right * wallSide);
        }
    }

    /// <summary>
    /// This creates the watertight single mesh, from all walls and furniture.
    /// </summary>
    public void CreateSpecialEffectMesh(WorldBeyondRoomObject[] worldBeyondObjects)
    {
        // for simplicity, doors/windows have been excluded from the experience
        // however, we'd still like them to render in this special effect mesh
        List<Transform> cubeFurniture = new List<Transform>();
        List<Transform> quadFurniture = new List<Transform>();
        List<GameObject> doorsAndWindows = new List<GameObject>();
        float ceilingHeight = 1.0f;
        for (int i = 0; i < worldBeyondObjects.Length; i++)
        {
            OVRSemanticClassification classification = worldBeyondObjects[i].GetComponent<OVRSemanticClassification>();
            if (worldBeyondObjects[i].GetComponent<OVRSceneVolume>())
            {
                // any child will have correct scale
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
        if (_sceneMesher)
        {
            try
            {
                _sceneMesh = _sceneMesher.CreateSceneMesh(_cornerPoints, cubeFurniture.ToArray(), quadFurniture.ToArray(), ceilingHeight);
                // attach it to an anchor object so it sticks to the real world
                _sceneMesh.transform.parent = _floorSceneAnchor.transform;
                _sceneMeshCreated = true;
            }
            catch
            {
                Debug.Log($"[{nameof(VirtualRoom)}]: Scene meshing failed.");
                _sceneMeshCreated = false;
            }
        }
    }

    /// <summary>
    /// The saved walls may not be clockwise, and they may not be in order.
    /// </summary>
    List<Vector3> GetClockwiseFloorOutline(WorldBeyondRoomObject[] allFurniture)
    {
        List<Vector3> cornerPoints = new List<Vector3>();

        if (null == _floorSceneAnchor || !OVRPlugin.GetSpaceBoundary2D(_floorSceneAnchor.Space, out Vector2[] boundary))
        {
            // fall back to wall corner method
            List<WorldBeyondRoomObject> justWalls = new List<WorldBeyondRoomObject>();
            for (int i = 0; i < allFurniture.Length; i++)
            {
                if (allFurniture[i]._isWall)
                {
                    justWalls.Add(allFurniture[i]);
                }
            }
            int seedWall = 0;
            for (int i = 0; i < justWalls.Count; i++)
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
                .ConvertAll<Vector3>(corner => new Vector3(-corner.x, corner.y, 0.0f));

            // GetSpaceBoundary2D is in anchor-space
            cornerPoints.Reverse();
            for (int i = 0; i < cornerPoints.Count; i++)
            {
                cornerPoints[i] = _floorSceneAnchor.transform.TransformPoint(cornerPoints[i]);
            }
        }

        return cornerPoints;
    }

    /// <summary>
    /// Search for the next corner point for the clockwise room spline, using the wall transforms.
    /// </summary>
    Vector3 GetNextSplinePoint(ref int thisID, List<WorldBeyondRoomObject> randomWalls)
    {
        Vector3 nextScale = randomWalls[thisID].GetComponent<WorldBeyondRoomObject>()._dimensions;

        Vector3 halfScale = nextScale * 0.5f;
        Vector3 bottomRight = randomWalls[thisID].transform.position - randomWalls[thisID].transform.up * halfScale.y - randomWalls[thisID].transform.right * halfScale.x;
        float closestCornerDistance = 100.0f;
        // When searching for a matching corner, the correct one should match positions. If they don't, assume there's a crack in the room.
        // This should be an impossible scenario and likely means broken data from Room Setup.
        const float distanceTolerance = 0.1f;
        Vector3 nextCorner = Vector3.zero;
        int nextWallID = 0;
        for (int i = 0; i < randomWalls.Count; i++)
        {
            // compare to bottom left point of other walls
            if (i != thisID)
            {
                Vector3 thisWallHalfScale = randomWalls[i].GetComponent<WorldBeyondRoomObject>()._dimensions * 0.5f;
                Vector3 bottomLeft = randomWalls[i].transform.position - randomWalls[i].transform.up * thisWallHalfScale.y + randomWalls[i].transform.right * thisWallHalfScale.x;
                float thisCornerDistance = Vector3.Distance(bottomLeft, bottomRight);
                if (thisCornerDistance < closestCornerDistance)
                {
                    closestCornerDistance = thisCornerDistance;
                    nextCorner = bottomLeft;
                    nextWallID = i;
                }
            }
        }

        if (closestCornerDistance > distanceTolerance)
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
        _effectRadius = 2.0f;
        if (_sceneMeshCreated)
        {
            _sceneMesh.enabled = true;
            _effectRadius = _sceneMesher.GetRoomDiameter() * 0.4f;
            _sceneMesh.material.SetVector("_EffectPosition", Vector3.up * 1000.0f);
            _sceneMesh.material.SetFloat("_EffectRadius", _effectRadius);
            _sceneMesh.material.SetFloat("_EffectWidth", 0.1f);
            _sceneMesh.material.SetFloat("_CeilingHeight", _sceneMesher._ceilingHeight);
        }
        else
        {
            for (int i = 0; i < _roomboxFurnishings.Count; i++)
            {
                _roomboxFurnishings[i]._passthroughMesh.material.SetVector("_EffectPosition", Vector3.up * 1000.0f);
                _roomboxFurnishings[i]._passthroughMesh.material.SetFloat("_EffectRadius", _effectRadius);
                _roomboxFurnishings[i]._passthroughMesh.material.SetFloat("_EffectWidth", 0.1f);
            }

            for (int i = 0; i < _roomboxWalls.Count; i++)
            {
                _roomboxWalls[i]._passthroughMesh.material.SetVector("_EffectPosition", Vector3.up * 1000.0f);
                _roomboxWalls[i]._passthroughMesh.material.SetFloat("_EffectRadius", _effectRadius);
                _roomboxWalls[i]._passthroughMesh.material.SetFloat("_EffectWidth", 0.1f);
            }
        }
    }

    /// <summary>
    /// After the intro, we no longer need the effect mesh.
    /// </summary>
    public void HideEffectMesh()
    {
        if (_sceneMeshCreated)
        {
            _sceneMesh.enabled = false;
        }
    }

    /// <summary>
    /// Replace the prefab's quad with a custom polygon, for an exact floor outline.
    /// </summary>
    void CreatePolygonMesh(List<WorldBeyondRoomObject> roomSurfaces, WorldBeyondRoomObject srObject, bool flipNormal)
    {
        try
        {
            // to avoid precision issues resulting in tiny cracks along the floor/ceiling edges, we want to extend out the polygon by a small amount
            for (int i = 0; i < _cornerPoints.Count; i++)
            {
                Vector3 startPos = _cornerPoints[i];
                Vector3 endPos = (i == _cornerPoints.Count - 1) ? _cornerPoints[0] : _cornerPoints[i + 1];
                Vector3 lastPos = (i == 0) ? _cornerPoints[_cornerPoints.Count - 1] : _cornerPoints[i - 1];

                Vector3 insetDirection = _sceneMesher.GetInsetDirection(lastPos, startPos, endPos);
                _cornerPoints[i] = _cornerPoints[i] - insetDirection * _edgeBuffer;
            }

            Mesh PolygonMesh = new Mesh();
            Vector3[] Vertices = new Vector3[_cornerPoints.Count];
            Vector2[] UVs = new Vector2[Vertices.Length];
            Color32[] Colors = new Color32[Vertices.Length];
            Vector3[] Normals = new Vector3[Vertices.Length];
            Vector4[] Tangents = new Vector4[Vertices.Length];
            int[] Triangles = new int[(_cornerPoints.Count - 2) * 3];

            for (int i = 0; i < _cornerPoints.Count; i++)
            {
                // transform vertex positions first
                Vector3 offsetPoint = new Vector3(_cornerPoints[i].x, srObject._passthroughMesh.transform.position.y, _cornerPoints[i].z);

                Vertices[i] = srObject._passthroughMesh.transform.InverseTransformPoint(offsetPoint);
                UVs[i] = new Vector2(_cornerPoints[i].x, _cornerPoints[i].z);
                Colors[i] = Color.white;
                Normals[i] = -Vector3.forward;
                Tangents[i] = new Vector4(1, 0, 0, 1);
            }

            // triangulate
            List<Vector2> points2d = new List<Vector2>(_cornerPoints.Count);
            for (int i = 0; i < _cornerPoints.Count; i++)
            {
                points2d.Add(new Vector2(_cornerPoints[i].x, _cornerPoints[i].z));
            }

            Triangulator triangulator = new Triangulator(points2d.ToArray());
            int[] indices = triangulator.Triangulate();
            int indexCounter = 0;
            for (int j = 0; j < _cornerPoints.Count - 2; j++)
            {
                int id0 = indices[j * 3];
                int id1 = indices[j * 3 + 1];
                int id2 = indices[j * 3 + 2];

                Triangles[indexCounter++] = id0;
                Triangles[indexCounter++] = (flipNormal ? id2 : id1);
                Triangles[indexCounter++] = (flipNormal ? id1 : id2);
            }


            // assign
            PolygonMesh.Clear();
            PolygonMesh.name = "PolygonMesh";
            PolygonMesh.vertices = Vertices;
            PolygonMesh.uv = UVs;
            PolygonMesh.colors32 = Colors;
            PolygonMesh.normals = Normals;
            PolygonMesh.tangents = Tangents;
            PolygonMesh.triangles = Triangles;
            if (srObject._passthroughMesh.gameObject.GetComponent<MeshFilter>())
            {
                srObject._passthroughMesh.gameObject.GetComponent<MeshFilter>().mesh = PolygonMesh;
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
    void CreateWallBorderEffects(WorldBeyondRoomObject wallObject)
    {
        for (int j = 0; j < 4; j++)
        {
            GameObject frame = Instantiate(_edgePrefab);
            // rotate each edge 90 degrees around the wall, starting from the top
            // even is Y axis, odd is X axis
            frame.transform.rotation = wallObject.transform.rotation * Quaternion.Euler(0, 0, 90 * j);
            Vector3 scl = wallObject._dimensions;
            float extent = (j % 2 == 0) ? scl.y * 0.5f : scl.x * 0.5f;
            frame.transform.position = wallObject.transform.position + frame.transform.up * extent;
            float width = (j % 2 == 1) ? scl.y : scl.x;
            frame.transform.localScale = new Vector3(1, 1.0f / scl.y, 1.0f / scl.z);

            WallEdge thisWallEdge = frame.GetComponent<WallEdge>();
            thisWallEdge._parentSurface = wallObject;
            thisWallEdge.AdjustParticleSystemRateAndSize(width);
            thisWallEdge.transform.parent = wallObject.transform;
            wallObject.wallEdges.Add(thisWallEdge);
        }
    }

    /// <summary>
    /// Create and initialize the N particle/border effects for the floor/ceiling.
    /// </summary>
    void CreateFloorCeilingBorderEffects()
    {
        // for ceiling & floor, still use walls to calculate edge transform, but ensure edge fades "belong" to ceiling/floor
        List<WallEdge> ceilingEdges = new List<WallEdge>();
        List<WallEdge> floorEdges = new List<WallEdge>();

        // make the edge frills for the floor/ceiling
        // different than the walls because floorplan isn't always a quad
        for (int i = 0; i < _roomboxWalls.Count; i++)
        {
            if (!_roomboxWalls[i]._isWall)
            {
                continue;
            }
            GameObject floorFrame = Instantiate(_edgePrefab);
            GameObject ceilingFrame = Instantiate(_edgePrefab);
            Transform refTransform = _roomboxWalls[i]._passthroughMesh.transform;
            Vector3 wallOut = -_roomboxWalls[i].transform.forward;
            Vector3 worldUp = Vector3.up;
            Vector3.OrthoNormalize(ref wallOut, ref worldUp);
            floorFrame.transform.rotation = Quaternion.LookRotation(worldUp, wallOut);
            ceilingFrame.transform.rotation = Quaternion.LookRotation(-worldUp, wallOut);

            floorFrame.transform.localScale = new Vector3(refTransform.localScale.x, floorFrame.transform.localScale.y, floorFrame.transform.localScale.z);
            ceilingFrame.transform.localScale = floorFrame.transform.localScale;

            floorFrame.transform.position = refTransform.position - refTransform.up * refTransform.localScale.y * 0.5f;
            ceilingFrame.transform.position = refTransform.position + refTransform.up * refTransform.localScale.y * 0.5f;

            WallEdge floorEdge = floorFrame.GetComponent<WallEdge>();
            floorEdge._parentSurface = _roomboxWalls[_roomFloorID];
            floorEdge.AdjustParticleSystemRateAndSize(refTransform.localScale.x);
            floorEdge.transform.parent = _roomboxWalls[_roomFloorID].transform;
            floorEdges.Add(floorEdge);

            WallEdge ceilingEdge = ceilingFrame.GetComponent<WallEdge>();
            ceilingEdge._parentSurface = _roomboxWalls[_roomCeilingID];
            ceilingEdge.AdjustParticleSystemRateAndSize(refTransform.localScale.x);
            ceilingEdge.transform.parent = _roomboxWalls[_roomCeilingID].transform;
            ceilingEdges.Add(ceilingEdge);
        }

        _roomboxWalls[_roomCeilingID].wallEdges.AddRange(ceilingEdges);
        _roomboxWalls[_roomFloorID].wallEdges.AddRange(floorEdges);
    }

    /// <summary>
    /// Grass shrubs around the base of the wall that appear when it's removed.
    /// </summary>
    void CreateWallDebris(WorldBeyondRoomObject wallObject)
    {
        // debris along the ground
        int debrisCount = Random.Range(6, 10);
        for (int i = 0; i < debrisCount; i++)
        {
            Vector3 scl = wallObject._dimensions;
            GameObject debris = Instantiate(WorldBeyondEnvironment.Instance._grassPrefab);
            Vector3 randomPosition = wallObject.transform.position - Vector3.up * scl.y * 0.5f;
            float posToEdge = Random.Range(-0.5f, 0.5f);
            if (i <= 1)
            {
                // put the first two towards the edge, for effect
                posToEdge = Random.Range(0.4f, 0.5f) * (i - 0.5f) * 2;
            }
            randomPosition += wallObject.transform.right * scl.x * posToEdge;
            randomPosition += wallObject.transform.forward * Random.Range(-0.2f, 0.1f);
            Vector3 randomScale = Vector3.one * Random.Range(0.8f, 1.2f) * (Mathf.Abs(posToEdge) + 0.5f);
            debris.transform.position = randomPosition;
            debris.transform.localScale = Vector3.zero;
            debris.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360.0f), 0);
            wallObject.wallDebris.Add(debris);
        }
    }

    /// <summary>
    /// Given a wall, set it up to work with Navmesh
    /// </summary>
    void CreateNavMeshObstacle(WorldBeyondRoomObject wallObject)
    {
        if (wallObject._passthroughMesh && wallObject._passthroughMesh.gameObject.GetComponent<NavMeshObstacle>() == null)
        {
            NavMeshObstacle obstacle = wallObject._passthroughMesh.gameObject.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = new Vector3(1, 1, 0.1f);
            obstacle.center = Vector3.zero;
        }
    }

    /// <summary>
    /// Grass shrubs that surround the bases of furniture, that hide if all walls are closed.
    /// </summary>
    void CreateFurnitureDebris(Transform child, Vector3 scl)
    {
        Vector3 basePos = new Vector3(child.position.x, _floorHeight, child.position.z);
        for (int j = 0; j < 12; j++)
        {
            GameObject debris = Instantiate(WorldBeyondEnvironment.Instance._grassPrefab);
            float side = (j % 2) - 0.5f;
            float debrisDiameter = 0.15f;
            float randomPos = Random.Range(-0.5f, 0.5f);

            float rndXsign = Mathf.Sign(Random.Range(0, 2) - 1);
            float rndYsign = Mathf.Sign(Random.Range(0, 2) - 1);

            debris.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360.0f), 0);
            debris.transform.position = basePos;
            Quaternion boxRot = child.rotation;
            Vector3 localOffset1 = new Vector3(side * scl.x * rndXsign - side * debrisDiameter, randomPos * scl.y * rndYsign, 0);
            Vector3 localOffset2 = new Vector3(randomPos * scl.x * rndXsign, side * scl.y * rndYsign - side * debrisDiameter, 0);
            Vector3 offset = (side < 0) ? boxRot * localOffset1 : boxRot * localOffset2;
            debris.transform.position += offset;
            debris.transform.localScale = Vector3.one * Random.Range(0.3f, 0.7f);
            _roomDebris.Add(debris);
        }
    }

    /// <summary>
    /// Assign parameters to the room's starry material, hinting to the player where they should shine the flashlight.
    /// </summary>
    public void SetWallEffectParams(Vector3 objPosition, float effectStrength, float ballMaskStrength)
    {
        for (int i = 0; i < _roomboxWalls.Count; i++)
        {
            _roomboxWalls[i]._passthroughMesh.material.SetVector("_OppyPosition", objPosition);
            _roomboxWalls[i]._passthroughMesh.material.SetFloat("_OppyRippleStrength", effectStrength);
            _roomboxWalls[i]._passthroughMesh.material.SetFloat("_MaskRippleStrength", ballMaskStrength);
        }
        for (int i = 0; i < _roomboxFurnishings.Count; i++)
        {
            _roomboxFurnishings[i]._passthroughMesh.material.SetVector("_OppyPosition", objPosition);
            _roomboxFurnishings[i]._passthroughMesh.material.SetFloat("_OppyRippleStrength", effectStrength);
            _roomboxFurnishings[i]._passthroughMesh.material.SetFloat("_MaskRippleStrength", ballMaskStrength);
        }
    }

    /// <summary>
    /// As Oppy is discovered, the world gets more and more saturated.
    /// </summary>
    public void SetRoomSaturation(float roomSat)
    {
        for (int i = 0; i < _roomboxWalls.Count; i++)
        {
            _roomboxWalls[i]._passthroughMesh.material.SetFloat("_SaturationAmount", roomSat);
        }
        for (int i = 0; i < _roomboxFurnishings.Count; i++)
        {
            _roomboxFurnishings[i]._passthroughMesh.material.SetFloat("_SaturationAmount", roomSat);
        }
    }

    /// <summary>
    /// A fallback position to spawn things in the room, in case raycasting fails.
    /// </summary>
    public Vector3 GetSimpleFloorPosition()
    {
        if (_roomboxWalls == null) return Vector3.zero;
        if (_roomboxWalls.Count < 2) return Vector3.zero;

        // to be extra safe, spawn it wherever the player is
        WorldBeyondRoomObject floor = _roomboxWalls[_roomFloorID];
        if (!floor) return Vector3.zero;
        Vector3 floorPos = new Vector3(WorldBeyondManager.Instance._mainCamera.transform.position.x, floor.transform.position.y, WorldBeyondManager.Instance._mainCamera.transform.position.z);
        return floorPos;
    }

    public bool GetOutdoorAudioPosition(ref Vector3 audioPos)
    {
        bool wallOpen = false;
        Vector3 averageOpenWallPosition = Vector3.up;
        for (int i = 0; i < _roomboxWalls.Count; i++)
        {
            if (!_roomboxWalls[i]._passthroughWallActive)
            {
                averageOpenWallPosition += _roomboxWalls[i].transform.position;
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
        bool isBlocked = false;
        LayerMask soundObstructionLayer = LayerMask.GetMask("Default");
        RaycastHit[] roomboxHit = Physics.RaycastAll(directionToSound, distance, soundObstructionLayer);
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
        return _wallHeight;
    }

    /// <summary>
    /// Mute the audio depending on how "open" the room is (how many passthrough walls are off)
    /// </summary>
    public float GetRoomOpenAmount()
    {
        if (_roomboxWalls == null || _roomboxWalls.Count == 0)
        {
            return 1f;
        }

        // give the ceiling 50% weight
        float totalAmount = _roomboxWalls[_roomboxWalls.Count - 1]._passthroughWallActive ? 0.0f : 0.5f;

        int wallsOpen = 0;
        for (int i = 0; i < _roomboxWalls.Count - 2; i++) // ignore last two walls (floor and ceiling)
        {
            if (!_roomboxWalls[i]._passthroughWallActive)
            {
                wallsOpen++;
            }
        }
        // the other 50% is a percentage of the remaining walls
        totalAmount += (0.5f * (wallsOpen / (float)(_roomboxWalls.Count - 2)));

        return totalAmount;
    }

    /// <summary>
    /// For the intro room box glowing lines.
    /// </summary>
    public void SetEdgeEffectIntensity(float normValue)
    {
        if (_sceneMeshCreated)
        {
            float intensity = _edgeIntensity.Evaluate(normValue);
            _sceneMesh.material.SetFloat("_EffectIntensity", intensity);
            _sceneMesh.material.SetFloat("_EdgeTimeline", normValue);
        }
    }

    /// <summary>
    /// When the roombox mesh is getting illuminated by the Multitoy, assign them to the material.
    /// </summary>
    public void SetEffectPosition(Vector3 worldPos, float timer)
    {
        if (_sceneMeshCreated)
        {
            _sceneMesh.material.SetVector("_EffectPosition", worldPos);
            _sceneMesh.material.SetFloat("_EffectRadius", timer * _effectRadius);
        }
        else
        {
            for (int i = 0; i < _roomboxFurnishings.Count; i++)
            {
                _roomboxFurnishings[i]._passthroughMesh.material.SetVector("_EffectPosition", worldPos);
                _roomboxFurnishings[i]._passthroughMesh.material.SetFloat("_EffectRadius", timer * _effectRadius);
            }

            for (int i = 0; i < _roomboxWalls.Count; i++)
            {
                _roomboxWalls[i]._passthroughMesh.material.SetVector("_EffectPosition", worldPos);
                _roomboxWalls[i]._passthroughMesh.material.SetFloat("_EffectRadius", timer * _effectRadius);
            }
        }
    }

    /// <summary>
    /// Simple bounding-box test to see if a position (with buffer radius) is in the room. Ignores height.
    /// </summary>
    public bool IsPositionInRoom(Vector3 pos, float positionBuffer, bool alsoCheckVertical = false)
    {
        bool inRoom = false;
        float xMin = 0.0f;
        float xMax = 0.0f;
        float yMin = 0.0f;
        float yMax = 0.0f;
        float zMin = 0.0f;
        float zMax = 0.0f;
        int anyWall = 0;
        for (int i = 0; i < _roomboxWalls.Count; i++)
        {
            if (!_roomboxWalls[i]._isWall)
            {
                continue;
            }
            anyWall = i;
            Vector3 wallRight = -_roomboxWalls[i].transform.right;
            Vector3 wallScale = _roomboxWalls[i]._passthroughMesh.transform.localScale;
            Vector3 pos1 = _roomboxWalls[i].transform.position - wallRight * wallScale.x * 0.5f;
            Vector3 pos2 = _roomboxWalls[i].transform.position + wallRight * wallScale.x * 0.5f;
            if (pos1.x < xMin) xMin = pos1.x;
            if (pos1.x > xMax) xMax = pos1.x;
            if (pos1.z < zMin) zMin = pos1.z;
            if (pos1.z > zMax) zMax = pos1.z;
        }
        inRoom = (pos.x > xMin - positionBuffer) && (pos.x < xMax + positionBuffer) && (pos.z > zMin - positionBuffer) && (pos.z < zMax + positionBuffer);
        if (alsoCheckVertical)
        {
            float floorPos = (_roomboxWalls[anyWall].transform.position - Vector3.up * _roomboxWalls[anyWall]._passthroughMesh.transform.localScale.y * 0.5f).y;
            float ceilingPos = (_roomboxWalls[anyWall].transform.position + Vector3.up * _roomboxWalls[anyWall]._passthroughMesh.transform.localScale.y * 0.5f).y;
            inRoom &= (pos.y > floorPos + positionBuffer);
            inRoom &= (pos.y < ceilingPos - positionBuffer);
        }
        return inRoom;
    }

    /// <summary>
    /// Point-in-polygon test to see if position is in room
    /// </summary>
    public bool IsPlayerInRoom()
    {
        Vector3 cameraPos = WorldBeyondManager.Instance._mainCamera.transform.position;
        cameraPos = new Vector3(cameraPos.x, _cornerPoints[0].y, cameraPos.z);
        // Shooting a ray from player to the right (X+), count how many walls it intersects.
        // If the count is odd, the player is in the room
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
                    Vector3 lineVec = (highestPoint - lowestPoint).normalized;
                    Vector3 camVec = (cameraPos - lowestPoint).normalized;
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
        return surfaceID == _roomFloorID;
    }

    /// <summary>
    /// During the intro, apply a different material to the room
    /// </summary>
    public void ShowDarkRoom(bool doShow)
    {
        // if using the sceneMesh, this function is unnecessary
        if (_sceneMeshCreated)
        {
            return;
        }
        if (doShow)
        {
            ShowAllWalls(true);
        }

        for (int i = 0; i < _roomboxFurnishings.Count; i++)
        {
            _roomboxFurnishings[i].ShowDarkRoomMaterial(doShow);
        }

        for (int i = 0; i < _roomboxWalls.Count; i++)
        {
            _roomboxWalls[i].ShowDarkRoomMaterial(doShow);
        }
    }
}
