// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using UnityEngine;

public class WorldBeyondEnvironment : MonoBehaviour
{
    static public WorldBeyondEnvironment Instance = null;
    public GameObject _grassPrefab;

    public Light _sun;
    public GameObject _envRoot;
    public GameObject[] _envObjects;

    // chance of a cell being occupied with an object, 0-1
    public Texture2D _grassDensityMap;
    // how wide the whole grid is, in meters
    public float _mapCoverage = 20.0f;
    // the grid is divided into cells
    int _cells = 20;

    GameObject[,] _worldObjects;

    public AudioSource _outdoorAudio;

    private void Awake()
    {
        if (!Instance)
        {
            Instance = this;
        }
    }

    public void Initialize()
    {
        CullForegroundObjects();
        SpawnObjectsWithMap();
    }

    /// <summary>
    /// Using a density map, spawn grass shrubs in the world, except in the room defined by Scene.
    /// </summary>
    void SpawnObjectsWithMap()
    {
        if (!_grassDensityMap)
        {
            return;
        }

        _worldObjects = new GameObject[_grassDensityMap.width, _grassDensityMap.height];
        _cells = _grassDensityMap.width;
        float cellSize = _mapCoverage / _cells;
        float cHalf = cellSize * 0.5f;
        Vector3 centerOffset = new Vector3(-_mapCoverage * 0.5f, 0, -_mapCoverage * 0.5f) + new Vector3(cellSize * 0.5f, 0, cellSize * 0.5f);
        for (int x = 0; x < _cells; x++)
        {
            for (int y = 0; y < _cells; y++)
            {
                Color pixelColor = _grassDensityMap.GetPixel(x, y);
                bool spawnDebris = Random.Range(0.0f, 1.0f) <= pixelColor.r;
                Vector3 cellCenter = centerOffset + new Vector3(x * cellSize, 0, y * cellSize);
                Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-cHalf, cHalf), 0, UnityEngine.Random.Range(-cHalf, cHalf));
                Vector3 desiredPosition = cellCenter + randomOffset + WorldBeyondManager.Instance.GetFloorHeight() * Vector3.up;
                if (!VirtualRoom.Instance.IsPositionInRoom(desiredPosition, 0.5f) && spawnDebris)
                {
                    GameObject newObj = Instantiate(_grassPrefab, _envRoot.transform);
                    newObj.transform.position = desiredPosition;
                    newObj.transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360.0f), 0);
                    newObj.transform.localScale = new Vector3(Random.Range(0.7f, 1.3f), Random.Range(0.5f, 1.5f), Random.Range(0.7f, 1.3f));
                    _worldObjects[x, y] = newObj;
                }
                else
                {
                    _worldObjects[x, y] = null;
                }
            }
        }
    }

    /// <summary>
    /// If an object contains the ForegroundObject component and is inside the room, destroy it.
    /// </summary>
    void CullForegroundObjects()
    {
        ForegroundObject[] foregroundObjects = GetComponentsInChildren<ForegroundObject>();
        foreach (ForegroundObject obj in foregroundObjects)
        {
            if (VirtualRoom.Instance.IsPositionInRoom(obj.transform.position, 2.0f))
            {
                Destroy(obj.gameObject);
            }
        }
    }

    /// <summary>
    /// Toggle the virtual world's objects.
    /// </summary>
    public void ShowEnvironment(bool doShow)
    {
        foreach (GameObject obj in _envObjects)
        {
            obj.SetActive(doShow);
        }
    }

    /// <summary>
    /// Turn on/off outdoor audio if all passthrough walls are closed.
    /// </summary>
    public void SetOutdoorAudioParams(Vector3 position, bool wallOpen)
    {
        if (!_outdoorAudio)
        {
            return;
        }

        _outdoorAudio.transform.position = position;
        if (wallOpen)
        {
            _outdoorAudio.volume = 1.0f;
            _outdoorAudio.Play();
        }
        else
        {
            _outdoorAudio.Stop();
        }
    }

    public void FadeOutdoorAudio(float volume)
    {
        if (_outdoorAudio)
        {
            _outdoorAudio.volume = volume;
        }
    }

    /// <summary>
    /// When the player first grabs the flashlight, turn on the main sun.
    /// </summary>
    public void FlickerSun()
    {
        StartCoroutine(FlickerSunCoroutine());
    }

    IEnumerator FlickerSunCoroutine()
    {
        float timer = 0.0f;
        float flickerTimer = 0.5f;
        while (timer <= flickerTimer)
        {
            timer += Time.deltaTime;
            float normTimer = Mathf.Clamp01(timer / flickerTimer);
            _sun.intensity = Mathf.Abs(normTimer - 0.5f) + 0.5f;
            yield return null;
        }
    }

    /// <summary>
    /// Adjust the entire world to the ground height of the floor anchor.
    /// </summary>
    public void MoveGroundFloor(float height)
    {
        foreach (GameObject obj in _envObjects)
        {
            obj.transform.position = new Vector3(obj.transform.position.x, height, obj.transform.position.z);
        }
    }
}
