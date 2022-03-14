using System.Collections.Generic;
using UnityEngine;

public class SanctuaryRoomObject : MonoBehaviour
{
    public MeshRenderer _passthroughWall;
    public MeshRenderer _guardianWall;
    public int _surfaceID = 0;

    [HideInInspector]
    public bool _isWall = false;
    [HideInInspector]
    public bool _isFurniture = false;

    [Header("Wall Fading")]
    bool _animating = false;
    [HideInInspector]
    public float _effectTimer = 0.0f;
    const float _effectTime = 1.0f;
    public bool _passthroughWallActive = true;
    public Vector3 _impactPosition = new Vector3(0, 1000, 0);
    public List<WallEdge> wallEdges = new List<WallEdge>();
    public List<GameObject> wallDebris = new List<GameObject>();

    private void Update()
    {
        if (!_animating)
        {
            return;
        }

        _effectTimer += Time.deltaTime;
        if (_effectTimer >= _effectTime)
        {
            _animating = false;
        }
        _effectTimer = Mathf.Clamp01(_effectTimer);
        if (_passthroughWall)
        {
            _passthroughWall.material.SetFloat("_EffectTimer", _effectTimer);
            _passthroughWall.material.SetVector("_EffectPosition", _impactPosition);
            _passthroughWall.material.SetFloat("_InvertedMask", _passthroughWallActive ? 1.0f : 0.0f);
        }
        foreach (WallEdge edge in wallEdges)
        {
            edge.UpdateParticleMaterial(_effectTimer, _impactPosition, _passthroughWallActive ? 1.0f : 0.0f);
        }

        float smoothTimer = Mathf.Cos(Mathf.PI * _effectTimer/_effectTime) * 0.5f + 0.5f;
        foreach (GameObject obj in wallDebris)
        {
            obj.transform.localScale = Vector3.one * (_passthroughWallActive ? smoothTimer : (1.0f - smoothTimer));
        }
    }

    public bool CanBeToggled()
    {
        return !_animating;
    }

    public bool ToggleWall(Vector3 hitPoint)
    {
        _impactPosition = hitPoint;
        _passthroughWallActive = !_passthroughWallActive;
        _effectTimer = 0.0f;
        _animating = true;
        return _passthroughWallActive;
    }

    public void ForcePassthroughMaterial()
    {
        _passthroughWallActive = true;

        if (_passthroughWall)
        {
            _passthroughWall.material.SetFloat("_EffectTimer", 0.0f);
            _passthroughWall.material.SetVector("_EffectPosition", Vector3.up * 1000);
            _passthroughWall.material.SetFloat("_InvertedMask", 0.0f);
        }

        foreach (WallEdge edge in wallEdges)
        {
            edge.UpdateParticleMaterial(0.0f, Vector3.up * 1000, 0.0f);
        }
    }
}
