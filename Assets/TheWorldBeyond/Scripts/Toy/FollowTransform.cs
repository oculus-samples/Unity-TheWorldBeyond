// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace TheWorldBeyond.Toy
{
    public class FollowTransform : MonoBehaviour
    {
        public bool IsLateUpdate = false;
        public bool UnscaledTime = false;
        public bool FollowRotate = true;
        public bool FollowWithOffset = true;
        public bool FollowWithSpring = true;
        public bool OffsetsOnEnable = true;

        public bool Animated = false;

        // if no transform specified, current is used
        public Transform TheTransform;

        // if no follow transform specified, parent is used
        public Transform TheFollowTransform;
        private Transform m_followTrans;
        private Vector3 m_prevFollowPos;

        [Range(0f, 1f)]
        public float Weight = 1f;

        // Dynamics settings
        public float Stiffness = 0.04f;
        public float Mass = 0.5f;
        public float Damping = 1f;
        public float Gravity = 0.0001f;
        private float m_currentDelta = 0f;

        // noise
        //public float sNoiseAmp = 0;
        //public float sNoiseFreq = 0.5f;

        // Target and dynamic positions
        private Vector3 m_targetPos;
        private Quaternion m_targetRot;
        private Vector3 m_dynamicPos;
        private Quaternion m_dynamicRot;
        private Vector3 m_force;
        private Vector3 m_acc;
        private Vector3 m_vel;
        private Vector3 m_followTransPosOffset = Vector3.zero;
        private Quaternion m_followTransRotOffset = Quaternion.identity;

        private void Start()
        {
            if (!OffsetsOnEnable)
            {
                Init();
            }
        }

        private void OnEnable()
        {
            if (OffsetsOnEnable)
            {
                Init();
            }
        }

        private void Init()
        {
            // assign this transform if not assigned
            if (TheTransform == null)
                TheTransform = transform;

            // assign parent transform if not assigned
            if (!TheFollowTransform && TheTransform.parent != null)
            {
                TheFollowTransform = TheTransform.parent;
            }

            m_followTrans = TheFollowTransform;

            if (FollowWithOffset)
            {
                m_followTransPosOffset = m_followTrans.InverseTransformPoint(TheTransform.position);
                m_followTransRotOffset = Quaternion.FromToRotation(m_followTrans.forward, TheTransform.forward);
            }

            m_targetPos = m_followTrans.TransformPoint(m_followTransPosOffset);
            m_targetRot = m_followTrans.rotation * m_followTransRotOffset;

            m_dynamicPos = m_targetPos;
            m_dynamicRot = m_targetRot;

            m_vel = Vector3.zero;

            TheTransform.position = m_targetPos;
            TheTransform.rotation = m_targetRot;
        }

        private void Update()
        {
            if (!IsLateUpdate)
            {
                Tick();
            }
        }

        private void LateUpdate()
        {
            if (IsLateUpdate)
            {
                Tick();
            }
        }

        private void Tick()
        {
            if (m_followTrans == null)
                return;

            if (Animated)
            {
                m_followTransPosOffset = m_followTrans.InverseTransformPoint(TheTransform.position);
                m_followTransRotOffset = Quaternion.FromToRotation(m_followTrans.forward, TheTransform.forward);
            }

            m_currentDelta = Time.deltaTime * 90f;

            // clamp delta to avoid craziness
            m_currentDelta = Mathf.Clamp(m_currentDelta, 0.8f, 1.2f);

            m_targetPos = m_followTrans.TransformPoint(m_followTransPosOffset);

            if (FollowRotate)
                m_targetRot = m_followTrans.rotation * m_followTransRotOffset;

            // Calculate m_force, acceleration, and velocity per X, Y and Z
            m_force.x = (m_targetPos.x - m_dynamicPos.x) * Stiffness;
            m_acc.x = m_force.x / Mass;
            m_vel.x += m_acc.x * (1f - Damping);

            m_force.y = (m_targetPos.y - m_dynamicPos.y) * Stiffness;
            m_force.y -= Gravity / 10f; // Add some Gravity
            m_acc.y = m_force.y / Mass;
            m_vel.y += m_acc.y * (1f - Damping);

            m_force.z = (m_targetPos.z - m_dynamicPos.z) * Stiffness;
            m_acc.z = m_force.z / Mass;
            m_vel.z += m_acc.z * (1f - Damping);

            // Update dynamic postion
            m_dynamicPos += (m_vel + m_force) * m_currentDelta;

            if (FollowRotate)
                m_dynamicRot = Quaternion.Lerp(TheTransform.rotation, m_targetRot, Stiffness * 3f * m_currentDelta);

            // set transform
            if (FollowWithSpring)
            {
                TheTransform.position = Vector3.Lerp(TheTransform.position, m_dynamicPos, Weight);

                if (FollowRotate)
                    TheTransform.rotation = Quaternion.Lerp(TheTransform.rotation, m_dynamicRot, Weight);
            }
            else
            {
                TheTransform.position = Vector3.Lerp(TheTransform.position, m_targetPos, Weight);

                if (FollowRotate)
                    TheTransform.rotation = Quaternion.Lerp(TheTransform.rotation, m_targetRot, Weight);
            }
        }

    }
}
