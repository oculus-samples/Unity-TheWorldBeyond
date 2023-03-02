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

// replicate the system dialog's behavior (gravity aligned, re-orient if out of view)
public class SampleDialog : MonoBehaviour
{
    Vector3 _currentFacingDirection = Vector3.forward;
    Vector3 _averageFacingDirection = Vector3.forward;

    void Update()
    {
        Transform cam = Camera.main.transform;
        Vector3 currentLook = new Vector3(cam.forward.x, 0.0f, cam.forward.z).normalized;
        if (Vector3.Dot(_currentFacingDirection, currentLook) < 0.5f)
        {
            _currentFacingDirection = currentLook;
        }

        _averageFacingDirection = Vector3.Slerp(_averageFacingDirection, _currentFacingDirection, 0.05f);
        transform.position = cam.position;
        transform.rotation = Quaternion.LookRotation(_averageFacingDirection, Vector3.up);
    }

    public void QuitApp()
    {
        Application.Quit();
    }
}
