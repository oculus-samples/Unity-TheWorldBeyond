// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using TheWorldBeyond.Environment.RoomEnvironment;
using TheWorldBeyond.GameManagement;
using UnityEngine;
using UnityEngine.Audio;
using Color = UnityEngine.Color;

namespace TheWorldBeyond.Audio
{
    public class SoundEntry_Manager : MonoBehaviour
    {
        public AudioMixer AudioMixer => AudioManager.Instance.AudioMixer;
        public static SoundEntry_Manager Instance = null;

        [NonSerialized]
        public static List<SoundEntry> SoundEntryList = null;

        public static float AmbDuckVolume = -9f;
        public static float AmbFilterCutoff = 1000f;

        private bool m_isPlaying;
        private float m_audioTimer;

        private AudioListener m_audioListener;
        private Vector3 m_position = default;

        public Vector3 Position
        {
            get
            {
                if (m_position == default)
                {
                    m_position = GetComponent<Transform>().position;
                }

                return m_position;
            }
        }

        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
            }
        }

        // Start is called before the first frame update
        private void Start()
        {
            SoundEntryList = new List<SoundEntry>();
            m_audioListener = FindObjectOfType<AudioListener>();
        }

        // Update is called once per frame
        public void DoLateUpdate()
        {
            if (WorldBeyondManager.Instance.CurrentChapter == WorldBeyondManager.GameChapter.OppyExploresReality)
            {
                HandleObstructed();
            }
            if (WorldBeyondManager.Instance.CurrentChapter == WorldBeyondManager.GameChapter.TheGreatBeyond)
            {
                HandleObstructed();
            }
            if (WorldBeyondManager.Instance.CurrentChapter == WorldBeyondManager.GameChapter.Ending)
            {
                HandleObstructed();
            }
        }

        #region OBSTRUCTION_MANAGER

        private static Plane s_plane;
        private static Ray s_ray;

        public void HandleObstructed()
        {
            if (SoundEntryList is null) return;

            // Handle Ambient SFX Emitters and Walls
            foreach (var sfxSoundEntry in SoundEntryList)
            {
                if (sfxSoundEntry is null) continue;
                if (!sfxSoundEntry.IsPlaying) continue;
                if (!sfxSoundEntry.UsesWallOcclusion) continue;

                var heading = sfxSoundEntry.transform.position - m_audioListener.transform.position;

                var distance = heading.magnitude;
                var direction = heading / distance;

                if (m_audioListener is not null)
                {
                    s_ray.origin = m_audioListener.transform.position;
                    s_ray.direction = direction;
                    if (!VirtualRoom.Instance.IsBlockedByWall(s_ray, distance))
                    {
                        Debug.DrawRay(s_ray.origin, s_ray.direction * distance, Color.yellow);
                        sfxSoundEntry.SetOccluded(false);
                    }
                    else
                    {
                        Debug.DrawRay(s_ray.origin, s_ray.direction * distance, Color.red);
                        sfxSoundEntry.SetOccluded(true);
                    }
                }
            }
        }

        public static void RegisterEntry(SoundEntry soundEntryIn)
        {
            if (soundEntryIn is null) return;
            if (SoundEntryList is null) return;
            if (!soundEntryIn.UsesWallOcclusion) return;
            SoundEntryList.Add(soundEntryIn);
        }

        public static void DeregisterEntry(SoundEntry soundEntryIn)
        {
            if (soundEntryIn is null) return;
            if (SoundEntryList is null) return;
            if (!soundEntryIn.UsesWallOcclusion) return;

            _ = SoundEntryList.Remove(soundEntryIn);
        }

        #endregion

    }
}
