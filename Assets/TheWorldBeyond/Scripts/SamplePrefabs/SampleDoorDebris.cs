// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

public class SampleDoorDebris : MonoBehaviour
{
    bool anchorInitialized = false;
    public GameObject _debrisPrefab;

    // optional: if this exists, reposition it slightly above the ground
    public GameObject _floorFadeObject;

    void Update()
    {
        if (!anchorInitialized)
        {
            if (gameObject.GetComponent<OVRScenePlane>() && gameObject.GetComponent<OVRSemanticClassification>())
            {
                if (gameObject.GetComponent<OVRSemanticClassification>().Contains(OVRSceneManager.Classification.DoorFrame))
                {
                    SpawnDebris(gameObject.GetComponent<OVRScenePlane>().Dimensions);
                    anchorInitialized = true;
                }
            }
        }
    }

    void SpawnDebris(Vector2 doorDimensions)
    {
        if (_debrisPrefab == null)
        {
            return;
        }

        // get bottom corners of door
        Vector3 basePos = transform.position - Vector3.up * doorDimensions.y * 0.5f;
        // move it into room a bit, to minimize object intersection with door frame
        float insetAmount = 0.1f;
        basePos += transform.forward * insetAmount;
        Vector3 offset = transform.right * ((doorDimensions.x * 0.5f) - insetAmount);

        // for this example, only do 2 debris
        // modify this to create your own debris distribution
        for (int i = 0; i < 2; i++)
        {
            GameObject debris = Instantiate(_debrisPrefab);
            // quick remapping of iterator (0,1) to left/right side (-1, +1)
            debris.transform.position = basePos + (offset * (i - 0.5f) * 2);
            debris.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360.0f), 0);
            debris.transform.parent = this.transform;
        }
    }

    public void AdjustFloorFade(float floorHeight)
    {
        if (_floorFadeObject)
        {
            Vector3 pos = _floorFadeObject.transform.position;
            _floorFadeObject.transform.position = new Vector3(pos.x, floorHeight + 0.01f, pos.z);
        }
    }
}
