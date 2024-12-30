// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace TheWorldBeyond.SampleScenes
{
    public class SampleShooter : MonoBehaviour
    {
        public GameObject GunPrefab;
        public GameObject BulletPrefab;
        public Transform LeftHandAnchor;
        public Transform RightHandAnchor;
        private GameObject m_leftGun;
        private GameObject m_rightGun;

        private void Start()
        {
            m_leftGun = Instantiate(GunPrefab);
            m_leftGun.transform.SetParent(LeftHandAnchor, false);
            m_rightGun = Instantiate(GunPrefab);
            m_rightGun.transform.SetParent(RightHandAnchor, false);
        }

        // Update is called once per frame
        private void Update()
        {
            if (OVRInput.GetDown(OVRInput.RawButton.LIndexTrigger))
            {
                ShootBall(m_leftGun.transform.position, m_leftGun.transform.forward);
                PlayShootSound(m_leftGun);
            }
            if (OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger))
            {
                ShootBall(m_rightGun.transform.position, m_rightGun.transform.forward);
                PlayShootSound(m_rightGun);
            }
        }

        public void ShootBall(Vector3 ballPosition, Vector3 ballDirection)
        {
            var ballPos = ballPosition + ballDirection * 0.1f;
            var newBall = Instantiate(BulletPrefab, ballPos, Quaternion.identity);
            var rigidBody = newBall.GetComponent<Rigidbody>();
            if (rigidBody)
            {
                rigidBody.AddForce(ballDirection * 3.0f);
            }
        }

        private void PlayShootSound(GameObject gun)
        {
            if (gun.GetComponent<AudioSource>())
            {
                gun.GetComponent<AudioSource>().time = 0.0f;
                gun.GetComponent<AudioSource>().Play();
            }
        }
    }
}
