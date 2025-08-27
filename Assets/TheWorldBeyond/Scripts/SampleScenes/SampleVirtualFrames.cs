// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using Meta.XR.MRUtilityKit;
using TheWorldBeyond.SamplePrefabs;
using UnityEngine;

namespace TheWorldBeyond.SampleScenes
{
    public class SampleVirtualFrames : MonoBehaviour
    {
        // if the room doesn't have a DOOR or WINDOW, inform user
        public GameObject ErrorScreen;

        // all virtual content is a Child of this
        public Transform EnvRoot;

        // drop the virtual world this far below the floor anchor
        private const float GOUND_DELTA = 0.02f;

        //public SimpleResizable _doorPrefab;
        //public SimpleResizable _windowPrefab;
        public GameObject ButterflyPrefab;

        private void Awake()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_ANDROID
            OVRManager.eyeFovPremultipliedAlphaModeEnabled = false;
#endif
            // hide error message for later, if Scene has no door or window
            ErrorScreen.SetActive(false);

            // hide the environment so app starts in Passthrough
            EnvRoot.gameObject.SetActive(false);
        }

        public void InitializeRoom()
        {
            var windows = new List<Transform>();
            var doorScripts = new List<SampleDoorDebris>();
            var floorHeight = 0.0f;
            var butterflyAdded = false;

            foreach (var anchor in MRUK.Instance.GetCurrentRoom().Anchors)
            {

                if (anchor.Label is MRUKAnchor.SceneLabels.FLOOR)
                {
                    // move the world slightly below the ground floor, so the virtual floor doesn't Z-fight
                    if (EnvRoot)
                    {
                        var envPos = EnvRoot.transform.position;
                        var groundHeight = anchor.transform.position.y - GOUND_DELTA;
                        EnvRoot.transform.position = new Vector3(envPos.x, groundHeight, envPos.z);
                        floorHeight = anchor.transform.position.y;
                    }
                }
                else if (anchor.Label is (MRUKAnchor.SceneLabels.WINDOW_FRAME or MRUKAnchor.SceneLabels.DOOR_FRAME))
                {
                    windows.Add(anchor.transform);

                    // cache all the door objects, to correctly place the floor fade object
                    if (anchor.GetComponentInChildren<SampleDoorDebris>())
                    {
                        doorScripts.Add(anchor.GetComponentInChildren<SampleDoorDebris>());
                    }

                    // spawn an optional butterfly
                    if (anchor.Label is MRUKAnchor.SceneLabels.DOOR_FRAME && ButterflyPrefab && !butterflyAdded)
                    {
                        var butterfly = Instantiate(ButterflyPrefab, anchor.transform);
                        butterfly.transform.localPosition = Vector3.zero;
                        butterfly.transform.rotation = Quaternion.LookRotation(anchor.transform.forward, Vector3.up);
                        // this is to only spawn one, regardless of door count
                        butterflyAdded = true;
                    }
                }
            }

            // adjust floor fades for each door
            foreach (var door in doorScripts)
            {
                door.AdjustFloorFade(floorHeight);
            }

            if (windows.Count == 0)
            {
                ErrorScreen.SetActive(true);
            }
            else
            {
                EnvRoot.gameObject.SetActive(true);
            }
        }
    }
}
