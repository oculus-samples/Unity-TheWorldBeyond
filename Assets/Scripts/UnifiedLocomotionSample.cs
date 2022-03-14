using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnifiedLocomotionSample : MonoBehaviour
{
    bool _foundRoom = false;
    public SceneEnvironment _sceneEnvironment;

    void Start()
    {
        StartCoroutine(DelayedRoomSearch());
    }

    IEnumerator DelayedRoomSearch()
    {
        yield return new WaitForSeconds(2);
        GetRoomFromScene();
    }

    void GetRoomFromScene()
    {
        if (_foundRoom)
        {
            return;
        }

        OVRSceneObject[] _sceneObjects = FindObjectsOfType<OVRSceneObject>();
        if (_sceneObjects.Length > 0)
        {
            _sceneEnvironment.Initialize(_sceneObjects);
            _foundRoom = true;
        }
    }
}
