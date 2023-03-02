/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections.Generic;
using Audio.Scripts;
using UnityEngine;

[System.Serializable]

public class OppyAudio : MonoBehaviour
{
    [NamedArray (new string[]
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
