// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class AmbSfx : MonoBehaviour
{
    #region PublicVariables
    [Header("Randomized Positioning")]
    public bool randomizePosition = true;
    public Vector3 positionRandomizationOffset;

    [Header("Randomization Timing")]
    public float triggerTime = 8f;
    public float triggerTimeVariation = 4f;

    [Header("Playback Behavior")]
    public bool layered = false;
    public List<SoundEntry> soundEntryList;
    public AudioSource audioSource = null;
    [HideInInspector] public Vector3 _positionToPlayAt;

    [Header("Debug Info")]
    public bool debugSpheresEnabled;
    public GameObject RandomizationSphere;
    #endregion

    #region  Private Variables
    private SoundEntry _currentSoundEntry;
    private bool _isPlaying;
    private float _waitTime;
    private Vector3 _positionOriginal;
    private float _timer;
    private Vector3 _position = default(Vector3);
    #endregion
    public Vector3 position
    {
        get
        {
            if (_position != default(Vector3))
            {
                _position = GetComponent<Transform>().position;
            }

            return _position;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        _positionOriginal = GetComponent<Transform>().position;
        UpdateDebugSphere();
        print($"{name} - Original Position: {_positionOriginal}");
        Play();
    }

    private float RandomizeNegative(float value)
    {
        float mod = 1f;
        if (Random.Range(-1f, 1f) < 0f)
        {
            mod = -1f;
        }

        return mod * value;
    }
    private void GenerateNewRandomPosition()
    {
        if (!randomizePosition)
        {
            return;
        }
        float new_x = _positionOriginal.x + RandomizeNegative(Random.Range(0f, positionRandomizationOffset.x));
        float new_y = _positionOriginal.y + RandomizeNegative(Random.Range(0f, positionRandomizationOffset.y));
        float new_z = _positionOriginal.z + RandomizeNegative(Random.Range(0f, positionRandomizationOffset.z));

        _positionToPlayAt.x = new_x;
        _positionToPlayAt.y = new_y;
        _positionToPlayAt.z = new_z;
        Debug.LogWarning($"[{name}] - Original Position: {_positionOriginal} <::::> Randomized position: {_positionToPlayAt}");
    }

    public void Play()
    {
        _timer = 0f;
        RecalculateTriggerTime();
        _isPlaying = true;
    }

    public void Stop()
    {
        _timer = 0f;
        _isPlaying = false;
    }


    private void RecalculateTriggerTime()
    {
        _waitTime = Random.Range(triggerTime - triggerTimeVariation, triggerTime + triggerTimeVariation);
    }

    // Update is called once per frame
    void Update()
    {
        if (!_isPlaying)
        {
            return;
        }

        int i;
        _timer += Time.deltaTime;
        if (_timer > _waitTime)
        {
            GenerateNewRandomPosition();

            // Debug Sphere
            UpdateDebugSphere();
            if (layered)
            {
                // Play all sounds from the sound entry list
                for (i = 0; i < soundEntryList.Count; i++)
                {
                    _currentSoundEntry = null;
                    soundEntryList[i].Play(_positionToPlayAt);
                }
            }
            else
            {
                // Pick a random sound from the sound entry list
                _currentSoundEntry = soundEntryList[Random.Range(0, soundEntryList.Count)];
                _currentSoundEntry.Play(_positionToPlayAt);
            }

            _timer = 0f;
            RecalculateTriggerTime();
        }
    }

    public void SetBlockedByWall(bool blocked)
    {
        if (layered)
        {
            for (var i = 0; i < soundEntryList.Count; i++)
            {
                if (blocked)
                {
                    soundEntryList[i].SetVolume(0f);
                }
                else
                {
                    soundEntryList[i].ResetVolume();
                }
            }
        }
        else
        {
            if (!_currentSoundEntry)
            {
                return;
            }
            if (blocked)
            {
                _currentSoundEntry.SetVolume(0f);
            }
            else
            {
                _currentSoundEntry.ResetVolume();
            }
        }
    }

    private void UpdateDebugSphere()
    {
        if (!RandomizationSphere)
        {
            return;
        }

        if (!debugSpheresEnabled)
        {
            RandomizationSphere.SetActive(false);
            return;
        }
        RandomizationSphere.SetActive(true);
        var transformScale = RandomizationSphere.transform.lossyScale;
        transformScale.x = positionRandomizationOffset.x * 2;
        transformScale.y = positionRandomizationOffset.y * 2;
        transformScale.z = positionRandomizationOffset.z * 2;
        RandomizationSphere.transform.localScale = transformScale;
    }
}
