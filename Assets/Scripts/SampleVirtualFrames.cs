// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;

public class SampleVirtualFrames : MonoBehaviour
{
    public OVRSceneManager _sceneManager;

    // if the room doesn't have a DOOR or WINDOW, inform user
    public GameObject _errorScreen;

    // all virtual content is a child of this
    public Transform _envRoot;

    // drop the virtual world this far below the floor anchor
    const float _groundDelta = 0.02f;

    //public SimpleResizable _doorPrefab;
    //public SimpleResizable _windowPrefab;
    public GameObject _butterflyPrefab;

    void Awake()
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_ANDROID
        OVRManager.eyeFovPremultipliedAlphaModeEnabled = false;
#endif
        // hide error message for later, if Scene has no door or window
        _errorScreen.SetActive(false);

        // hide the environment so app starts in Passthrough
        _envRoot.gameObject.SetActive(false);

        _sceneManager.SceneModelLoadedSuccessfully += InitializeRoom;
    }

    void InitializeRoom()
    {
        OVRSceneAnchor[] sceneAnchors = FindObjectsOfType<OVRSceneAnchor>();
        if (sceneAnchors != null)
        {
            List<Transform> windows = new List<Transform>();
            List<SampleDoorDebris> doorScripts = new List<SampleDoorDebris>();
            float floorHeight = 0.0f;
            bool butterflyAdded = false;

            for (int i = 0; i < sceneAnchors.Length; i++)
            {
                OVRSceneAnchor instance = sceneAnchors[i];
                OVRSemanticClassification classification = instance.GetComponent<OVRSemanticClassification>();

                if (classification.Contains(OVRSceneManager.Classification.Floor))
                {
                    // move the world slightly below the ground floor, so the virtual floor doesn't Z-fight
                    if (_envRoot)
                    {
                        Vector3 envPos = _envRoot.transform.position;
                        float groundHeight = instance.transform.position.y - _groundDelta;
                        _envRoot.transform.position = new Vector3(envPos.x, groundHeight, envPos.z);
                        floorHeight = instance.transform.position.y;
                    }
                }
                else if (classification.Contains(OVRSceneManager.Classification.WindowFrame) ||
                    classification.Contains(OVRSceneManager.Classification.DoorFrame))
                {
                    windows.Add(instance.transform);

                    //SimpleResizer resizer = new SimpleResizer();
                    //SimpleResizable prefab = classification.Contains(OVRSceneManager.Classification.DoorFrame) ? _doorPrefab : _windowPrefab;
                    Vector3 dimensions = instance.transform.GetChild(0).localScale;

                    // cache all the door objects, to correctly place the floor fade object
                    if (instance.GetComponent<SampleDoorDebris>())
                    {
                        doorScripts.Add(instance.GetComponent<SampleDoorDebris>());
                    }

                    // spawn an optional butterfly
                    if (classification.Contains(OVRSceneManager.Classification.DoorFrame) && _butterflyPrefab && !butterflyAdded)
                    {
                        GameObject butterfly = Instantiate(_butterflyPrefab, instance.transform);
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
                    //    dimensions += prefab.GetComponent<ResizablePadding>().dimensionPadding;
                    //}
                    //resizer.CreateResizedObject(dimensions, sceneAnchors[i].gameObject, prefab);
                }
            }

            // adjust floor fades for each door
            foreach (SampleDoorDebris door in doorScripts)
            {
                door.AdjustFloorFade(floorHeight);
            }

            if (windows.Count == 0)
            {
                _errorScreen.SetActive(true);
            }
            else
            {
                _envRoot.gameObject.SetActive(true);
            }
        }
    }
}
