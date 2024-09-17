// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

public class LightBeam : MonoBehaviour
{
    public Light _beamLight;
    public Transform _beamCore;
    public Transform _beamBase;
    public ParticleSystem _beamParticles;

    // player has looked at hole, so play animation
    bool _observed = false;
    float _viewCone = 0.95f;
    float _observedTimer = 0.0f;
    const float _animTime = 6.0f;
    Vector3 _floatingToyPosition = Vector3.up;
    Vector3 _hiddenToyPosition = Vector3.zero;

    Vector3 _beamCoreStartScale = Vector3.one;
    Vector3 _beamBaseStartScale = Vector3.one;

    [Header("Audio")]
    public SoundEntry _beamIntro;
    public SoundEntry _beamLoop;
    public SoundEntry _beamOutro;
    public float _beamLoopVolume_Small = 0.3f;
    public float _beamLoopPitch_Small = 2f;

    private void Update()
    {
        if (_observed)
        {
            _observedTimer += Time.deltaTime;
            float normTime = Mathf.Clamp01(_observedTimer / _animTime);

            Vector3 finalCoreScale = new Vector3(1.0f, VirtualRoom.Instance.GetCeilingHeight(), 1.0f);
            Vector3 finalBaseScale = Vector3.one;

            // aperture opens just in the beginning
            float beamTimer = Mathf.Cos(Mathf.Clamp01(normTime * 5) * Mathf.PI) * 0.5f + 0.5f;
            beamTimer = 1 - beamTimer;
            _beamCore.localScale = Vector3.Lerp(_beamCoreStartScale, finalCoreScale, beamTimer);
            Vector3 baseScale = Vector3.Lerp(_beamBaseStartScale, finalBaseScale, beamTimer);
            _beamBase.localScale = baseScale;

            // toy elevates as aperture finishes opening
            float toyTimer = Mathf.Cos(Mathf.Clamp01((normTime - 0.25f) / 0.75f) * Mathf.PI) * 0.5f + 0.5f;
            toyTimer = 1 - toyTimer;
            Vector3 toyPosition = Vector3.Lerp(_hiddenToyPosition, _floatingToyPosition, toyTimer);
            MultiToy.Instance.gameObject.SetActive(toyTimer > 0.01f);
            float bobbleTimer = Mathf.Clamp01((normTime - 0.9f) / 0.1f);
            MultiToy.Instance.transform.position = toyPosition + (Mathf.Sin(Time.time * 2) * Vector3.up * 0.05f) * bobbleTimer;
            MultiToy.Instance.transform.Rotate(Vector3.up * 0.5f, Space.World);

            // apply "lit room" effect to Scene objects
            VirtualRoom.Instance.SetEffectPosition(MultiToy.Instance.transform.position, toyTimer);
        }
        else
        {
            Vector3 lookAt = (_beamBase.position - WorldBeyondManager.Instance._mainCamera.transform.position).normalized;
            if (Vector3.Dot(lookAt, WorldBeyondManager.Instance._mainCamera.transform.forward) >= _viewCone)
            {
                _observed = true;
                _beamParticles.Play();
                _beamLoop.ResetPitch();
                _beamLoop.ResetVolume();
                _beamIntro.Play();
            }
            // enlarge the view cone requirement over time, in case player isn't looking directly at beam
            _viewCone -= Time.deltaTime * 0.05f;
            _viewCone = Mathf.Clamp01(_viewCone);
        }
    }

    /// <summary>
    /// Position the light beam, prepare it for opening when the player looks at it.
    /// </summary>
    public void Prepare(Vector3 toyPos)
    {
        _floatingToyPosition = toyPos;

        transform.position = new Vector3(toyPos.x, WorldBeyondManager.Instance.GetFloorHeight(), toyPos.z);
        _hiddenToyPosition = transform.position - Vector3.up * 0.3f;

        _beamLight.gameObject.SetActive(true);
        _observed = false;
        _observedTimer = 0.0f;
        _viewCone = 0.95f;

        float baseDiameter = 0.1f;
        _beamCoreStartScale = new Vector3(baseDiameter, baseDiameter, baseDiameter);
        _beamCore.localScale = _beamCoreStartScale;
        _beamCore.transform.localPosition = Vector3.zero;

        _beamBaseStartScale = new Vector3(baseDiameter, 1.0f, baseDiameter);
        _beamBase.localScale = _beamBaseStartScale;

        MultiToy.Instance.transform.position = _hiddenToyPosition;
        MultiToy.Instance.transform.rotation = Quaternion.identity;
        MultiToy.Instance.gameObject.SetActive(false);

        _beamLoop.Play();
        _beamLoop.SetVolume(_beamLoopVolume_Small);
        _beamLoop.SetPitch(2f);
    }

    public void CloseBeam()
    {
        this.gameObject.SetActive(false);
    }
}
