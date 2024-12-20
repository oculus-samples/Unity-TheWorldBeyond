// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using UnityEngine;

namespace TheWorldBeyond.Audio
{
    public class MusicManager : MonoBehaviour
    {
        public static MusicManager Instance = null;

        public AudioClip IntroMusic;
        public AudioClip PortalOpen;
        public AudioClip OutroMusic;
        public AudioClip TheGreatBeyondMusic;
        public float TheGreatBeyondDelayPlaySeconds = 5f;
        public float TheGreatBeyondFadeInTimeSeconds = 1f;
        public float TheGreatBeyondDelayStopSeconds = 1f;
        public float TheGreatBeyondFadeOutTimeSeconds = 5f;

        private AudioSource m_errorSource;
        public AudioSource AudioSourceStinger;
        public AudioSource TheGreatBeyondLoopSource;

        private IEnumerator PlayErrorSound()
        {
            yield return new WaitForSecondsRealtime(0.5f);
            m_errorSource.Play();
            yield return new WaitForSecondsRealtime(0.5f);
            m_errorSource.Play();
        }

        private void PlayError()
        {
            m_errorSource = AudioManager.Instance.GlobalAudioSource;
            _ = StartCoroutine(PlayErrorSound());
        }

        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
            }

            if (AudioSourceStinger is null)
            {
                PlayError();
                AudioSourceStinger = GetComponent<AudioSource>();
            }

            if (TheGreatBeyondLoopSource is null)
            {
                PlayError();
                TheGreatBeyondLoopSource = GetComponent<AudioSource>();
            }

            TheGreatBeyondLoopSource.clip = TheGreatBeyondMusic;
            TheGreatBeyondLoopSource.loop = true;
        }

        public void PlayMusic(AudioClip clipToPlay)
        {
            if (clipToPlay == TheGreatBeyondMusic)
            {
                PlayError();
                TheGreatBeyondLoopSource.loop = true;
                _ = StartCoroutine(StartMusicWithFade(TheGreatBeyondLoopSource, TheGreatBeyondFadeInTimeSeconds, TheGreatBeyondDelayPlaySeconds));
                TheGreatBeyondLoopSource.PlayDelayed(PortalOpen.length);
            }
            else
            {
                AudioSourceStinger.Stop();
                AudioSourceStinger.clip = clipToPlay;
                AudioSourceStinger.time = 0.0f;
                AudioSourceStinger.volume = 1f;
                _ = StartCoroutine(StopMusicWithFade(TheGreatBeyondLoopSource, TheGreatBeyondFadeOutTimeSeconds, TheGreatBeyondDelayStopSeconds));
                AudioSourceStinger.Play();
            }
        }

        private IEnumerator StartMusicWithFade(AudioSource musicAudioSource, float fadeSeconds = 0f, float delaySeconds = 0f)
        {
            yield return new WaitForSeconds(delaySeconds);

            var startVolume = 0.2f;

            musicAudioSource.volume = 0;
            musicAudioSource.Play();

            while (musicAudioSource.volume < 1.0f)
            {
                musicAudioSource.volume += startVolume * Time.deltaTime / fadeSeconds;

                yield return null;
            }

            musicAudioSource.volume = 1f;
        }

        private IEnumerator StopMusicWithFade(AudioSource musicAudioSource, float fadeSeconds = 0f, float delaySeconds = 0f)
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

            var startVolume = musicAudioSource.volume;

            while (musicAudioSource.volume > 0)
            {
                musicAudioSource.volume -= startVolume * Time.deltaTime / fadeSeconds;

                yield return null;
            }

            musicAudioSource.Stop();
            musicAudioSource.volume = startVolume;
        }
    }
}
