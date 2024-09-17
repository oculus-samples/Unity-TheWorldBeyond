// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using Audio.Scripts;
using UnityEngine;

[System.Serializable]

public class OppyAudio : MonoBehaviour
{
    [NamedArray(new string[]
    {
        "0 Footstep Run",
        "1 Footstep Walk",
        "2 Footstep Jump",
        "3 Stand EyeBlink",
        "4 Wave",
        "5 Run Away",
        "6 Look Around Start",
        "7 Look Around Stop",
        "8 Like Start",
        "9 Like Stop",
        "10 Eat",
        "11 PowerUp",
        "12 Dislike Start",
        "13 Listen Short",
        "14 Listen Long",
        "15 Listen Fail",
        "16 Pet"
    })]
    public List<SoundEntry> SoundEntries = new List<SoundEntry>();

    public void PlaySound(int soundIndex)
    {
        if (soundIndex < SoundEntries.Count)
        {
            SoundEntries[soundIndex].Play();
        }
        else
        {
            Debug.Log("Error: invalid sound index");
        }
    }

    public void StopSound(int soundIndex)
    {
        if (soundIndex < SoundEntries.Count)
        {
            SoundEntries[soundIndex].Stop();
        }
        else
        {
            Debug.Log("Error: invalid sound index");
        }
    }
}
