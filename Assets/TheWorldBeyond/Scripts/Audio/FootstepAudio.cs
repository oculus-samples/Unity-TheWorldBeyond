// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

public class FootstepAudio : MonoBehaviour
{
    [SerializeField] private AudioClip[] walkArray;
    [SerializeField] private AudioClip[] runArray;
    [SerializeField] private AudioClip[] jumpArray;

    AudioSource _oppyAudioSource;

    private void Awake()
    {
        _oppyAudioSource = GetComponent<AudioSource>();
    }

    private void walkStep()
    {
        AudioClip clip = GetRandomWalkClip();
        _oppyAudioSource.clip = clip;
        _oppyAudioSource.time = 0.0f;
        _oppyAudioSource.Play();
    }

    private void runStep()
    {
        AudioClip clip = GetRandomRunClip();
        _oppyAudioSource.clip = clip;
        _oppyAudioSource.time = 0.0f;
        _oppyAudioSource.Play();
    }

    private void jumpStep()
    {
        AudioClip clip = GetRandomJumpClip();
        _oppyAudioSource.clip = clip;
        _oppyAudioSource.time = 0.0f;
        _oppyAudioSource.Play();
    }

    private AudioClip GetRandomWalkClip()
    {
        return walkArray[UnityEngine.Random.Range(0, walkArray.Length)];
    }

    private AudioClip GetRandomRunClip()
    {
        return runArray[UnityEngine.Random.Range(0, runArray.Length)];
    }

    private AudioClip GetRandomJumpClip()
    {
        return jumpArray[UnityEngine.Random.Range(0, jumpArray.Length)];
    }
}
