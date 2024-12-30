// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

public class ListeningMaterialAnimation : MonoBehaviour
{
    public float ScrollSpeed = -0.05F;
    public Color Color = Color.red;
    public float Intensity = 0.7f;
    private float _intensity = 0.7f;
    private Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
        rend.material.color = Color;
    }
    void Update()
    {
        // It might be good to only do this when in listening state
        float offset = Time.time * ScrollSpeed;
        rend.material.SetFloat("_ScrollAmount", offset);

        Vector3 objFwd = transform.position - WorldBeyondManager.Instance._mainCamera.transform.position;
        objFwd.y = 0;
        _intensity = Mathf.Clamp(Intensity * objFwd.magnitude, 0.5f, 3);
        rend.material.SetFloat("_Intensity", _intensity);
    }
}
