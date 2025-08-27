// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using TheWorldBeyond.Environment.RoomEnvironment;
using TheWorldBeyond.GameManagement;
using UnityEngine;
using Color = UnityEngine.Color;

namespace TheWorldBeyond.Audio
{
    public class AmbSfx_Manager : MonoBehaviour
    {
        public static AmbSfx_Manager Instance = null;

        [NonSerialized]
        public static AmbSfx[] AmbSfxList = null;

        private bool m_isPlaying;

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
            AmbSfxList = FindObjectsByType<AmbSfx>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            m_audioListener = FindFirstObjectByType<AudioListener>();
        }

        public void SetEnabled(bool isEnabled = true)
        {
            if (AmbSfxList.Length > 0)
            {
                for (var i = 0; i < AmbSfxList.Length; i++)
                {
                    if (isEnabled)
                    {
                        AmbSfxList[i].Play();
                        m_isPlaying = true;
                    }
                    else
                    {
                        AmbSfxList[i].Stop();
                        m_isPlaying = false;
                    }
                }
            }
        }

        // Update is called once per frame
        public void DoLateUpdate()
        {
            if (!m_isPlaying)
            {
                if (WorldBeyondManager.Instance.CurrentChapter == WorldBeyondManager.GameChapter.TheGreatBeyond)
                {
                    SetEnabled(true);
                }
            }
            else
            {
                if (WorldBeyondManager.Instance.CurrentChapter == WorldBeyondManager.GameChapter.Title)
                {
                    SetEnabled(false);
                }
            }
            // If the lister is blocked by a wall stop the emitter.
            HandleObstructed();
        }

        #region OBSTRUCTION_MANAGER

        private static Plane s_plane;
        private static Ray s_ray;

        public void HandleObstructed()
        {
            // Handle Ambient SFX Emitters and Walls
            foreach (var ambAudioSource in AudioManager.AmbPool)
            {
                if (!ambAudioSource) continue;

                var heading = ambAudioSource.transform.position - m_audioListener.transform.position;

                var distance = heading.magnitude;
                var direction = heading / distance;

                if (m_audioListener is not null)
                {
                    s_ray.origin = m_audioListener.transform.position;
                    s_ray.direction = direction;
                    if (!VirtualRoom.Instance.IsBlockedByWall(s_ray, distance))
                    {
                        Debug.DrawRay(s_ray.origin, s_ray.direction * distance, Color.green);
                        ambAudioSource.mute = false;
                    }
                    else
                    {
                        Debug.DrawRay(s_ray.origin, s_ray.direction * distance, Color.red);
                        ambAudioSource.mute = true;
                    }
                }
            }
        }

        #endregion

    }
}
