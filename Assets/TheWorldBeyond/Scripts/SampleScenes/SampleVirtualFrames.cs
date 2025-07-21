// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using TheWorldBeyond.SamplePrefabs;
using UnityEngine;
#pragma warning disable CS0618 // Type or member is obsolete

namespace TheWorldBeyond.SampleScenes
{
    public class SampleVirtualFrames : MonoBehaviour
    {
        public OVRSceneManager SceneManager;

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

            SceneManager.SceneModelLoadedSuccessfully += InitializeRoom;
        }

        private void InitializeRoom()
        {
            var sceneAnchors = FindObjectsOfType<OVRSceneAnchor>();
            if (sceneAnchors != null)
            {
                var windows = new List<Transform>();
                var doorScripts = new List<SampleDoorDebris>();
                var floorHeight = 0.0f;
                var butterflyAdded = false;

                for (var i = 0; i < sceneAnchors.Length; i++)
                {
                    var instance = sceneAnchors[i];
                    var classification = instance.GetComponent<OVRSemanticClassification>();

                    if (classification.Contains(OVRSceneManager.Classification.Floor))
                    {
                        // move the world slightly below the ground floor, so the virtual floor doesn't Z-fight
                        if (EnvRoot)
                        {
                            var envPos = EnvRoot.transform.position;
                            var groundHeight = instance.transform.position.y - GOUND_DELTA;
                            EnvRoot.transform.position = new Vector3(envPos.x, groundHeight, envPos.z);
                            floorHeight = instance.transform.position.y;
                        }
                    }
                    else if (classification.Contains(OVRSceneManager.Classification.WindowFrame) ||
                        classification.Contains(OVRSceneManager.Classification.DoorFrame))
                    {
                        windows.Add(instance.transform);

                        //SimpleResizer resizer = new SimpleResizer();
                        //SimpleResizable prefab = classification.Contains(OVRSceneManager.Classification.DoorFrame) ? _doorPrefab : _windowPrefab;
                        _ = instance.transform.GetChild(0).localScale;

                        // cache all the door objects, to correctly place the floor fade object
                        if (instance.GetComponent<SampleDoorDebris>())
                        {
                            doorScripts.Add(instance.GetComponent<SampleDoorDebris>());
                        }

                        // spawn an optional butterfly
                        if (classification.Contains(OVRSceneManager.Classification.DoorFrame) && ButterflyPrefab && !butterflyAdded)
                        {
                            var butterfly = Instantiate(ButterflyPrefab, instance.transform);
                            butterfly.transform.localPosition = Vector3.zero;
                            butterfly.transform.rotation = Quaternion.LookRotation(instance.transform.forward, Vector3.up);
                            // this is to only spawn one, regardless of door count
                            butterflyAdded = true;
                        }

                        // the Resizer scales the mesh so that the bounds are flush with the window extents
                        // in this case, we want the mesh frame to extend "outside" of the extents, so we adjust it
                        // as well, the vines on the door also require special treatment
                        //if (prefab.GetComponent<ResizablePadding>())
                        //{
                        //    dimensions += prefab.GetComponent<ResizablePadding>().DimensionPadding;
                        //}
                        //resizer.CreateResizedObject(dimensions, sceneAnchors[i].gameObject, prefab);
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
}
