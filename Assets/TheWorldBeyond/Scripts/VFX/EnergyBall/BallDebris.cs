// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

public class BallDebris : MonoBehaviour
{
    bool _dead = false;
    float _deathTimer = 0.5f;
    float _maxAbsorbStrength = 7.0f;
    public Rigidbody _rigidBody;

    void Update()
    {
        if (_dead)
        {
            // shrink into oblivion
            _deathTimer -= Time.deltaTime * 0.5f;
            if (_deathTimer <= 0.0f)
            {
                Destroy(this.gameObject);
            }
            else
            {
                transform.localScale = _deathTimer * Vector3.one;
            }
        }
    }

    public void Kill()
    {
        if (!_dead)
        {
            _deathTimer = transform.localScale.x;
            _dead = true;
        }
    }

    public void AddForce(Vector3 direction, float absorbScale)
    {
        _rigidBody.AddForce(direction * absorbScale * _maxAbsorbStrength, ForceMode.Force);
    }
}
