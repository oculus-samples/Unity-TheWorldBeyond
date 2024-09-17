// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using UnityEngine;
using Color = UnityEngine.Color;

public class AmbSfx_Manager : MonoBehaviour
{
    static public AmbSfx_Manager Instance = null;

    [NonSerialized]
    static public AmbSfx[] AmbSfxList = null;

    private bool _isPlaying;

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
        AmbSfxList = FindObjectsOfType<AmbSfx>();
        _audioListener = FindObjectOfType<AudioListener>();
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
                    _isPlaying = true;
                }
                else
                {
                    AmbSfxList[i].Stop();
                    _isPlaying = false;
                }
            }
        }
    }

    // Update is called once per frame
    public void DoLateUpdate()
    {
        if (!_isPlaying)
        {
            if (WorldBeyondManager.Instance._currentChapter == WorldBeyondManager.GameChapter.TheGreatBeyond)
            {
                SetEnabled(true);
            }
        }
        else
        {
            if (WorldBeyondManager.Instance._currentChapter == WorldBeyondManager.GameChapter.Title)
            {
                SetEnabled(false);
            }
        }
        // If the lister is blocked by a wall stop the emitter.
        HandleObstructed();
    }

    #region OBSTRUCTION_MANAGER

    private static Plane _plane;
    private static Ray _ray;

    public void HandleObstructed()
    {
        // Handle Ambient SFX Emitters and Walls
        foreach (var ambAudioSource in AudioManager.AmbPool)
        {
            if (!ambAudioSource) continue;

            var heading = ambAudioSource.transform.position - _audioListener.transform.position;

            var distance = heading.magnitude;
            var direction = heading / distance;

            if (!(_audioListener is null))
            {
                _ray.origin = _audioListener.transform.position;
                _ray.direction = direction;
                if (!VirtualRoom.Instance.IsBlockedByWall(_ray, distance))
                {
                    Debug.DrawRay(_ray.origin, _ray.direction * distance, Color.green);
                    ambAudioSource.mute = false;
                }
                else
                {
                    Debug.DrawRay(_ray.origin, _ray.direction * distance, Color.red);
                    ambAudioSource.mute = true;
                }
            }
        }
    }

    #endregion

}
