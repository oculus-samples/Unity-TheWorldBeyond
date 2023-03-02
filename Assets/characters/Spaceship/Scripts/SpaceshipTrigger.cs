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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
[CustomEditor(typeof(SpaceshipTrigger))]
public class SpaceshipTriggerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Space(EditorGUIUtility.singleLineHeight);
        if (GUILayout.Button("Trigger Animation"))
        {
            (target as SpaceshipTrigger).TriggerAnim();
        }
        if (GUILayout.Button("Reset Animation"))
        {
            (target as SpaceshipTrigger).ResetAnim();
        }
    }
}
#endif
public class SpaceshipTrigger : MonoBehaviour
{
    public Animator[] Animators;
    public SoundEntry sfxIdle;
    public SoundEntry sfxTakeOff;
    public List<GameObject> lightObjects;

    void Start()
    {
        FindLightObjects();
        EnableCostlyEffects(false);
    }

    public void TriggerAnim()
    {
        EnableCostlyEffects(true);
        foreach (Animator a in Animators)
        {
            a.Play("FlyAway", 0);
        }
        sfxIdle.Stop();
        sfxTakeOff.Play();
    }

    public void ResetAnim()
    {
        EnableCostlyEffects(false);
        sfxIdle.Stop();
        foreach (Animator a in Animators)
        {
            a.Play("Idle", 0);
        }
        sfxIdle.Play();
    }

    public void StartIdleSound()
    {
        sfxIdle.Play();
    }
    
    public void StopIdleSound()
    {
        sfxIdle.Stop();
    }
    
    public void FindLightObjects()
    {
        CheckForLight(this.transform);
    }

    public void CheckForLight(Transform xform)
    {
        if (xform.gameObject.name == "RimLightShaft" ||
            xform.gameObject.name == "RoundLightShaft" ||
            xform.gameObject.name == "QuadLightShaft")
        {
            lightObjects.Add(xform.gameObject);
        }

        foreach (Transform child in xform)
        {
            CheckForLight(child);
        }
    }

    public void EnableCostlyEffects(bool doEnable)
    {
        foreach (GameObject go in lightObjects)
        {
            go.SetActive(doEnable);
        }
    }
}
