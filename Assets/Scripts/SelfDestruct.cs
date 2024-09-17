// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

public class SelfDestruct : MonoBehaviour
{
    public float _selfDestructionTimer = 5.0f;

    void Update()
    {
        _selfDestructionTimer -= Time.deltaTime;
        if (_selfDestructionTimer <= 0.0f)
        {
            Destroy(this.gameObject);
        }
    }
}
