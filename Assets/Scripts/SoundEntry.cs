// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

// Definition for a sound class to add to prefabs. Contains clips, parameters and source
public class SoundEntry : MonoBehaviour
{
    public string displayName;
    private Vector3 _location;
    public AudioSource AudioSourceComponent;

    [Header("Use Ambiance Pooling System")]
    public bool UseAmbiancePool = false;
    [FormerlySerializedAs("MixerGroupChoice")] public AudioMixerGroup OneShotFireForgetMixerGroupChoice;

    [Header("Occlusion Settings")]
    public float wallOcclusionDuckingAmt;
    public bool wallOcclusionMuteEmitter;

    public bool UsesWallOcclusion => ((wallOcclusionDuckingAmt != 0f) || wallOcclusionMuteEmitter);

    public bool IsPlaying => AudioSourceComponent.isPlaying;

    private Vector3 _position = default(Vector3);

    public Vector3 position
    {
        get
        {
            if (_position == default(Vector3))
            {
                _position = GetComponent<Transform>().position;
            }

            return _position;
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

    private float _defaultVolume = 1.0f;
    private float _defaultPitch = 1.0f;
    private AudioClip _currentClip;

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

    public List<AudioClip> AudioClips = new List<AudioClip>();

    public SoundEntry()
    {

    }

    public void SetCurrentClip(AudioClip clipIn)
    {
        _currentClip = clipIn;
    }

    public AudioClip GetCurrentClip()
    {
        return _currentClip;
    }

    public void Play(
        Vector3 positionIn = default(Vector3),
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
        SetVolume(_defaultVolume);
    }

    public void ResetPitch()
    {
        SetPitch(_defaultPitch);
    }

    public void Awake()
    {
        _defaultPitch = Pitch;
        _defaultVolume = Volume;
        if (AudioSourceComponent is null)
        {
            Debug.LogError($"Audio Source Is Null for sound: {displayName}");
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

        SetVolume(_defaultVolume - wallOcclusionDuckingAmt);
        AudioSourceComponent.mute = wallOcclusionMuteEmitter;
    }
}
