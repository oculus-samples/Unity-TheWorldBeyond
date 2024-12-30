// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using TheWorldBeyond.Environment.RoomEnvironment;
using TheWorldBeyond.GameManagement;
using UnityEngine;

namespace TheWorldBeyond.Environment
{
    public class WorldBeyondEnvironment : MonoBehaviour
    {
        public static WorldBeyondEnvironment Instance = null;
        public GameObject GrassPrefab;

        public Light Sun;
        public GameObject EnvRoot;
        public GameObject[] EnvObjects;

        // chance of a cell being occupied with an object, 0-1
        public Texture2D GrassDensityMap;
        // how wide the whole grid is, in meters
        public float MapCoverage = 20.0f;

        // the grid is divided into cells
        private int m_cells = 20;
        private GameObject[,] m_worldObjects;

        public AudioSource OutdoorAudio;

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
        private void SpawnObjectsWithMap()
        {
            if (!GrassDensityMap)
            {
                return;
            }

            m_worldObjects = new GameObject[GrassDensityMap.width, GrassDensityMap.height];
            m_cells = GrassDensityMap.width;
            var cellSize = MapCoverage / m_cells;
            var cHalf = cellSize * 0.5f;
            var centerOffset = new Vector3(-MapCoverage * 0.5f, 0, -MapCoverage * 0.5f) + new Vector3(cellSize * 0.5f, 0, cellSize * 0.5f);
            for (var x = 0; x < m_cells; x++)
            {
                for (var y = 0; y < m_cells; y++)
                {
                    var pixelColor = GrassDensityMap.GetPixel(x, y);
                    var spawnDebris = Random.Range(0.0f, 1.0f) <= pixelColor.r;
                    var cellCenter = centerOffset + new Vector3(x * cellSize, 0, y * cellSize);
                    var randomOffset = new Vector3(Random.Range(-cHalf, cHalf), 0, Random.Range(-cHalf, cHalf));
                    var desiredPosition = cellCenter + randomOffset + WorldBeyondManager.Instance.GetFloorHeight() * Vector3.up;
                    if (!VirtualRoom.Instance.IsPositionInRoom(desiredPosition, 0.5f) && spawnDebris)
                    {
                        var newObj = Instantiate(GrassPrefab, EnvRoot.transform);
                        newObj.transform.position = desiredPosition;
                        newObj.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360.0f), 0);
                        newObj.transform.localScale = new Vector3(Random.Range(0.7f, 1.3f), Random.Range(0.5f, 1.5f), Random.Range(0.7f, 1.3f));
                        m_worldObjects[x, y] = newObj;
                    }
                    else
                    {
                        m_worldObjects[x, y] = null;
                    }
                }
            }
        }

        /// <summary>
        /// If an object contains the ForegroundObject component and is inside the room, destroy it.
        /// </summary>
        private void CullForegroundObjects()
        {
            var foregroundObjects = GetComponentsInChildren<ForegroundObject>();
            foreach (var obj in foregroundObjects)
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
            foreach (var obj in EnvObjects)
            {
                obj.SetActive(doShow);
            }
        }

        /// <summary>
        /// Turn on/off outdoor audio if all passthrough walls are closed.
        /// </summary>
        public void SetOutdoorAudioParams(Vector3 position, bool wallOpen)
        {
            if (!OutdoorAudio)
            {
                return;
            }

            OutdoorAudio.transform.position = position;
            if (wallOpen)
            {
                OutdoorAudio.volume = 1.0f;
                OutdoorAudio.Play();
            }
            else
            {
                OutdoorAudio.Stop();
            }
        }

        public void FadeOutdoorAudio(float volume)
        {
            if (OutdoorAudio)
            {
                OutdoorAudio.volume = volume;
            }
        }

        /// <summary>
        /// When the player first grabs the flashlight, turn on the main sun.
        /// </summary>
        public void FlickerSun()
        {
            _ = StartCoroutine(FlickerSunCoroutine());
        }

        private IEnumerator FlickerSunCoroutine()
        {
            var timer = 0.0f;
            var flickerTimer = 0.5f;
            while (timer <= flickerTimer)
            {
                timer += Time.deltaTime;
                var normTimer = Mathf.Clamp01(timer / flickerTimer);
                Sun.intensity = Mathf.Abs(normTimer - 0.5f) + 0.5f;
                yield return null;
            }
        }

        /// <summary>
        /// Adjust the entire world to the ground height of the floor anchor.
        /// </summary>
        public void MoveGroundFloor(float height)
        {
            foreach (var obj in EnvObjects)
            {
                obj.transform.position = new Vector3(obj.transform.position.x, height, obj.transform.position.z);
            }
        }
    }
}
