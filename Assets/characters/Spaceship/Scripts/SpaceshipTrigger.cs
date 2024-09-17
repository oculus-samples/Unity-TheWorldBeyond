// Copyright (c) Meta Platforms, Inc. and affiliates.

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
