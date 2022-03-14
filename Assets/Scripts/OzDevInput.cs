// Copyright(c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

public class OzDevInput : MonoBehaviour
{
    public float Speed = 10;

    void Start()
    {
        
    }

    void Update()
    {
        transform.position += 
            new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")) *
            Speed *
            Time.deltaTime;
    }
}
