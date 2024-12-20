// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

namespace TheWorldBeyond.Audio
{
    // Definition for a sound class to add to prefabs. Contains clips, parameters and source
    public class SoundEntry : MonoBehaviour
    {
        public string DisplayName;
        private Vector3 m_location;
        public AudioSource AudioSourceComponent;

        [Header("Use Ambiance Pooling System")]
        public bool UseAmbiancePool = false;
        [FormerlySerializedAs("MixerGroupChoice")] public AudioMixerGroup OneShotFireForgetMixerGroupChoice;

        [Header("Occlusion Settings")]
        public float WallOcclusionDuckingAmt;
        public bool WallOcclusionMuteEmitter;

        public bool UsesWallOcclusion => (WallOcclusionDuckingAmt != 0f) || WallOcclusionMuteEmitter;

        public bool IsPlaying => AudioSourceComponent.isPlaying;

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

        [Header("Playback Properties")]
        // Fade In/Out
        public float FadeInMs;
        public float FadeOutMs;
        public float DelayMs;
        public bool IsLooping;

        [Range(0f, 2f)]
        public float Volume = 1.0f;

        private float m_defaultVolume = 1.0f;
        private float m_defaultPitch = 1.0f;
        private AudioClip m_currentClip;

        // Volume Randomization
        [Range(0f, 0.5f)]
        public float VolumeVariance = 0f;
        public float LowVolRnd => VolumeVariance > 0.0f ? 1.0f - VolumeVariance : 0.0f;
        public float HighVolRnd => VolumeVariance > 0.0f ? 1.0f + VolumeVariance : 0.0f;

        [Range(.1f, 3f)]
        public float Pitch = 1.0f;

        // Pitch Randomization
        [Range(0f, 0.5f)]
        public float PitchVariance = 0f;
        public float LowPitchRnd => PitchVariance > 0.0f ? 1.0f - PitchVariance : 0.0f;    //The lowest a sound effect will be randomly pitched.
        public float HighPitchRnd => PitchVariance > 0.0f ? 1.0f + PitchVariance : 0.0f;    //The highest a sound effect will be randomly pitched.

        public List<AudioClip> AudioClips = new();

        public SoundEntry()
        {

        }

        public void SetCurrentClip(AudioClip clipIn)
        {
            m_currentClip = clipIn;
        }

        public AudioClip GetCurrentClip()
        {
            return m_currentClip;
        }

        public void Play(
        Vector3 positionIn = default,
            Transform transformToFollow = null)
        {
            AudioSourceComponent.enabled = true;
            AudioManager.PlaySoundEntry(this, "SoundEntry::Play()", position: positionIn, transformToFollow: transformToFollow);
            SoundEntry_Manager.RegisterEntry(this);
        }

        public void Stop()
        {
            SoundEntry_Manager.DeregisterEntry(this);
            AudioManager.Stop(AudioSourceComponent);
        }

        public void Pause()
        {
            SoundEntry_Manager.DeregisterEntry(this);
            AudioManager.Pause(AudioSourceComponent);
        }

        public void Resume()
        {
            AudioManager.UnPause(AudioSourceComponent);
            SoundEntry_Manager.RegisterEntry(this);
        }

        public void SetVolume(float volIn)
        {
            Volume = volIn;
            AudioSourceComponent.volume = volIn;
        }

        public void SetPitch(float pitchIn)
        {
            Pitch = pitchIn;
            AudioSourceComponent.pitch = pitchIn;
        }

        public void ResetVolume()
        {
            SetVolume(m_defaultVolume);
        }

        public void ResetPitch()
        {
            SetPitch(m_defaultPitch);
        }

        public void Awake()
        {
            m_defaultPitch = Pitch;
            m_defaultVolume = Volume;
            if (AudioSourceComponent is null)
            {
                Debug.LogError($"Audio Source Is Null for sound: {DisplayName}");
            }
        }

        public void SetOccluded(bool value)
        {
            if (!value)
            {
                ResetVolume();
                AudioSourceComponent.mute = false;
                return;
            }

            SetVolume(m_defaultVolume - WallOcclusionDuckingAmt);
            AudioSourceComponent.mute = WallOcclusionMuteEmitter;
        }
    }
}
