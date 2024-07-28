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
using UnityEngine.AI;

public class SamplePetExperience : MonoBehaviour
{
    public OVRSceneManager _sceneManager;
    bool _roomReady = false;

    // build the nav mesh when Scene is detected
    public NavMeshSurface _ground;
    public NavMeshAgent _agent;

    // the point on the ground where your controller points
    public Transform _targetingIcon;
    public LayerMask _sceneLayer;

    void Awake()
    {
        _agent.SetDestination(Vector3.zero);
        _agent.updateRotation = false;

        _sceneManager.SceneModelLoadedSuccessfully += InitializeRoom;
    }

    void InitializeRoom()
    {
        _ground.BuildNavMesh();
        _roomReady = true;
    }

    void Update()
    {
        if (!_roomReady)
        {
            return;
        }

        RaycastHit hitInfo;
        Vector3 rayPos = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
        Vector3 rayFwd = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch) * Vector3.forward;
        if (Physics.Raycast(rayPos, rayFwd, out hitInfo, 1000.0f, _sceneLayer))
        {
            // if hitting a vertical surface, drop quad to the floor
            float iconHeight = Mathf.Abs(Vector3.Dot(Vector3.up, hitInfo.normal)) < 0.5f ? 0 : hitInfo.point.y;
            // offset quad a bit so it doesn't z-flicker
            _targetingIcon.position = new Vector3(hitInfo.point.x, iconHeight + 0.01f, hitInfo.point.z);
        }

        bool pressingButton = OVRInput.Get(OVRInput.RawButton.RIndexTrigger) || OVRInput.Get(OVRInput.RawButton.A);
        if (pressingButton)
        {
            _agent.SetDestination(_targetingIcon.position);
        }
        _targetingIcon.localScale = Vector3.one * (pressingButton ? 0.6f : 0.5f);
    }
}
