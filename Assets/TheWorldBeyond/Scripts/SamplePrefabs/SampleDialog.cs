// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

// replicate the system dialog's behavior (gravity aligned, re-orient if out of view)
public class SampleDialog : MonoBehaviour
{
    Vector3 _currentFacingDirection = Vector3.forward;
    Vector3 _averageFacingDirection = Vector3.forward;

    void Update()
    {
        Transform cam = Camera.main.transform;
        Vector3 currentLook = new Vector3(cam.forward.x, 0.0f, cam.forward.z).normalized;
        if (Vector3.Dot(_currentFacingDirection, currentLook) < 0.5f)
        {
            _currentFacingDirection = currentLook;
        }

        _averageFacingDirection = Vector3.Slerp(_averageFacingDirection, _currentFacingDirection, 0.05f);
        transform.position = cam.position;
        transform.rotation = Quaternion.LookRotation(_averageFacingDirection, Vector3.up);
    }

    public void QuitApp()
    {
        Application.Quit();
    }
}
