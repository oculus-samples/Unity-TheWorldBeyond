// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using UnityEngine;

public class RoomFramer : WorldBeyondToy
{
    static public RoomFramer Instance = null;

    WorldBeyondRoomObject _hoveredSurface = null;
    Vector3 _hoveredPoint = Vector3.zero;
    Vector3 _hoveredNormal = Vector3.up;
    public GameObject _sparkPrefab;
    public GameObject _hoverBorderPrefab;
    public GameObject _wallOpenEffect;
    public GameObject _wallCloseEffect;
    LineRenderer _pointerLine;
    [HideInInspector]
    public ParticleSystem pointerSparks;
    ParticleSystemRenderer _sparksRenderer = null;

    public Color _wallHitColor;
    public Color _furnishingHitColor;

    enum HoveredObject
    {
        None,
        Wall,
        Floor,
        Furnishing
    };

    public override void Initialize()
    {
        if (!Instance)
        {
            Instance = this;
        }

        GameObject pointerObj = Instantiate(_hoverBorderPrefab);
        _pointerLine = pointerObj.GetComponent<LineRenderer>();
        _pointerLine.gameObject.SetActive(false);

        GameObject sparkObj = Instantiate(_sparkPrefab);
        pointerSparks = sparkObj.GetComponent<ParticleSystem>();
        _sparksRenderer = sparkObj.GetComponent<ParticleSystemRenderer>();
        MultiToy.Instance.wallToyTarget = sparkObj.transform;
    }

    private void Update()
    {
        if (!_isActivated)
        {
            return;
        }

        // highlight selected wall
        WorldBeyondRoomObject lastSurface = _hoveredSurface;
        HoveredObject hoveringWall = CheckForWall();
        SetBeamColor(hoveringWall);

        // handle any UI
        MultiToy.Instance.PointingAtWall();

        _pointerLine.positionCount = 2;
        _pointerLine.SetPosition(0, MultiToy.Instance.GetMuzzlePosition());
        _pointerLine.SetPosition(1, _hoveredPoint);

        pointerSparks.transform.position = _hoveredPoint;
        pointerSparks.transform.rotation = Quaternion.Lerp(pointerSparks.transform.rotation, Quaternion.LookRotation(-_hoveredNormal), 0.3f);

        WorldBeyondManager.Instance.AffectDebris(_hoveredPoint, true);
    }

    public override void ActionUp()
    {
        if (_hoveredSurface != null)
        {
            WorldBeyondRoomObject clickedWall = _hoveredSurface.GetComponent<WorldBeyondRoomObject>();
            bool surfacePassthroughActive = clickedWall.ToggleWall(_hoveredPoint);
            int surfaceId = _hoveredSurface._surfaceID;
            VirtualRoom.Instance.CloseWall(surfaceId, surfacePassthroughActive);
            AudioClip wallSound = surfacePassthroughActive ? MultiToy.Instance._wallToyWallClose : MultiToy.Instance._wallToyWallOpen;
            AudioManager.Instance.PlayAudio(wallSound, clickedWall.transform.position);

            if (!surfacePassthroughActive)
            {
                WorldBeyondManager.Instance.OpenedWall(surfaceId);
                WorldBeyondTutorial.Instance.HideMessage(WorldBeyondTutorial.TutorialMessage.ShootWall);
            }

            // recalculate the outdoor audio position
            Vector3 audioPos = Vector3.up;
            bool audioOn = VirtualRoom.Instance.GetOutdoorAudioPosition(ref audioPos);
            WorldBeyondEnvironment.Instance.SetOutdoorAudioParams(audioPos, audioOn);

            StartCoroutine(ExpandWallRing(surfacePassthroughActive));
        }
    }

    HoveredObject CheckForWall()
    {
        // highlight selected wall
        HoveredObject hoveringWall = HoveredObject.None;
        Vector3 controllerPos = Vector3.zero;
        Quaternion controllerRot = Quaternion.identity;
        WorldBeyondManager.Instance.GetDominantHand(ref controllerPos, ref controllerRot);

        // modify the beam direction, for hands
        if (WorldBeyondManager.Instance._usingHands)
        {
            controllerRot *= Quaternion.Euler(-60, 0, 0);
        }

        LayerMask acceptableLayers = LayerMask.GetMask("RoomBox", "Furniture");
        RaycastHit[] roomboxHit = Physics.RaycastAll(controllerPos, controllerRot * Vector3.forward, 1000.0f, acceptableLayers);
        float closestHit = 100.0f;
        Vector3 targetPoint = _hoveredPoint;
        foreach (RaycastHit hit in roomboxHit)
        {
            GameObject hitObj = hit.collider.gameObject;
            float thisHit = Vector3.Distance(hit.point, controllerPos);
            if (thisHit < closestHit)
            {
                closestHit = thisHit;
                targetPoint = hit.point + hit.normal * 0.02f;
                _hoveredNormal = hit.normal;
                WorldBeyondRoomObject rbs = hitObj.GetComponent<WorldBeyondRoomObject>();
                if (rbs)
                {
                    if (rbs._isFurniture)
                    {
                        hoveringWall = HoveredObject.Furnishing;
                    }
                    else if (VirtualRoom.Instance.IsFloor(rbs._surfaceID))
                    {
                        hoveringWall = HoveredObject.Floor;
                    }
                    else
                    {
                        if (rbs.CanBeToggled())
                        {
                            _hoveredSurface = rbs;
                            hoveringWall = HoveredObject.Wall;
                        }
                        else
                        {
                            hoveringWall = HoveredObject.None;
                        }
                    }
                }
            }
        }

        _hoveredPoint = Vector3.Lerp(_hoveredPoint, targetPoint, 0.1f);
        if (hoveringWall != HoveredObject.Wall)
        {
            _hoveredSurface = null;
        }

        return hoveringWall;
    }

    public override void Activate()
    {
        base.Activate();
        _hoveredSurface = null;
        _hoveredPoint = transform.position;
        HoveredObject surf = CheckForWall();
        if (surf != HoveredObject.None)
        {
            _pointerLine.gameObject.SetActive(true);
            pointerSparks.transform.position = _hoveredPoint;
            pointerSparks.transform.rotation = Quaternion.LookRotation(-_hoveredNormal);
            MultiToy.Instance._wallToyLoop_1.Play();
            SetBeamColor(surf);
        }
        pointerSparks.Play();
    }

    void SetBeamColor(HoveredObject surf)
    {
        Color beamColor = surf == HoveredObject.Wall ? _wallHitColor : _furnishingHitColor;
        _pointerLine.sharedMaterial.SetColor("_Color", beamColor);
        _sparksRenderer.sharedMaterial.SetColor("_Color", beamColor);
    }

    public override void Deactivate()
    {
        base.Deactivate();
        _pointerLine.gameObject.SetActive(false);
        pointerSparks.Stop();
        pointerSparks.Clear();
        MultiToy.Instance._wallToyLoop_1.Stop();
    }

    public bool IsHighlightingWall()
    {
        if (_pointerLine)
        {
            return (_pointerLine.gameObject.activeSelf && _hoveredSurface != null);
        }
        return false;
    }

    IEnumerator ExpandWallRing(bool ptActive)
    {
        // caution: the timing and numbers here should match the material effect in PassthroughWall.shader
        Quaternion ringRot = Quaternion.LookRotation(_hoveredNormal);
        GameObject effectObj = ptActive ? Instantiate(_wallCloseEffect, _hoveredPoint, ringRot) : Instantiate(_wallOpenEffect, _hoveredPoint, ringRot);
        ParticleSystem effectPrt = effectObj?.GetComponent<ParticleSystem>();
        float timer = 0.0f;
        float effectTime = 1.0f;
        while (timer <= effectTime)
        {
            timer += Time.deltaTime;
            if (effectPrt)
            {
                var shape = effectPrt.shape;
                // it takes 1 second to reach 10m radius
                shape.radius = timer * 5;

                int maxParticleRate = 150;
                var rate = effectPrt.emission;
                float pingpong = Mathf.Abs(timer - 0.5f) * 2;
                rate.rateOverTime = maxParticleRate * (1 - pingpong) * 100;
            }
            yield return null;
        }
    }
}
