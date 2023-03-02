/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections.Generic;
using UnityEngine;

public class WorldBeyondRoomObject : MonoBehaviour
{
    public MeshRenderer _passthroughMesh;
    Material _defaultMaterial;
    public Material _darkRoomMaterial;
    public int _surfaceID = 0;
    public Vector3 _dimensions = Vector3.one;

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

    private void Start()
    {
        _defaultMaterial = _passthroughMesh.material;
    }

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
        if (_passthroughMesh)
        {
            _passthroughMesh.material.SetFloat("_EffectTimer", _effectTimer);
            _passthroughMesh.material.SetVector("_EffectPosition", _impactPosition);
            _passthroughMesh.material.SetFloat("_InvertedMask", _passthroughWallActive ? 1.0f : 0.0f);
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

    /// <summary>
    /// The toggle rate of the animation effect needs to be limited for it to work properly.
    /// </summary>
    public bool CanBeToggled()
    {
        return !_animating;
    }

    /// <summary>
    /// Trigger the particles and shader effect on the wall material, as well as the start position for it.
    /// </summary>
    public bool ToggleWall(Vector3 hitPoint)
    {
        _impactPosition = hitPoint;
        _passthroughWallActive = !_passthroughWallActive;
        _effectTimer = 0.0f;
        _animating = true;
        return _passthroughWallActive;
    }

    /// <summary>
    /// "Reset" the wall to full Passthrough.
    /// </summary>
    public void ForcePassthroughMaterial()
    {
        _passthroughWallActive = true;

        if (_passthroughMesh)
        {
            _passthroughMesh.material.SetFloat("_EffectTimer", 0.0f);
            _passthroughMesh.material.SetVector("_EffectPosition", Vector3.up * 1000);
            _passthroughMesh.material.SetFloat("_InvertedMask", 0.0f);
        }

        foreach (WallEdge edge in wallEdges)
        {
            edge.UpdateParticleMaterial(0.0f, Vector3.up * 1000, 0.0f);
        }
    }

    /// <summary>
    /// During the intro sequence when the MultiToy appears, the room is a different material
    /// </summary>
    public void ShowDarkRoomMaterial(bool showDarkRoom)
    {
        _passthroughMesh.material = showDarkRoom ? _darkRoomMaterial : _defaultMaterial;
    }
}
