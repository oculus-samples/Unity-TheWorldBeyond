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
