using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneEnvironment : MonoBehaviour
{
    public GameObject _wallPrefab;
    public GameObject _grassPrefab;
    public Transform _envRoot;
    List<SanctuaryRoomObject> _roomboxWalls = new List<SanctuaryRoomObject>();
    int _roomFloorID = 0;
    int _roomCeilingID = 0;

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
                    _envRoot.position = new Vector3(_envRoot.position.x, instance.transform.position.y - 0.005f, _envRoot.position.z);
                }
                else if (instance.classification.labels[0] == "CEILING")
                {
                    _roomCeilingID = _roomboxWalls.Count;
                }
                _roomboxWalls.Add(sro);

                if (instance.classification.labels[0] == "WALL_FACE")
                {
                    sro._isWall = true;
                }

                // fake Guardian
                sro._guardianWall.gameObject.SetActive(false);

                // the collider that the wall toggler uses
                if (sro.GetComponent<BoxCollider>())
                {
                    sro.GetComponent<BoxCollider>().size = new Vector3(sceneObjects[i].dimensions.x, sceneObjects[i].dimensions.y, 0.01f);
                }
            }
        }

        CreatePolygonMesh(_roomboxWalls[_roomFloorID], false);
        CreatePolygonMesh(_roomboxWalls[_roomCeilingID], true);

        // cull foreground objects
        ForegroundObject[] foregroundObjects = GetComponentsInChildren<ForegroundObject>();
        foreach (ForegroundObject obj in foregroundObjects)
        {
            if (IsPositionInRoom(obj.transform.position, 2.0f))
            {
                Destroy(obj.gameObject);
            }
        }
    }

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
        if (!IsListCW(cornerPoints))
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
