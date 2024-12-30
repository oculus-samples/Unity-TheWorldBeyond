// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace TheWorldBeyond.VFX
{
    public class BallDebris : MonoBehaviour
    {
        private bool m_dead = false;
        private float m_deathTimer = 0.5f;
        private float m_maxAbsorbStrength = 7.0f;
        public Rigidbody RigidBody;

        private void Update()
        {
            if (m_dead)
            {
                // shrink into oblivion
                m_deathTimer -= Time.deltaTime * 0.5f;
                if (m_deathTimer <= 0.0f)
                {
                    Destroy(gameObject);
                }
                else
                {
                    transform.localScale = m_deathTimer * Vector3.one;
                }
            }
        }

        public void Kill()
        {
            if (!m_dead)
            {
                m_deathTimer = transform.localScale.x;
                m_dead = true;
            }
        }

        public void AddForce(Vector3 direction, float absorbScale)
        {
            RigidBody.AddForce(direction * absorbScale * m_maxAbsorbStrength, ForceMode.Force);
        }
    }
}
