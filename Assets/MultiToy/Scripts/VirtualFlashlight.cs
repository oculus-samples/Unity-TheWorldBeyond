// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;

public class VirtualFlashlight : WorldBeyondToy
{
    public GameObject _flashlight;
    public GameObject _lightVolume;
    List<MeshRenderer> _lightQuads = new List<MeshRenderer>();
    public AnimationCurve _flickerStrength;
    public MeshRenderer _magicEffectCone;
    public Light _spotlight;
    float _spotlightBaseIntensity = 1.0f;
    float _effectTimer = 0.0f;
    float _effectAccel = 0.0f;

    [HideInInspector]
    public bool _absorbingBall = false;
    public AudioSource ballAbsorbAudioSource;

    public Transform _lightBone;

    void Start()
    {
        GameObject refQuad = _lightVolume.transform.GetChild(0).gameObject;
        int sliceCount = 4;
        float basePos = 0.3f;
        float baseScale = 0.4f;
        for (int i = 0; i < sliceCount; i++)
        {
            GameObject newQuad = refQuad;
            if (i > 0)
            {
                newQuad = Instantiate(refQuad, _lightVolume.transform);
            }
            float normPos = i / (float)(sliceCount - 1);
            float dist = Mathf.Pow(normPos, 1.5f);
            newQuad.transform.localPosition = Vector3.up * (basePos + dist) * 0.6f;
            newQuad.transform.localScale = Vector3.one * (baseScale + dist * 1.5f);
            MeshRenderer nqm = newQuad.GetComponent<MeshRenderer>();

            _lightQuads.Add(nqm);
        }

        _flashlight.SetActive(false);
        if (_spotlight) _spotlightBaseIntensity = _spotlight.intensity;
    }

    void LateUpdate()
    {
        // ensure all the light volume quads are camera-facing
        for (int i = 0; i < _lightQuads.Count; i++)
        {
            Quaternion quadLook = Quaternion.LookRotation(_lightQuads[i].transform.position - WorldBeyondManager.Instance._mainCamera.transform.position);
            _lightQuads[i].transform.rotation = quadLook;
        }

        _effectAccel += _absorbingBall ? -0.1f : 0.05f;
        // when using the mesh, it's attached to the spinning piece, so adjust a bit
        float rotationOffset = WorldBeyondManager.Instance._usingHands ? 0.0f : -0.03f;
        _effectAccel = Mathf.Clamp(_effectAccel, -0.2f, 0.05f) + rotationOffset;
        _effectTimer += Time.deltaTime * _effectAccel;
        _magicEffectCone.material.SetFloat("_ScrollAmount", _effectTimer);

        if (_isActivated && _absorbingBall)
        {
            WorldBeyondManager.Instance.AffectDebris(transform.position, false);
        }
    }

    public void SetLightStrength(float strength)
    {
        MultiToy.Instance._flashlightLoop_1.SetVolume(strength);
        if (WorldBeyondManager.Instance._usingHands)
        {
            MultiToy.Instance._flashlightAbsorbLoop_1.SetVolume(0f);
        }

        _spotlight.intensity = _spotlightBaseIntensity * strength;
        _magicEffectCone.material.SetFloat("_Intensity", strength);

        for (int i = 0; i < _lightQuads.Count; i++)
        {
            _lightQuads[i].material.SetFloat("_Intensity", strength);
        }
    }

    public override void Activate()
    {
        base.Activate();
        _flashlight.SetActive(true);
        MultiToy.Instance._flashlightLoop_1.Play();
    }

    public override void Deactivate()
    {
        base.Deactivate();
        _flashlight.SetActive(false);
        MultiToy.Instance.StopLoopingSound();
        _absorbingBall = false;
        MultiToy.Instance._flashlightAbsorbLoop_1.Stop();
        if (!WorldBeyondManager.Instance._usingHands)
        {
            OVRInput.SetControllerVibration(1, 0, WorldBeyondManager.Instance._gameController);
        }
    }

    public override void ActionDown()
    {
        _absorbingBall = true;
        MultiToy.Instance._flashlightAbsorbLoop_1.Play();
    }

    public override void Action()
    {
        if (!WorldBeyondManager.Instance._usingHands)
        {
            OVRInput.SetControllerVibration(1, 0.5f, WorldBeyondManager.Instance._gameController);
        }
    }

    public override void ActionUp()
    {
        _absorbingBall = false;
        MultiToy.Instance._flashlightAbsorbLoop_1.Stop();
        if (!WorldBeyondManager.Instance._usingHands)
        {
            OVRInput.SetControllerVibration(1, 0, WorldBeyondManager.Instance._gameController);
        }
    }
}
