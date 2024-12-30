// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

public class SampleShooter : MonoBehaviour
{
    public GameObject _gunPrefab;
    public GameObject _bulletPrefab;
    public Transform _leftHandAnchor;
    public Transform _rightHandAnchor;

    GameObject _leftGun;
    GameObject _rightGun;

    void Start()
    {
        _leftGun = Instantiate(_gunPrefab);
        _leftGun.transform.SetParent(_leftHandAnchor, false);
        _rightGun = Instantiate(_gunPrefab);
        _rightGun.transform.SetParent(_rightHandAnchor, false);
    }

    // Update is called once per frame
    void Update()
    {
        if (OVRInput.GetDown(OVRInput.RawButton.LIndexTrigger))
        {
            ShootBall(_leftGun.transform.position, _leftGun.transform.forward);
            PlayShootSound(_leftGun);
        }
        if (OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger))
        {
            ShootBall(_rightGun.transform.position, _rightGun.transform.forward);
            PlayShootSound(_rightGun);
        }
    }

    public void ShootBall(Vector3 ballPosition, Vector3 ballDirection)
    {
        Vector3 ballPos = ballPosition + ballDirection * 0.1f;
        GameObject newBall = Instantiate(_bulletPrefab, ballPos, Quaternion.identity);
        Rigidbody _rigidBody = newBall.GetComponent<Rigidbody>();
        if (_rigidBody)
        {
            _rigidBody.AddForce(ballDirection * 3.0f);
        }
    }

    void PlayShootSound(GameObject gun)
    {
        if (gun.GetComponent<AudioSource>())
        {
            gun.GetComponent<AudioSource>().time = 0.0f;
            gun.GetComponent<AudioSource>().Play();
        }
    }
}
