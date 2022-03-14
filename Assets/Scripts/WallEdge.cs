// Copyright(c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

public class WallEdge : MonoBehaviour
{
    [HideInInspector]
    public SanctuaryRoomObject _parentSurface;
    public ParticleSystem _edgePassthroughParticles;
    public ParticleSystem _edgeVirtualParticles;
    ParticleSystemRenderer _passthroughRenderer = null;
    ParticleSystemRenderer _virtualRenderer = null;
    [HideInInspector]
    public WallEdge _siblingEdge = null;

    // how much the edge particles emit per meter per second
    const int _edgeParticleRate = 50;

    public void AdjustParticleSystemRateAndSize(float prtWidth)
    {
        if (!_passthroughRenderer)
        {
            _passthroughRenderer = _edgePassthroughParticles.gameObject.GetComponent<ParticleSystemRenderer>();
        }
        if (!_virtualRenderer)
        {
            _virtualRenderer = _edgeVirtualParticles.gameObject.GetComponent<ParticleSystemRenderer>();
        }
        SetParams(_edgePassthroughParticles, prtWidth);
        SetParams(_edgeVirtualParticles, prtWidth);
    }

    void SetParams(ParticleSystem _renderer, float prtWidth)
    {
        var prtBox = _renderer.shape;
        prtBox.scale = new Vector3(prtWidth, prtBox.scale.y, prtBox.scale.z);
        var rate = _renderer.emission;
        rate.rateOverTime = _edgeParticleRate * prtWidth;
    }

    public void UpdateParticleMaterial(float EffectTimer, Vector3 impactPosition, float invertedMask)
    {
        if (_passthroughRenderer)
        {
            _passthroughRenderer.material.SetFloat("_EffectTimer", EffectTimer);
            _passthroughRenderer.material.SetVector("_EffectPosition", impactPosition);
            _passthroughRenderer.material.SetFloat("_InvertedMask", invertedMask);
        }
    }

    public void ShowEdge(bool doShow)
    {
        if (doShow)
        {
            _edgePassthroughParticles.Play();
            _edgeVirtualParticles.Play();
        }
        else
        {
            _edgePassthroughParticles.Stop();
            _edgeVirtualParticles.Stop();
        }
    }

    public void ClearEdgeParticles()
    {
        _edgePassthroughParticles.Clear();
        _edgeVirtualParticles.Clear();
    }
}
