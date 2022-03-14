// Copyright(c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class VirtualRoom : MonoBehaviour
{
    static public VirtualRoom Instance = null;

    public GameObject _anchorPrefab;
    public GameObject _wallPrefab;
    public GameObject _volumePrefab;
    public GameObject _edgePrefab;
    bool _roomOpen = false;
    List<SanctuaryRoomObject> _roomboxFurnishings = new List<SanctuaryRoomObject>();
    List<SanctuaryRoomObject> _roomboxWalls = new List<SanctuaryRoomObject>();
    public SceneMesher _sceneMesher;
    int _roomFloorID;
    int _roomCeilingID;

    List<GameObject> _roomDebris = new List<GameObject>();

    MeshRenderer _sceneMesh = null;
    float _effectRadius = 10.0f;
    public AnimationCurve _edgeIntensity;

    private void Awake()
    {
        if (!Instance)
        {
            Instance = this;
        }
    }

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
        _roomboxWalls[wallID]._passthroughWall.gameObject.SetActive(doClose);

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

    public void CheckForGuardian()
    {
        float guardianFade = 0.0f;
        bool activateGuardian = false;
        
        // check each wall to ensure camera is within range
        for (int i = 0; i < _roomboxWalls.Count; i++)
        {
            if (_roomboxWalls[i]._guardianWall && _roomboxWalls[i]._isWall)
            {
                Vector3 toWall = SanctuaryExperience.Instance._mainCamera.transform.position - _roomboxWalls[i].transform.position;
                Vector3 perpDistance = Vector3.Project(toWall, -_roomboxWalls[i].transform.forward);
                activateGuardian |= (perpDistance.magnitude < 0.75f);
                // fade Guardian in from 0.75 to 0.5, at which point the red ring happens
                float fade = -(perpDistance.magnitude - 0.75f) * 4;
                if (fade > guardianFade)
                {
                    guardianFade = fade;
                }
            }
        }

        for (int i = 0; i < _roomboxWalls.Count; i++)
        {
            if (_roomboxWalls[i]._guardianWall && _roomboxWalls[i] && _roomboxWalls[i]._isWall)
            {
                _roomboxWalls[i]._guardianWall.gameObject.SetActive(activateGuardian && !_roomboxWalls[i]._passthroughWallActive);

                if (activateGuardian)
                {
                    _roomboxWalls[i]._guardianWall.material.SetVector("_CamPosition", SanctuaryExperience.Instance._mainCamera.transform.position);
                    _roomboxWalls[i]._guardianWall.material.SetFloat("_GuardianFade", Mathf.Clamp01(guardianFade));
                }
            }
        }
    }

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

    public void ShowAllWalls(bool doShow)
    {
        for (int i = 0; i < _roomboxFurnishings.Count; i++)
        {
            _roomboxFurnishings[i]._passthroughWall.gameObject.SetActive(doShow);
        }

        for (int i = 0; i < _roomboxWalls.Count; i++)
        {
            _roomboxWalls[i]._passthroughWall.gameObject.SetActive(doShow);
            if (!doShow)
            {
                _roomboxWalls[i]._guardianWall.gameObject.SetActive(false);
            }

            SanctuaryRoomObject sro = _roomboxWalls[i];
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

    // DEBUG only: Scene is failing to load. Use a generic room so we can develop.
    public void Initialize()
    {
        // sceneMesher should have some corner points. Use them to create the room
        // fill an array with OVRSceneObjects so we can reuse the normal initialization flow
        List<OVRSceneObject> tempSceneObjects = new List<OVRSceneObject>();
        float xMin = 0.0f;
        float xMax = 0.0f;
        float zMin = 0.0f;
        float zMax = 0.0f;
        const float height = 2.5f;
        if (_sceneMesher._debugSplinePoints.Length >= 4)
        {
            // create a wall for each point
            for (int i = 0; i < _sceneMesher._debugSplinePoints.Length; i++)
            {
                int nextID = (i == _sceneMesher._debugSplinePoints.Length - 1 ? 0 : i + 1);
                Vector3 upVec = _sceneMesher._debugSplinePoints[i].position - _sceneMesher._debugSplinePoints[nextID].position;
                Vector3 fwdVec = Vector3.Cross(upVec.normalized, Vector3.up);
                GameObject wallObj = Instantiate(_anchorPrefab);
                OVRSceneObject ovrso = wallObj.GetComponent<OVRSceneObject>();
                wallObj.transform.position = _sceneMesher._debugSplinePoints[i].position - upVec * 0.5f + Vector3.up * height * 0.5f;
                wallObj.transform.rotation = Quaternion.LookRotation(fwdVec, upVec);
                ovrso.dimensions = new Vector3(height, upVec.magnitude, 1);
                ovrso.classification.labels[0] = "WALL_FACE";
                tempSceneObjects.Add(ovrso);

                Vector3 pos = _sceneMesher._debugSplinePoints[i].position;
                if (pos.x < xMin) xMin = pos.x;
                if (pos.x > xMax) xMax = pos.x;
                if (pos.z < zMin) zMin = pos.z;
                if (pos.z > zMax) zMax = pos.z;
            }

            Vector3 floorScale = new Vector3(xMax - xMin, zMax - zMin, 1.0f);
            Vector3 floorCenter = new Vector3((xMax + xMin) * 0.5f, 0.0f, (zMax + zMin) * 0.5f);

            // then create floor/ceiling
            GameObject floorObj = Instantiate(_anchorPrefab);
            OVRSceneObject floorSO = floorObj.GetComponent<OVRSceneObject>();
            floorObj.transform.position = floorCenter;
            floorObj.transform.rotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
            floorSO.dimensions = floorScale;
            floorSO.classification.labels[0] = "FLOOR";
            tempSceneObjects.Add(floorSO);

            GameObject ceilingObj = Instantiate(_anchorPrefab);
            OVRSceneObject ceilingSO = ceilingObj.GetComponent<OVRSceneObject>();
            ceilingObj.transform.position = floorCenter + Vector3.up * height;
            ceilingObj.transform.rotation = Quaternion.LookRotation(-Vector3.up, Vector3.forward);
            ceilingSO.dimensions = floorScale;
            ceilingSO.classification.labels[0] = "CEILING";
            tempSceneObjects.Add(ceilingSO);
        }

        Initialize(tempSceneObjects.ToArray());
    }

    public void Initialize(OVRSceneObject[] sceneObjects)
    {
        for (int i = 0; i < sceneObjects.Length; i++)
        {
            OVRSceneObject instance = sceneObjects[i];
            if (instance.classification.labels[0] == "WALL_FACE" ||
                instance.classification.labels[0] == "FLOOR" ||
                instance.classification.labels[0] == "CEILING")
            {
                // create the actual surfaces, since the OVRSceneObject array contains just metadata
                GameObject newSurface = Instantiate(_wallPrefab);
                newSurface.transform.position = instance.transform.position;
                newSurface.transform.rotation = instance.transform.rotation;
                foreach (Transform child in newSurface.transform)
                {
                    child.transform.localScale = instance.dimensions;
                }
                SanctuaryRoomObject sro = newSurface.GetComponent<SanctuaryRoomObject>();

                sro._surfaceID = _roomboxWalls.Count;

                if (instance.classification.labels[0] == "FLOOR")
                {
                    _roomFloorID = _roomboxWalls.Count;

                    // move the world slightly below the ground floor, so the virtual floor doesn't Z-fight
                    SanctuaryExperience.Instance.MoveGroundFloor(instance.transform.position.y-0.015f);
                }
                else if (instance.classification.labels[0] == "CEILING")
                {
                    _roomCeilingID = _roomboxWalls.Count;
                }
                _roomboxWalls.Add(sro);

                if (instance.classification.labels[0] == "WALL_FACE")
                {
                    sro._isWall = true;

                    CreateWallBorderEffects(sro, instance.dimensions);
                    CreateWallDebris(sro, instance.dimensions);

                    // add as navmesh obstacles
                    if (sro._passthroughWall)
                    {
                        NavMeshObstacle obstacle = sro._passthroughWall.gameObject.AddComponent<NavMeshObstacle>();
                        obstacle.carving = true;
                        obstacle.shape = NavMeshObstacleShape.Box;
                        obstacle.size = new Vector3(1,1,0.1f);
                        obstacle.center = Vector3.zero;
                    }
                }

                // fake Guardian
                sro._guardianWall.material.SetVector("_WallScale", new Vector4(instance.dimensions.x, instance.dimensions.y,0,0));
                sro._guardianWall.gameObject.SetActive(false);

                // the collider that the wall toggler uses
                if (sro.GetComponent<BoxCollider>())
                {
                    sro.GetComponent<BoxCollider>().size = new Vector3(sceneObjects[i].dimensions.x, sceneObjects[i].dimensions.y, 0.01f);
                }
            }
            else if (instance.classification.labels[0] != "FLOOR" &&
                instance.classification.labels[0] != "CEILING")
            {
                // can't use the VolumeAndPlaneSwitcher because that has a delay
                // TODO: remove this once final API lands
                Vector3 position = Vector3.zero;
                Quaternion rotation = Quaternion.identity;
                Vector3 localScale = Vector3.zero;
                GameObject volumeObject = Instantiate(OVRSceneManager.instance.volumePrefab.gameObject, instance.transform.parent);
                OVRSceneObject ovrso = volumeObject.GetComponent<OVRSceneObject>();
                SceneObjectHelper.GetVolumeFromTopPlane(
                     instance.transform,
                     instance.dimensions,
                     out position,
                     out rotation,
                     out localScale);
                volumeObject.transform.localScale = Vector3.one;
                if (volumeObject.GetComponent<BoxCollider>())
                {
                    volumeObject.GetComponent<BoxCollider>().size = localScale;
                }

                SanctuaryRoomObject newsro = volumeObject.GetComponent<SanctuaryRoomObject>();
                if (newsro)
                {
                    newsro._passthroughWall.transform.localScale = localScale;
                    newsro._isFurniture = true;
                    _roomboxFurnishings.Add(newsro);

                    // add as navmesh obstacles
                    BoxCollider bc = newsro._passthroughWall.GetComponent<BoxCollider>();
                    if (bc)
                    {
                        NavMeshObstacle obstacle = bc.gameObject.AddComponent<NavMeshObstacle>();
                        obstacle.carving = true;
                        obstacle.shape = NavMeshObstacleShape.Box;
                        obstacle.size = Vector3.one;
                        obstacle.center = bc.center;
                    }
                }
                volumeObject.transform.rotation = rotation;
                volumeObject.transform.position = position;
                ovrso.dimensions = localScale;
                for (int j = 0; j < instance.classification.labels.Length; j++)
                {
                    ovrso.classification.labels[j] = instance.classification.labels[j];
                }

                // destroy old plane, replace it with volume
                Destroy(instance.gameObject);
                instance = ovrso;

                CreateFurnitureDebris(instance.transform, instance.dimensions);
            }
        }

        CreatePolygonMesh(_roomboxWalls[_roomFloorID], false);
        CreatePolygonMesh(_roomboxWalls[_roomCeilingID], true);

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
    }

    // when toggling walls, pre-pairing edges makes them easier to coordinate
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
            edge.transform.rotation = Quaternion.LookRotation(-siblingEdge._parentSurface.transform.forward, -siblingEdge._parentSurface.transform.up * wallSide);
            siblingEdge.transform.rotation = Quaternion.LookRotation(-edge._parentSurface.transform.forward, edge._parentSurface.transform.up * wallSide);
        }
    }

    // this creates the watertight single mesh, from all walls and furniture
    public void CreateSpecialEffectMesh()
    {
        List<Transform> justWalls = new List<Transform>();
        List<Transform> justFurniture = new List<Transform>();
        foreach (SanctuaryRoomObject obj in _roomboxWalls)
        {
            if (obj._isWall)
            {
                justWalls.Add(obj._passthroughWall.transform);
            }
        }
        foreach (SanctuaryRoomObject obj in _roomboxFurnishings)
        {
            justFurniture.Add(obj._passthroughWall.transform);
        }

        // create special effect mesh
        if (_sceneMesher)
        {
            float ceilingHeight = justWalls[0].localScale.x;
            _sceneMesh = _sceneMesher.CreateSceneMesh(justWalls, justFurniture, ceilingHeight);
        }

    }

    public void AnimateEffectMesh(Transform camXform)
    {
        if (!_sceneMesh)
        {
            return;
        }
        _sceneMesh.enabled = true;
        _effectRadius = _sceneMesher.GetRoomDiameter() * 0.4f;
        _sceneMesh.material.SetVector("_EffectPosition", Vector3.up * 1000.0f);
        _sceneMesh.material.SetFloat("_EffectRadius", _effectRadius);
        _sceneMesh.material.SetFloat("_EffectWidth", 0.1f);
        _sceneMesh.material.SetFloat("_CeilingHeight", _sceneMesher._ceilingHeight);
    }

    public void HideEffectMesh()
    {
        if (_sceneMesh) _sceneMesh.enabled = false;
    }

    // replace the prefab's quad with a custom polygon, for a an exact floor outline
    void CreatePolygonMesh(SanctuaryRoomObject srObject, bool flipNormal)
    {
        Mesh PolygonMesh = new Mesh();

        List<Vector3> cornerPoints = new List<Vector3>();
        for (int i = 0; i < _roomboxWalls.Count; i++)
        {
            if (_roomboxWalls[i]._isWall)
            {
                Transform wallXform = _roomboxWalls[i].transform;
                Vector3 objScale = wallXform.localScale;
                if (_roomboxWalls[i].GetComponent<OVRSceneObject>())
                {
                    objScale = _roomboxWalls[i].GetComponent<OVRSceneObject>().dimensions;
                }
                else if (wallXform.childCount > 0 && wallXform.GetChild(0))
                {
                    objScale = wallXform.GetChild(0).localScale;
                }
                Vector3 bottomLeftCorner = wallXform.position - (wallXform.up * objScale.y * 0.5f) + (wallXform.right * objScale.x * 0.5f);
                bottomLeftCorner = new Vector3(bottomLeftCorner.x, 0, bottomLeftCorner.z);
                cornerPoints.Add(bottomLeftCorner);
            }
        }
        // check if walls were created CCW or CW (layout spline requires they be CW)
        if (!_sceneMesher.IsListCW(cornerPoints))
        {
            cornerPoints.Reverse();
        }

        Vector3[] Vertices = new Vector3[cornerPoints.Count];
        Vector2[] UVs = new Vector2[Vertices.Length];
        Color32[] Colors = new Color32[Vertices.Length];
        Vector3[] Normals = new Vector3[Vertices.Length];
        Vector4[] Tangents = new Vector4[Vertices.Length];
        int[] Triangles = new int[(cornerPoints.Count - 2) * 3];

        for (int i = 0; i < cornerPoints.Count; i++)
        {
            // transform vertex positions first
            Vector3 offsetPoint = new Vector3(cornerPoints[i].x, srObject._passthroughWall.transform.position.y, cornerPoints[i].z);
            Vertices[i] = srObject._passthroughWall.transform.InverseTransformPoint(offsetPoint);

            UVs[i] = new Vector2(cornerPoints[i].x, cornerPoints[i].z);
            Colors[i] = Color.white;
            Normals[i] = -Vector3.forward;
            Tangents[i] = new Vector4(1, 0, 0, 1);
        }

        // triangulate
        List<Vector2> points2d = new List<Vector2>(cornerPoints.Count);
        for (int i = 0; i < cornerPoints.Count; i++)
        {
            points2d.Add(new Vector2(cornerPoints[i].x, cornerPoints[i].z));
        }

        Triangulator triangulator = new Triangulator(points2d.ToArray());
        int[] indices = triangulator.Triangulate();
        int indexCounter = 0;
        for (int j = 0; j < cornerPoints.Count - 2; j++)
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
        PolygonMesh.triangles = Triangles;
        PolygonMesh.normals = Normals;
        PolygonMesh.tangents = Tangents;
        if (srObject._passthroughWall.gameObject.GetComponent<MeshFilter>())
        {
            srObject._passthroughWall.gameObject.GetComponent<MeshFilter>().mesh = PolygonMesh;
        }
    }

    void CreateWallBorderEffects(SanctuaryRoomObject wallObject, Vector3 localDimensions)
    {
        for (int j = 0; j < 4; j++)
        {
            GameObject frame = Instantiate(_edgePrefab);
            // rotate each edge 90 degrees around the wall, starting from the top
            // even is Y axis, odd is X axis
            frame.transform.rotation = wallObject.transform.rotation * Quaternion.Euler(0, 0, 90 * j);
            Vector3 scl = localDimensions;
            float extent = (j % 2 == 0) ? scl.y * 0.5f : scl.x * 0.5f;
            frame.transform.position = wallObject.transform.position + frame.transform.up * extent;
            float width = (j % 2 == 1) ? scl.y : scl.x;
            frame.transform.localScale = new Vector3(1, 1.0f/scl.y, 1.0f/scl.z);

            WallEdge thisWallEdge = frame.GetComponent<WallEdge>();
            thisWallEdge._parentSurface = wallObject;
            thisWallEdge.AdjustParticleSystemRateAndSize(width);
            wallObject.wallEdges.Add(thisWallEdge);
        }
    }

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
            Transform refTransform = _roomboxWalls[i]._passthroughWall.transform;
            Vector3 wallOut = -_roomboxWalls[i].transform.forward;
            Vector3 worldUp = Vector3.up;
            Vector3.OrthoNormalize(ref wallOut, ref worldUp);
            floorFrame.transform.rotation = Quaternion.LookRotation(worldUp, wallOut);
            ceilingFrame.transform.rotation = Quaternion.LookRotation(-worldUp, wallOut);

            floorFrame.transform.localScale = new Vector3(refTransform.localScale.y, floorFrame.transform.localScale.y, floorFrame.transform.localScale.z);
            ceilingFrame.transform.localScale = floorFrame.transform.localScale;

            floorFrame.transform.position = refTransform.position - refTransform.right * refTransform.localScale.x * 0.5f;
            ceilingFrame.transform.position = refTransform.position + refTransform.right * refTransform.localScale.x * 0.5f;

            WallEdge floorEdge = floorFrame.GetComponent<WallEdge>();
            floorEdge._parentSurface = _roomboxWalls[_roomFloorID];
            floorEdge.AdjustParticleSystemRateAndSize(refTransform.localScale.y);
            floorEdges.Add(floorEdge);

            WallEdge ceilingEdge = ceilingFrame.GetComponent<WallEdge>();
            ceilingEdge._parentSurface = _roomboxWalls[_roomCeilingID];
            ceilingEdge.AdjustParticleSystemRateAndSize(refTransform.localScale.y);
            ceilingEdges.Add(ceilingEdge);
        }

        _roomboxWalls[_roomCeilingID].wallEdges.AddRange(ceilingEdges);
        _roomboxWalls[_roomFloorID].wallEdges.AddRange(floorEdges);
    }

    // grass shrubs around the base of the wall, that appear when it's removed
    void CreateWallDebris(SanctuaryRoomObject wallObject, Vector3 localDimensions)
    {
        // debris along the ground
        int debrisCount = Random.Range(6, 10);
        for (int i = 0; i < debrisCount; i++)
        {
            Vector3 scl = localDimensions;
            GameObject debris = Instantiate(SanctuaryEnvironment.Instance._grassPrefab);
            Vector3 randomPosition = wallObject.transform.position - Vector3.up * scl.x * 0.5f;
            float posToEdge = Random.Range(-0.5f, 0.5f);
            if (i <= 1)
            {
                // put the first two towards the edge, for effect
                posToEdge = Random.Range(0.4f, 0.5f) * (i - 0.5f) * 2;
            }
            randomPosition += wallObject.transform.up * scl.y * posToEdge;
            randomPosition += wallObject.transform.forward * Random.Range(-0.2f, 0.1f);
            Vector3 randomScale = Vector3.one * Random.Range(0.8f, 1.2f) * (Mathf.Abs(posToEdge) + 0.5f);
            debris.transform.position = randomPosition;
            debris.transform.localScale = Vector3.zero;
            debris.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360.0f), 0);
            wallObject.wallDebris.Add(debris);
        }
    }

    // grass shrubs that surround the bases of furniture, that hide if all walls are closed
    void CreateFurnitureDebris(Transform child, Vector3 scl)
    {
        Vector3 basePos = child.transform.position - Vector3.up * scl.y * 0.5f;
        // only do it for furnishings that touch the floor
        if (Mathf.Abs(basePos.y) < 0.1f)
        {
            for (int j = 0; j < 12; j++)
            {
                GameObject debris = Instantiate(SanctuaryEnvironment.Instance._grassPrefab);
                float side = (j % 2) - 0.5f;
                float debrisDiameter = 0.15f;
                float randomPos = Random.Range(-0.5f, 0.5f);

                float rndXsign = Mathf.Sign(Random.Range(0, 2) - 1);
                float rndZsign = Mathf.Sign(Random.Range(0, 2) - 1);

                debris.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360.0f), 0);
                debris.transform.position = basePos;
                Quaternion boxRot = child.transform.rotation;
                Vector3 localOffset1 = new Vector3(side * scl.x * rndXsign - side * debrisDiameter, 0, randomPos * scl.z * rndZsign);
                Vector3 localOffset2 = new Vector3(randomPos * scl.x * rndXsign, 0, side * scl.z * rndZsign - side * debrisDiameter);
                Vector3 offset = (side < 0) ? boxRot * localOffset1 : boxRot * localOffset2;
                debris.transform.position += offset;
                debris.transform.localScale = Vector3.one * Random.Range(0.3f, 0.7f);
                _roomDebris.Add(debris);
            }
        }
    }

    public void SetWallEffectParams(Vector3 objPosition, float effectStrength)
    {
        for (int i = 0; i < _roomboxWalls.Count; i++)
        {
            _roomboxWalls[i]._passthroughWall.material.SetVector("_OzPosition", objPosition);
            _roomboxWalls[i]._passthroughWall.material.SetFloat("_OzRippleStrength", effectStrength);
        }
        for (int i = 0; i < _roomboxFurnishings.Count; i++)
        {
            _roomboxFurnishings[i]._passthroughWall.material.SetVector("_OzPosition", objPosition);
            _roomboxFurnishings[i]._passthroughWall.material.SetFloat("_OzRippleStrength", effectStrength);
        }
    }

    public void SetRoomSaturation(float roomSat)
    {
        for (int i = 0; i < _roomboxWalls.Count; i++)
        {
            _roomboxWalls[i]._passthroughWall.material.SetFloat("_SaturationAmount", roomSat);
        }
        for (int i = 0; i < _roomboxFurnishings.Count; i++)
        {
            _roomboxFurnishings[i]._passthroughWall.material.SetFloat("_SaturationAmount", roomSat);
        }
    }

    public Vector3 GetSimpleFloorPosition()
    {
        if (_roomboxWalls == null) return Vector3.zero;
        if (_roomboxWalls.Count < 2) return Vector3.zero;

        // to be extra safe, spawn it wherever the player is
        SanctuaryRoomObject floor = _roomboxWalls[_roomFloorID];
        if (!floor) return Vector3.zero;
        Vector3 floorPos = new Vector3(SanctuaryExperience.Instance._mainCamera.transform.position.x, floor.transform.position.y, SanctuaryExperience.Instance._mainCamera.transform.position.z);
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

    public bool IsBlockedByWall(Ray directionToSound, float distance)
    {
        bool isBlocked = false;
        LayerMask soundObstructionLayer = LayerMask.GetMask("RoomBox");
        RaycastHit[] roomboxHit = Physics.RaycastAll(directionToSound, distance, soundObstructionLayer);
        // just testing for yes/no, so there's no need for additional data
        if (roomboxHit != null && roomboxHit.Length > 0)
        {
            isBlocked = true;
        }
        return isBlocked;
    }

    public float GetCeilingHeight()
    {
        if (_sceneMesher)
        {
            return _sceneMesher._ceilingHeight;
        }
        return 1.0f;
    }

    public float GetRoomOpenAmount()
    {
        if (_roomboxWalls == null || _roomboxWalls.Count == 0)
        {
            return 1f;
        }
        
        // give the ceiling 50% weight
        float totalAmount = _roomboxWalls[_roomboxWalls.Count - 1]._passthroughWallActive ? 0.0f : 0.5f;

        int wallsOpen = 0;
        for (int i = 0; i < _roomboxWalls.Count-2; i++) // ignore last two walls (floor and ceiling)
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

    public void SetEdgeEffectIntensity(float normValue)
    {
        if (!_sceneMesh) return;
        float intensity = _edgeIntensity.Evaluate(normValue);
        _sceneMesh.material.SetFloat("_EffectIntensity", intensity);
        _sceneMesh.material.SetFloat("_EdgeTimeline", normValue);
    }

    public void SetEffectPosition(Vector3 worldPos, float timer)
    {
        if (!_sceneMesh) return;
        _sceneMesh.material.SetVector("_EffectPosition", worldPos);
        _sceneMesh.material.SetFloat("_EffectRadius", timer * _effectRadius);
    }

    // a bounding-box quick test for placing things outside of the room
    public bool IsPositionInRoom(Vector3 pos, float positionBuffer)
    {
        bool inRoom = false;
        float xMin = 0.0f;
        float xMax = 0.0f;
        float zMin = 0.0f;
        float zMax = 0.0f;
        for (int i = 0; i < _roomboxWalls.Count; i++)
        {
            if (!_roomboxWalls[i]._isWall)
            {
                continue;
            }

            Vector3 wallRight = _roomboxWalls[i].transform.up;
            Vector3 pos1 = _roomboxWalls[i].transform.position - wallRight * _roomboxWalls[i]._passthroughWall.transform.localScale.y * 0.5f;
            Vector3 pos2 = _roomboxWalls[i].transform.position + wallRight * _roomboxWalls[i]._passthroughWall.transform.localScale.y * 0.5f;
            if (pos1.x < xMin) xMin = pos1.x;
            if (pos1.x > xMax) xMax = pos1.x;
            if (pos1.z < zMin) zMin = pos1.z;
            if (pos1.z > zMax) zMax = pos1.z;
        }
        inRoom = (pos.x > xMin - positionBuffer) && (pos.x < xMax + positionBuffer) && (pos.z > zMin - positionBuffer) && (pos.z < zMax + positionBuffer);
        return inRoom;
    }

    public bool IsFloor(int surfaceID)
    {
        return surfaceID == _roomFloorID;
    }
}
