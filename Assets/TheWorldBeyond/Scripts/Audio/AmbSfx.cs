// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TheWorldBeyond.Audio
{
    public class AmbSfx : MonoBehaviour
    {
        #region PublicVariables
        [Header("Randomized Positioning")]
        public bool RandomizePosition = true;
        public Vector3 PositionRandomizationOffset;

        [Header("Randomization Timing")]
        public float TriggerTime = 8f;
        public float TriggerTimeVariation = 4f;

        [Header("Playback Behavior")]
        public bool Layered = false;
        public List<SoundEntry> SoundEntryList;
        public AudioSource AudioSource = null;
        [HideInInspector] public Vector3 PositionToPlayAt;

        [Header("Debug Info")]
        public bool DebugSpheresEnabled;
        public GameObject RandomizationSphere;
        #endregion

        #region  Private Variables
        private SoundEntry m_currentSoundEntry;
        private bool m_isPlaying;
        private float m_waitTime;
        private Vector3 m_positionOriginal;
        private float m_timer;
        private Vector3 m_position = default;
        #endregion
        public Vector3 Position
        {
            get
            {
                if (m_position != default)
                {
                    m_position = GetComponent<Transform>().position;
                }

                return m_position;
            }
        }

        // Start is called before the first frame update
        private void Start()
        {
            m_positionOriginal = GetComponent<Transform>().position;
            UpdateDebugSphere();
            print($"{name} - Original Position: {m_positionOriginal}");
            Play();
        }

        private float RandomizeNegative(float value)
        {
            var mod = 1f;
            if (Random.Range(-1f, 1f) < 0f)
            {
                mod = -1f;
            }

            return mod * value;
        }
        private void GenerateNewRandomPosition()
        {
            if (!RandomizePosition)
            {
                return;
            }
            var new_x = m_positionOriginal.x + RandomizeNegative(Random.Range(0f, PositionRandomizationOffset.x));
            var new_y = m_positionOriginal.y + RandomizeNegative(Random.Range(0f, PositionRandomizationOffset.y));
            var new_z = m_positionOriginal.z + RandomizeNegative(Random.Range(0f, PositionRandomizationOffset.z));

            PositionToPlayAt.x = new_x;
            PositionToPlayAt.y = new_y;
            PositionToPlayAt.z = new_z;
            Debug.Log($"[{name}] - Original Position: {m_positionOriginal} <::::> Randomized Position: {PositionToPlayAt}");
        }

        public void Play()
        {
            m_timer = 0f;
            RecalculateTriggerTime();
            m_isPlaying = true;
        }

        public void Stop()
        {
            m_timer = 0f;
            m_isPlaying = false;
        }


        private void RecalculateTriggerTime()
        {
            m_waitTime = Random.Range(TriggerTime - TriggerTimeVariation, TriggerTime + TriggerTimeVariation);
        }

        // Update is called once per frame
        private void Update()
        {
            if (!m_isPlaying)
            {
                return;
            }

            int i;
            m_timer += Time.deltaTime;
            if (m_timer > m_waitTime)
            {
                GenerateNewRandomPosition();

                // Debug Sphere
                UpdateDebugSphere();
                if (Layered)
                {
                    // Play all sounds from the sound entry list
                    for (i = 0; i < SoundEntryList.Count; i++)
                    {
                        m_currentSoundEntry = null;
                        SoundEntryList[i].Play(PositionToPlayAt);
                    }
                }
                else
                {
                    // Pick a random sound from the sound entry list
                    m_currentSoundEntry = SoundEntryList[Random.Range(0, SoundEntryList.Count)];
                    m_currentSoundEntry.Play(PositionToPlayAt);
                }

                m_timer = 0f;
                RecalculateTriggerTime();
            }
        }

        public void SetBlockedByWall(bool blocked)
        {
            if (Layered)
            {
                for (var i = 0; i < SoundEntryList.Count; i++)
                {
                    if (blocked)
                    {
                        SoundEntryList[i].SetVolume(0f);
                    }
                    else
                    {
                        SoundEntryList[i].ResetVolume();
                    }
                }
            }
            else
            {
                if (!m_currentSoundEntry)
                {
                    return;
                }
                if (blocked)
                {
                    m_currentSoundEntry.SetVolume(0f);
                }
                else
                {
                    m_currentSoundEntry.ResetVolume();
                }
            }
        }

        private void UpdateDebugSphere()
        {
            if (!RandomizationSphere)
            {
                return;
            }

            if (!DebugSpheresEnabled)
            {
                RandomizationSphere.SetActive(false);
                return;
            }
            RandomizationSphere.SetActive(true);
            var transformScale = RandomizationSphere.transform.lossyScale;
            transformScale.x = PositionRandomizationOffset.x * 2;
            transformScale.y = PositionRandomizationOffset.y * 2;
            transformScale.z = PositionRandomizationOffset.z * 2;
            RandomizationSphere.transform.localScale = transformScale;
        }
    }
}
