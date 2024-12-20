// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace TheWorldBeyond.Audio
{
    public class FootstepAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip[] m_walkArray;
        [SerializeField] private AudioClip[] m_runArray;
        [SerializeField] private AudioClip[] m_jumpArray;
        private AudioSource m_oppyAudioSource;

        private void Awake()
        {
            m_oppyAudioSource = GetComponent<AudioSource>();
        }

        private void WalkStep()
        {
            var clip = GetRandomWalkClip();
            m_oppyAudioSource.clip = clip;
            m_oppyAudioSource.time = 0.0f;
            m_oppyAudioSource.Play();
        }

        private void RunStep()
        {
            var clip = GetRandomRunClip();
            m_oppyAudioSource.clip = clip;
            m_oppyAudioSource.time = 0.0f;
            m_oppyAudioSource.Play();
        }

        private void JumpStep()
        {
            var clip = GetRandomJumpClip();
            m_oppyAudioSource.clip = clip;
            m_oppyAudioSource.time = 0.0f;
            m_oppyAudioSource.Play();
        }

        private AudioClip GetRandomWalkClip()
        {
            return m_walkArray[Random.Range(0, m_walkArray.Length)];
        }

        private AudioClip GetRandomRunClip()
        {
            return m_runArray[Random.Range(0, m_runArray.Length)];
        }

        private AudioClip GetRandomJumpClip()
        {
            return m_jumpArray[Random.Range(0, m_jumpArray.Length)];
        }
    }
}
