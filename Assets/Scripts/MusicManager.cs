// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    static public MusicManager Instance = null;

    public AudioClip IntroMusic;
    public AudioClip PortalOpen;
    public AudioClip OutroMusic;
    public AudioClip TheGreatBeyondMusic;
    public float TheGreatBeyondDelayPlaySeconds = 5f;
    public float TheGreatBeyondFadeInTimeSeconds = 1f;
    public float TheGreatBeyondDelayStopSeconds = 1f;
    public float TheGreatBeyondFadeOutTimeSeconds = 5f;

    private AudioSource _errorSource;
    public AudioSource audioSourceStinger;
    public AudioSource theGreatBeyondLoopSource;

    IEnumerator PlayErrorSound()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        _errorSource.Play();
        yield return new WaitForSecondsRealtime(0.5f);
        _errorSource.Play();
    }

    void PlayError()
    {
        _errorSource = AudioManager.Instance.GlobalAudioSource;
        StartCoroutine(PlayErrorSound());
    }

    private void Awake()
    {
        if (!Instance)
        {
            Instance = this;
        }

        if (audioSourceStinger is null)
        {
            PlayError();
            audioSourceStinger = GetComponent<AudioSource>();
        }

        if (theGreatBeyondLoopSource is null)
        {
            PlayError();
            theGreatBeyondLoopSource = GetComponent<AudioSource>();
        }

        theGreatBeyondLoopSource.clip = TheGreatBeyondMusic;
        theGreatBeyondLoopSource.loop = true;
    }

    public void PlayMusic(AudioClip clipToPlay)
    {
        if (clipToPlay == TheGreatBeyondMusic)
        {
            PlayError();
            theGreatBeyondLoopSource.loop = true;
            StartCoroutine(StartMusicWithFade(theGreatBeyondLoopSource, TheGreatBeyondFadeInTimeSeconds, TheGreatBeyondDelayPlaySeconds));
            theGreatBeyondLoopSource.PlayDelayed(PortalOpen.length);
        }
        else
        {
            audioSourceStinger.Stop();
            audioSourceStinger.clip = clipToPlay;
            audioSourceStinger.time = 0.0f;
            audioSourceStinger.volume = 1f;
            StartCoroutine(StopMusicWithFade(theGreatBeyondLoopSource, TheGreatBeyondFadeOutTimeSeconds, TheGreatBeyondDelayStopSeconds));
            audioSourceStinger.Play();
        }
    }

    IEnumerator StartMusicWithFade(AudioSource musicAudioSource, float fadeSeconds = 0f, float delaySeconds = 0f)
    {
        yield return new WaitForSeconds(delaySeconds);

        float startVolume = 0.2f;

        musicAudioSource.volume = 0;
        musicAudioSource.Play();

        while (musicAudioSource.volume < 1.0f)
        {
            musicAudioSource.volume += startVolume * Time.deltaTime / fadeSeconds;

            yield return null;
        }

        musicAudioSource.volume = 1f;
    }


    IEnumerator StopMusicWithFade(AudioSource musicAudioSource, float fadeSeconds = 0f, float delaySeconds = 0f)
    {
        if (musicAudioSource is null)
        {
            yield break;
        }

        if (!musicAudioSource.isPlaying)
        {
            yield break;
        }

        yield return new WaitForSeconds(delaySeconds);

        float startVolume = musicAudioSource.volume;

        while (musicAudioSource.volume > 0)
        {
            musicAudioSource.volume -= startVolume * Time.deltaTime / fadeSeconds;

            yield return null;
        }

        musicAudioSource.Stop();
        musicAudioSource.volume = startVolume;
    }
}
