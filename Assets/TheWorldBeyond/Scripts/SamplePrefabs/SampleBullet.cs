// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace TheWorldBeyond.SamplePrefabs
{
    public class SampleBullet : MonoBehaviour
    {
        public GameObject DebrisPrefab;
        private Rigidbody m_rigidBody;
        private AudioSource m_bounceSound;

        private void Start()
        {
            m_rigidBody = GetComponent<Rigidbody>();
            m_bounceSound = GetComponent<AudioSource>();
        }

        public void OnCollisionEnter(Collision collision)
        {
            // make sure this was an intentional shot, as opposed to a light graze
            var impactNormal = collision.GetContact(0).normal;

            SpawnImpactDebris(collision.GetContact(0).point + impactNormal * 0.005f, Quaternion.LookRotation(-impactNormal));

            var impactDot = Mathf.Abs(Vector3.Dot(impactNormal, m_rigidBody.linearVelocity.normalized));
            if (impactDot > 0.7f)
            {
                m_bounceSound.time = 0.0f;
                m_bounceSound.Play();
            }
        }

        /// <summary>
        /// When colliding with something, spawn little impact gems that play with the world.
        /// </summary>
        private void SpawnImpactDebris(Vector3 position, Quaternion impactSpace)
        {
            // spawn debris in a cone formation
            var debrisCount = Random.Range(2, 4);
            for (var i = 0; i < debrisCount; i++)
            {
                var angle = Mathf.Deg2Rad * Random.Range(35.0f, 55.0f);
                var localEjectDirection = new Vector3(Mathf.Cos(angle), 0, -Mathf.Sin(angle));
                localEjectDirection = Quaternion.Euler(0, 0, Random.Range(0.0f, 360.0f)) * localEjectDirection;
                var localEjectPosition = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), 0).normalized * 0.03f;
                localEjectPosition = impactSpace * localEjectPosition;
                var debrisInstance = Instantiate(DebrisPrefab, position + localEjectPosition, Quaternion.identity);
                debrisInstance.transform.localScale = Random.Range(0.2f, 0.5f) * Vector3.one;
                debrisInstance.GetComponent<Rigidbody>().AddForce(impactSpace * localEjectDirection * Random.Range(0.5f, 1.5f), ForceMode.Impulse);
                var randomTorque = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f));
                debrisInstance.GetComponent<Rigidbody>().AddTorque(randomTorque * Random.Range(1.0f, 2.0f), ForceMode.Impulse);
                var selfDestruct = debrisInstance.AddComponent<SelfDestruct>();
                selfDestruct.SelfDestructionTimer = 3.0f;
            }
        }
    }
}
