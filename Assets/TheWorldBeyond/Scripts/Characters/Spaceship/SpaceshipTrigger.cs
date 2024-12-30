// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using TheWorldBeyond.Audio;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace TheWorldBeyond.Character
{
#if UNITY_EDITOR
    [CustomEditor(typeof(SpaceshipTrigger))]
    public class SpaceshipTriggerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            _ = DrawDefaultInspector();
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
        public SoundEntry SfxIdle;
        public SoundEntry SfxTakeOff;
        public List<GameObject> LightObjects;

        private void Start()
        {
            FindLightObjects();
            EnableCostlyEffects(false);
        }

        public void TriggerAnim()
        {
            EnableCostlyEffects(true);
            foreach (var a in Animators)
            {
                a.Play("FlyAway", 0);
            }
            SfxIdle.Stop();
            SfxTakeOff.Play();
        }

        public void ResetAnim()
        {
            EnableCostlyEffects(false);
            SfxIdle.Stop();
            foreach (var a in Animators)
            {
                a.Play("Idle", 0);
            }
            SfxIdle.Play();
        }

        public void StartIdleSound()
        {
            SfxIdle.Play();
        }

        public void StopIdleSound()
        {
            SfxIdle.Stop();
        }

        public void FindLightObjects()
        {
            CheckForLight(transform);
        }

        public void CheckForLight(Transform xform)
        {
            if (xform.gameObject.name is "RimLightShaft" or
                "RoundLightShaft" or
                "QuadLightShaft")
            {
                LightObjects.Add(xform.gameObject);
            }

            foreach (Transform child in xform)
            {
                CheckForLight(child);
            }
        }

        public void EnableCostlyEffects(bool doEnable)
        {
            foreach (var go in LightObjects)
            {
                go.SetActive(doEnable);
            }
        }
    }
}
