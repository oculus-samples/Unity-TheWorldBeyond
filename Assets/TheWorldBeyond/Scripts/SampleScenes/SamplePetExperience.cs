// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.AI;
#pragma warning disable CS0618 // Type or member is obsolete

namespace TheWorldBeyond.SampleScenes
{
    public class SamplePetExperience : MonoBehaviour
    {
        public OVRSceneManager SceneManager;
        private bool m_roomReady = false;

        // build the nav mesh when Scene is detected
        public NavMeshSurface Ground;
        public NavMeshAgent Agent;

        // the point on the ground where your controller points
        public Transform TargetingIcon;
        public LayerMask SceneLayer;

        private void Awake()
        {
            _ = Agent.SetDestination(Vector3.zero);
            Agent.updateRotation = false;

            SceneManager.SceneModelLoadedSuccessfully += InitializeRoom;
        }

        private void InitializeRoom()
        {
            Ground.BuildNavMesh();
            m_roomReady = true;
        }

        private void Update()
        {
            if (!m_roomReady)
            {
                return;
            }

            var rayPos = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
            var rayFwd = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch) * Vector3.forward;
            if (Physics.Raycast(rayPos, rayFwd, out var hitInfo, 1000.0f, SceneLayer))
            {
                // if hitting a vertical surface, drop quad to the floor
                var iconHeight = Mathf.Abs(Vector3.Dot(Vector3.up, hitInfo.normal)) < 0.5f ? 0 : hitInfo.point.y;
                // offset quad a bit so it doesn't z-flicker
                TargetingIcon.position = new Vector3(hitInfo.point.x, iconHeight + 0.01f, hitInfo.point.z);
            }

            var pressingButton = OVRInput.Get(OVRInput.RawButton.RIndexTrigger) || OVRInput.Get(OVRInput.RawButton.A);
            if (pressingButton)
            {
                _ = Agent.SetDestination(TargetingIcon.position);
            }
            TargetingIcon.localScale = Vector3.one * (pressingButton ? 0.6f : 0.5f);
        }
    }
}
