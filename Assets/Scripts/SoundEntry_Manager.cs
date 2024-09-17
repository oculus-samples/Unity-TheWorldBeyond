// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Color = UnityEngine.Color;

public class SoundEntry_Manager : MonoBehaviour
{
    public AudioMixer audioMixer => AudioManager.Instance._audioMixer;
    static public SoundEntry_Manager Instance = null;

    [NonSerialized]
    static public List<SoundEntry> SoundEntryList = null;

    public static float AmbDuckVolume = -9f;
    public static float AmbFilterCutoff = 1000f;

    private bool _isPlaying;
    private float _audioTimer;

    private AudioListener _audioListener;
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

    private void Awake()
    {
        if (!Instance)
        {
            Instance = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        SoundEntryList = new List<SoundEntry>();
        _audioListener = FindObjectOfType<AudioListener>();
    }

    // Update is called once per frame
    public void DoLateUpdate()
    {
        if (WorldBeyondManager.Instance._currentChapter == WorldBeyondManager.GameChapter.OppyExploresReality)
        {
            HandleObstructed();
        }
        if (WorldBeyondManager.Instance._currentChapter == WorldBeyondManager.GameChapter.TheGreatBeyond)
        {
            HandleObstructed();
        }
        if (WorldBeyondManager.Instance._currentChapter == WorldBeyondManager.GameChapter.Ending)
        {
            HandleObstructed();
        }
    }

    #region OBSTRUCTION_MANAGER

    private static Plane _plane;
    private static Ray _ray;

    public void HandleObstructed()
    {
        if (SoundEntryList is null) return;

        // Handle Ambient SFX Emitters and Walls
        foreach (var sfxSoundEntry in SoundEntryList)
        {
            if (sfxSoundEntry is null) continue;
            if (!sfxSoundEntry.IsPlaying) continue;
            if (!sfxSoundEntry.UsesWallOcclusion) continue;

            var heading = sfxSoundEntry.transform.position - _audioListener.transform.position;

            var distance = heading.magnitude;
            var direction = heading / distance;

            if (!(_audioListener is null))
            {
                _ray.origin = _audioListener.transform.position;
                _ray.direction = direction;
                if (!VirtualRoom.Instance.IsBlockedByWall(_ray, distance))
                {
                    Debug.DrawRay(_ray.origin, _ray.direction * distance, Color.yellow);
                    sfxSoundEntry.SetOccluded(false);
                }
                else
                {
                    Debug.DrawRay(_ray.origin, _ray.direction * distance, Color.red);
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

        SoundEntryList.Remove(soundEntryIn);
    }

    #endregion

}
