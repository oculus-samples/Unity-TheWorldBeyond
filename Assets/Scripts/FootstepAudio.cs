using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FootstepAudio : MonoBehaviour {
  //This will need to be replaced to eventually reference either audio manager or audio trigger prefabs.
  [SerializeField] private AudioClip[] walkArray;
  [SerializeField] private AudioClip[] runArray;
  [SerializeField] private AudioClip[] jumpArray;

  AudioSource OzAudioSource;

  private void Awake() {
    OzAudioSource = GetComponent<AudioSource>();
  }

  //I've forgotten how to use case statements, the following seems really inefficient.
  private void walkStep() {
    AudioClip clip = GetRandomWalkClip();
    OzAudioSource.clip = clip;
    OzAudioSource.time = 0.0f;
    OzAudioSource.Play();
    //OzAudioSource.PlayOneShot(clip);
  }

  private void runStep() {
    AudioClip clip = GetRandomRunClip();
    OzAudioSource.clip = clip;
    OzAudioSource.time = 0.0f;
    OzAudioSource.Play();
    //OzAudioSource.PlayOneShot(clip);
  }

  private void jumpStep() {
    AudioClip clip = GetRandomJumpClip();
    OzAudioSource.clip = clip;
    OzAudioSource.time = 0.0f;
    OzAudioSource.Play();
    //OzAudioSource.PlayOneShot(clip);
  }


  private AudioClip GetRandomWalkClip() {
    return walkArray[UnityEngine.Random.Range(0, walkArray.Length)];
  }

  private AudioClip GetRandomRunClip() {
    return runArray[UnityEngine.Random.Range(0, runArray.Length)];
  }

  private AudioClip GetRandomJumpClip() {
    return jumpArray[UnityEngine.Random.Range(0, jumpArray.Length)];
  }
}
