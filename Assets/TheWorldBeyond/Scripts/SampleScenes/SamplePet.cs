// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.AI;

namespace TheWorldBeyond.SampleScenes
{
    public class SamplePet : MonoBehaviour
    {
        private NavMeshAgent m_agent;
        public Animator Animator;
        public Transform PetRoot;
        private Vector3 m_lastPetPosition;
        private Vector3 m_faceDirection = Vector3.forward;

        // head is Z-left, X-down, and Y-forward
        public Transform HeadBone;
        private Quaternion m_targetRotation = Quaternion.identity;
        private Quaternion m_originalHeadRot = Quaternion.identity;
        // eyes are Z-forward
        public Transform LeftEye;
        public Transform RightEye;
        private Quaternion m_originalLeftEyeRot = Quaternion.identity;
        private Quaternion m_originalRightEyeRot = Quaternion.identity;
        private const float MAX_HEAD_ANGLE = 45.0f;
        private const float MAX_EYE_ANGLE = 30.0f;

        // m_pet looks at user by default, unless this transform is close
        public Transform NearbyTarget;

        private void Start()
        {
            m_agent = GetComponent<NavMeshAgent>();
            m_originalLeftEyeRot = LeftEye.localRotation;
            m_originalRightEyeRot = RightEye.localRotation;
            m_originalHeadRot = HeadBone.localRotation;
        }

        // because the look behavior overrides RootBone rotations, use IsLateUpdate() instead of Update()
        private void LateUpdate()
        {
            Animator.SetBool("Running", m_agent.velocity.magnitude > Mathf.Epsilon);
            Animator.SetFloat("Speed", Mathf.Clamp01(m_agent.velocity.magnitude * 0.25f + 0.5f));

            // face direction of travel
            var targetFaceDirection = PetRoot.forward;
            var currentVelocity = transform.position - m_lastPetPosition;
            var petNavigating = currentVelocity.magnitude > Mathf.Epsilon;
            if (petNavigating)
            {
                targetFaceDirection = currentVelocity.normalized;

                // update "look" rotation even when unused, for proper RootBone blending
                m_targetRotation = HeadBone.parent.rotation * m_originalHeadRot;
            }

            m_lastPetPosition = transform.position;
            m_faceDirection = Vector3.Slerp(m_faceDirection, targetFaceDirection, 0.1f);
            FacePosition(transform.position + m_faceDirection);

            // by default, look at the user/camera
            // otherwise, look at the target if it's close or being chased
            var lookPos = Camera.main.transform.position;
            var graphicNearby = Vector3.Distance(NearbyTarget.position, PetRoot.position) <= 1.0f;
            graphicNearby &= Vector3.Dot(m_faceDirection, (NearbyTarget.position - PetRoot.position).normalized) >= 0.0f;
            if (petNavigating || (!petNavigating && graphicNearby))
            {
                lookPos = NearbyTarget.position;
            }
            DoLookAtBehavior(lookPos);
        }

        /// <summary>
        /// Point m_pet's Z+ towards a world Position.
        /// </summary>
        public void FacePosition(Vector3 worldPosition)
        {
            worldPosition = new Vector3(worldPosition.x, transform.position.y, worldPosition.z);
            PetRoot.rotation = Quaternion.LookRotation(worldPosition - transform.position);
        }

        /// <summary>
        /// Have the m_pet try and look at a Position in the world. Bone angles clamp to prevent Exorcist-neck.
        /// </summary>
        private void DoLookAtBehavior(Vector3 lookPosition)
        {
            var headLook = HeadBone.transform.rotation * Vector3.up;
            var headUp = HeadBone.transform.rotation * Vector3.right;

            // the head RootBone is at a neck Position
            // so the illusion requires it to look at a point slightly below the camera
            var petHeadY = lookPosition - Vector3.up * 0.2f - HeadBone.position;

            // clamp angle so m_pet doesn't break its neck
            var currentAngle = Vector3.Angle(petHeadY, headLook);
            petHeadY = Vector3.Slerp(headLook, petHeadY, Mathf.Clamp01(MAX_HEAD_ANGLE / currentAngle)).normalized;

            var petHeadX = headUp;
            Vector3.OrthoNormalize(ref petHeadY, ref petHeadX);
            var petHeadZ = Vector3.Cross(petHeadX, petHeadY);
            var lookRot = Quaternion.LookRotation(petHeadZ, petHeadY);
            m_targetRotation = Quaternion.Lerp(m_targetRotation, lookRot, 0.05f);

            HeadBone.rotation = m_targetRotation;

            // make eyes look
            var leftLook = lookPosition - LeftEye.position;
            var rightLook = lookPosition - RightEye.position;
            var leftEyeFwd = LeftEye.parent.rotation * m_originalLeftEyeRot * Vector3.forward;
            var rightEyeFwd = RightEye.parent.rotation * m_originalRightEyeRot * Vector3.forward;
            var leftAngle = Vector3.Angle(leftEyeFwd, leftLook);
            var rightAngle = Vector3.Angle(rightEyeFwd, rightLook);
            leftLook = Vector3.Slerp(leftEyeFwd, leftLook, Mathf.Clamp01(MAX_EYE_ANGLE / leftAngle)).normalized;
            rightLook = Vector3.Slerp(rightEyeFwd, rightLook, Mathf.Clamp01(MAX_EYE_ANGLE / rightAngle)).normalized;

            LeftEye.rotation = Quaternion.LookRotation(leftLook);
            RightEye.rotation = Quaternion.LookRotation(rightLook);
        }
    }
}
