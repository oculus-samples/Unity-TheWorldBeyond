// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

// this is designed for fire-and-forget, non-looping sounds
public class AudioManager : MonoBehaviour
{
    // STATIC COMPONENTS
    public static AudioManager Instance = null;
    private static int _audioSourcesBufferPoolSize = 10;
    public static float WallFilterWallOpeness = 22050f - 373f;
    public static float WallVolumeWallOpeness;

    [NonSerialized] private static SoundEntry_Manager _soundEntryManager;
    [NonSerialized] private static AmbSfx_Manager _ambSfxManager;

    // Static Audio Sources
    private static AudioSource[] _audioSourcesPool;  // audioSources or 'emitter' pool
    private static readonly AudioSource AudioSourceGlobal = new AudioSource();
    private static AudioSource[] _audioSourcesPoolAmb;  // audioSources or 'emitter' pool
    private static readonly AudioSource AudioSourceGlobalAmb = new AudioSource();
    public static AudioSource[] AmbPool => _audioSourcesPoolAmb;
    public AudioSource GlobalAudioSource => AudioSourceGlobal;

    [NonSerialized] public AudioSource CurrentAudioSource;
    [NonSerialized] public SoundEntry CurrentSound;

    [Header("Snapshot Transitions")]
    private static int _currentPoolIndex = 0;  // Index of the currently playing AudioSource from the pool
    public AudioMixer _audioMixer;
    public AudioMixerGroup _defaultOutputMixerGroup;
    public int TransitionTimeToTitle = 1;
    public AudioMixerSnapshot[] MixSnapshot_Title;
    public int TransitionTimeToIntro = 1;
    public AudioMixerSnapshot[] MixSnapshot_Introduction;
    public int TransitionTimeToOppyExploresReality = 1;
    public AudioMixerSnapshot[] OppyExploresReality;
    public int TransitionTimeToGreatBeyond = 1;
    public AudioMixerSnapshot[] MixSnapshot_TheGreatBeyond;
    public int TransitionTimeToTheGreatBeyond_AfterPowerup = 1;
    public AudioMixerSnapshot[] MixSnapshot_TheGreatBeyond_AfterPowerup;
    private static float[] _weights = new float[1] { 1f };
    public int TransitionTimeToEnding = 1;
    public AudioMixerSnapshot[] MixSnapshot_Ending;
    public int TransitionTimeToReset = 1;
    public AudioMixerSnapshot[] MixSnapshot_Reset;

    [Header("Virtual Room Openness")]
    public float openessFilterRange = 22050 - 375f;
    public float openessVolRange = 9f;
    public float openessReverbRange = 1500f;

    [NonSerialized] public static float Openness;
    private float _audioTimer;

    public static void SetSnapshot_Title()
    {
        Instance._audioMixer.TransitionToSnapshots(Instance.MixSnapshot_Title, _weights, Instance.TransitionTimeToTitle);
        Instance._audioMixer.SetFloat("OccAmbVol", 0f);
    }

    public static void SetSnapshot_Introduction()
    {
        Instance._audioMixer.SetFloat("OccAmbVol", 0f);
        Instance._audioMixer.TransitionToSnapshots(Instance.MixSnapshot_Introduction, _weights, Instance.TransitionTimeToIntro);
    }

    public static void SetSnapshot_TheGreatBeyond()
    {
        Instance._audioMixer.TransitionToSnapshots(Instance.MixSnapshot_TheGreatBeyond, _weights, Instance.TransitionTimeToGreatBeyond);
    }

    public static void SetSnapshot_Ending()
    {
        Instance._audioMixer.TransitionToSnapshots(Instance.MixSnapshot_Ending, _weights, Instance.TransitionTimeToEnding);
    }

    public static void SetSnapshot_Reset()
    {
        Instance._audioMixer.SetFloat("OccAmbVol", 0f);
        Instance._audioMixer.TransitionToSnapshots(Instance.MixSnapshot_Reset, _weights, Instance.TransitionTimeToReset);
    }
    public static void SetSnapshot_OppyExploresReality()
    {
        Instance._audioMixer.SetFloat("OccAmbVol", 0f);
        Instance._audioMixer.TransitionToSnapshots(Instance.OppyExploresReality, _weights, Instance.TransitionTimeToOppyExploresReality);
    }

    public static void SetSnapshot_TheGreatBeyond_AfterPowerup()
    {
        Instance._audioMixer.SetFloat("OccAmbVol", 0f);
        Instance._audioMixer.TransitionToSnapshots(Instance.MixSnapshot_TheGreatBeyond_AfterPowerup, _weights, Instance.TransitionTimeToTheGreatBeyond_AfterPowerup);
    }

    private void Start()
    {
        Instance._audioMixer.SetFloat("OccAmbVol", 0f);
        _soundEntryManager = SoundEntry_Manager.Instance;
        _ambSfxManager = AmbSfx_Manager.Instance;

        SetSnapshot_Title();
        _audioSourcesPool = new AudioSource[_audioSourcesBufferPoolSize];
        _audioSourcesPoolAmb = new AudioSource[_audioSourcesBufferPoolSize];
        for (int i = 0; i < _audioSourcesBufferPoolSize; i++)
        {
            _audioSourcesPool[i] = CreateNewAudioSource();
            _audioSourcesPool[i].spatialize = true;
            _audioSourcesPool[i].outputAudioMixerGroup = _defaultOutputMixerGroup;

            _audioSourcesPoolAmb[i] = CreateNewAudioSource();
            if (!AudioSourceGlobalAmb)
            {
                continue;
            }
            if (AudioSourceGlobalAmb.outputAudioMixerGroup)
            {
                _audioSourcesPoolAmb[i].outputAudioMixerGroup = AudioSourceGlobalAmb.outputAudioMixerGroup;
            }
            _audioSourcesPoolAmb[i].minDistance = AudioSourceGlobalAmb.minDistance;
            _audioSourcesPoolAmb[i].maxDistance = AudioSourceGlobalAmb.maxDistance;
            _audioSourcesPoolAmb[i].spatialize = true;

        }
    }

    // Old
    public void PlayAudio(
        AudioClip clipToPlay,
        Vector3 position = default(Vector3),
        Transform transformToFollow = null,
        float pitch = 1.0f,
        float volume = 1.0f,
        float delayInMs = 0.0f,
        AudioMixerGroup useMixerGroup = null,
        bool useAmbiancePool = false,
        float minDistanceIn = 0f,
        float maxDistanceIn = 0f)
    {
        if (clipToPlay is null)
        {
            return;
        }

        var audioSourcePoolChoice = useAmbiancePool ? _audioSourcesPoolAmb : _audioSourcesPool;

        // get first free one
        for (int i = 0; i < audioSourcePoolChoice.Length; i++)
        {

            if (!audioSourcePoolChoice[i])
            {
                // if for some reason it's null, that's because it was attached to an object that was destroyed
                audioSourcePoolChoice[i] = CreateNewAudioSource();
                audioSourcePoolChoice[i].spatialize = true;
            }
            if (audioSourcePoolChoice[i].isPlaying)
            {
                continue;
            }
            else
            {
                audioSourcePoolChoice[i].volume = volume;
                audioSourcePoolChoice[i].pitch = pitch;
                audioSourcePoolChoice[i].clip = clipToPlay;
                audioSourcePoolChoice[i].time = 0.0f;
                audioSourcePoolChoice[i].outputAudioMixerGroup = useMixerGroup;
                audioSourcePoolChoice[i].spatialize = true;
                audioSourcePoolChoice[i].Play();
                if (useAmbiancePool)
                {
                    audioSourcePoolChoice[i].minDistance = minDistanceIn;
                    audioSourcePoolChoice[i].maxDistance = maxDistanceIn;
                }
                if (transformToFollow)
                {
                    audioSourcePoolChoice[i].transform.parent = transformToFollow;
                    audioSourcePoolChoice[i].transform.localPosition = Vector3.zero;
                }
                else
                {
                    audioSourcePoolChoice[i].transform.position = position;
                }

                break;
            }
        }
    }

    static AudioSource CreateNewAudioSource()
    {
        GameObject newAudio = new GameObject("AudioSource");
        AudioSource audioSrc = newAudio.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;
        return audioSrc;
    }

    public static void PlaySoundEntry(
        SoundEntry soundEntryIn,
        string debugMsg = "",
        Vector3 position = default(Vector3),
        Transform transformToFollow = null)
    {
        if (soundEntryIn.AudioClips is null)
        {
            Debug.LogError($"AudioManager::PlayRnd -{soundEntryIn.displayName} No Clips Provided : {debugMsg}");
            return;
        }

        Instance.CurrentAudioSource = soundEntryIn.AudioSourceComponent;

        if (Instance.CurrentAudioSource is null)
        {
            Debug.LogAssertion($"AudioManager::PlayRnd -{soundEntryIn.displayName} No Audio Source Provided using Global Source : {debugMsg}");
            Instance.CurrentAudioSource = AudioSourceGlobal;
        }

        // Set Looping
        Instance.CurrentAudioSource.loop = soundEntryIn.IsLooping;

        //Choose a random pitch to play back our clip at between our high and low pitch ranges.
        Instance.CurrentAudioSource.pitch = RandomizeWithBase(soundEntryIn.Pitch, soundEntryIn.PitchVariance);

        //Choose a random volume to play back our clip at between our high and low volume ranges.
        Instance.CurrentAudioSource.volume = RandomizeWithBase(soundEntryIn.Volume, soundEntryIn.VolumeVariance);

        // Play one shot
        AudioClip clip = soundEntryIn.AudioClips[Random.Range(0, soundEntryIn.AudioClips.Count)];
        Instance.CurrentAudioSource.clip = clip;
        if (soundEntryIn.AudioClips.Count > 1)
        {
            var currentClip = soundEntryIn.GetCurrentClip();
            int whileCount = 0;

            while (clip != currentClip && (whileCount < 5))
            {
                clip = soundEntryIn.AudioClips[Random.Range(0, soundEntryIn.AudioClips.Count)];
                whileCount++;
            }
        }
        soundEntryIn.SetCurrentClip(clip);

        if (soundEntryIn.IsLooping)
        {
            if (soundEntryIn.DelayMs > 0f)
                Instance.CurrentAudioSource.PlayDelayed(soundEntryIn.DelayMs);
            else
                Instance.CurrentAudioSource.Play((ulong)soundEntryIn.DelayMs);
        }
        else
        {
            // When using a given position - utilize a pooled emitter.
            if (position != default(Vector3))
            {
                Instance.PlayAudio(clip, position, transformToFollow,
                    pitch: soundEntryIn.Pitch,
                    volume: soundEntryIn.Volume, delayInMs: soundEntryIn.DelayMs,
                    useMixerGroup: soundEntryIn.OneShotFireForgetMixerGroupChoice,
                    useAmbiancePool: soundEntryIn.UseAmbiancePool,
                    minDistanceIn: soundEntryIn.AudioSourceComponent.minDistance,
                    maxDistanceIn: soundEntryIn.AudioSourceComponent.maxDistance);
            }
            else
            {
                if (soundEntryIn.DelayMs > 0f)
                    Instance.CurrentAudioSource.PlayDelayed(soundEntryIn.DelayMs);
                else
                    Instance.CurrentAudioSource.PlayOneShot(clip);
            }
        }

        soundEntryIn.AudioSourceComponent = Instance.CurrentAudioSource;
    }

    public static void Play(
        AudioClip audioClipIn,
        AudioSource audioSourceIn = null,
        bool looped = false,
        float delayInMs = 0.0f,
        float basePitchValue = 1.0f,
        float pitchRndRange = 0.0f,
        float baseVolValue = 1.0f,
        float volRndRange = 0.0f,
        string debugMsg = "",
        Vector3 position = default(Vector3),
        Transform transformToFollow = null)
    {
        if (audioSourceIn is null)
        {
            Debug.LogAssertion($"AudioManager::PlayRnd - No Audio Source Provided using Global Source : {debugMsg}");
            audioSourceIn = AudioSourceGlobal;
        }

        if (audioClipIn is null)
        {
            Debug.LogAssertion($"AudioManager::Play - No Clip Provided : {debugMsg}");
            return;
        }

        Instance.CurrentAudioSource = audioSourceIn;

        // Set Looping
        audioSourceIn.loop = looped;

        //Choose a random pitch to play back our clip at between our high and low pitch ranges.
        audioSourceIn.pitch = RandomizeWithBase(basePitchValue, pitchRndRange);

        //Choose a random volume to play back our clip at between our high and low volume ranges.
        audioSourceIn.volume = RandomizeWithBase(baseVolValue, volRndRange);

        // Play one shot
        audioSourceIn.clip = audioClipIn;
        if (looped)
        {
            if (delayInMs > 0f)
                audioSourceIn.PlayDelayed(delayInMs);
            else
                audioSourceIn.Play((ulong)delayInMs);
        }
        else
        {
            // When using a given position - utilize a pooled emitter.
            if (position != default(Vector3))
            {
                Instance.PlayAudio(audioClipIn, position, transformToFollow, pitch: basePitchValue, volume: baseVolValue, delayInMs: delayInMs);
            }
            else
            {
                if (delayInMs > 0f)
                    audioSourceIn.PlayDelayed(delayInMs);
                else
                    audioSourceIn.PlayOneShot(audioClipIn);
            }
        }
    }

    public static void Stop(AudioSource audioSourceIn)
    {
        audioSourceIn.Stop();
    }

    public static void Pause(AudioSource audioSourceIn)
    {
        audioSourceIn.Pause();
    }

    public static void UnPause(AudioSource audioSourceIn)
    {
        audioSourceIn.UnPause();
    }

    private static float RandomizeWithBase(float baseValue = 1.0f, float varianceValue = 0.0f)
    {
        return Random.Range(baseValue - varianceValue, baseValue + varianceValue);
    }

    void Awake()
    {
        //Check if there is already an instance of SoundManager
        if (Instance == null)
        {
            //if not, set it to this.
            Instance = this;
        }
        //If instance already exists:
        else if (Instance != this)
        {
            //Destroy this, this enforces our singleton pattern so there can only be one instance of SoundManager.
            Destroy(gameObject);
        }

        //Set SoundManager to DontDestroyOnLoad so that it won't be destroyed when reloading our scene.
        DontDestroyOnLoad(gameObject);
    }

    IEnumerator PlayFadeIn(AudioSource fadeAudioSource, float fadeSeconds = 0f, float delaySeconds = 0f, float fadeVolIn = 1f)
    {
        yield return new WaitForSeconds(delaySeconds);

        float startVolume = 0.2f;

        fadeAudioSource.volume = 0;
        fadeAudioSource.Play();

        while (fadeAudioSource.volume < 1.0f)
        {
            fadeAudioSource.volume += startVolume * Time.deltaTime / fadeSeconds;

            yield return null;
        }

        fadeAudioSource.volume = 1f;

    }
    IEnumerator StopFadeOut(AudioSource fadeAudioSource, float fadeSeconds = 0f, float delaySeconds = 0f)
    {
        if (fadeAudioSource is null)
        {
            yield break;
        }

        if (!fadeAudioSource.isPlaying)
        {
            yield break;
        }

        yield return new WaitForSeconds(delaySeconds);

        float startVolume = fadeAudioSource.volume;

        while (fadeAudioSource.volume > 0)
        {
            fadeAudioSource.volume -= startVolume * Time.deltaTime / fadeSeconds;

            yield return null;
        }

        fadeAudioSource.Stop();
        fadeAudioSource.volume = startVolume;
    }

    public static void SetRoomOpenness(float openness)
    {
        Openness = openness;
        var filter_amt = (22050f - Instance.openessFilterRange) + (Openness * Instance.openessFilterRange);
        var vol_amt = -Instance.openessVolRange + (AudioManager.Openness * Instance.openessVolRange);
        var reverb_amt = (-Instance.openessReverbRange + (Openness * Instance.openessReverbRange));

        Instance._audioMixer.SetFloat("AmbianceOpenness", reverb_amt);
        Instance._audioMixer.SetFloat("OccAmbVol", vol_amt);
        Instance._audioMixer.SetFloat("OccAmbCutoff", filter_amt);
    }

    void LateUpdate()
    {
        // Check every 10 frames
        _audioTimer += Time.deltaTime;
        if (_audioTimer <= 0.25f)
        {
            return;
        };
        _audioTimer = 0f;

        Openness = VirtualRoom.Instance.GetRoomOpenAmount();
        _soundEntryManager.DoLateUpdate();
        _ambSfxManager.DoLateUpdate();
    }
}

public class AudioSounds
{
    public List<AudioClip> AudioClips;
}
