// Copyright 2014-Present Oculus VR, LLC. Proprietary and Confidential.

using System.Collections.Generic;
using UnityEngine;

namespace Modules.Teleporter {
  public class ArcTargeter : Targeter {
    private readonly Vector3 _g = new Vector3(0, -9.81f, 0);

    [SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private bool _enableLineRenderer = true;
    [SerializeField] private float _speed = 20;
    [SerializeField] private Color[] _validColors = new Color[2];
    [SerializeField] private Color[] _invalidColors = new Color[2];

    private List<Vector3> _trajectoryPositions = new List<Vector3>();
    private Transform leftAnchor;
    private Transform rightAnchor;

    protected virtual void Awake() {
      Debug.Assert(_lineRenderer != null);
    }

    public override bool ValidTarget {
      get {
        if (!DidHit) return false;
        if (HitTeleportObject != null) {
          return HitTeleportObject.IsInteractable && !HitTeleportObject.IsOccupied;
        }
        return Vector3.Dot(_hitInfo.normal, Vector3.up) >= Mathf.Cos(SlopeToleranceRadians);
      }
    }

    public override void Init(Hand targetingHand) {
      leftAnchor = OVRManager.instance.GetComponentInChildren<OVRCameraRig>().leftControllerAnchor;
      rightAnchor = OVRManager.instance.GetComponentInChildren<OVRCameraRig>().rightControllerAnchor;
      Origin = targetingHand == Hand.Left ? leftAnchor : rightAnchor;
      _lineRenderer.enabled = _enableLineRenderer;
      _initialized = true;
    }

    public override void Tick(Quaternion inputRotation) {
      if (!_initialized) {
        Debug.LogError("Trying to tick the targeter without initializing.");
        return;
      }

      _trajectoryPositions.Clear();

      var timeStep = 0.01f; // 2 per second
      var maxSteps = 500;

      var velocity = _speed * Origin.forward;
      var pos = Origin.position + velocity * timeStep;

      HitTeleportObject = null;

      for (var i = 0; i < maxSteps; i++) {
        _trajectoryPositions.Add(pos);
        velocity += _g * timeStep;
        var frameMove = velocity * timeStep;

        DidHit = Physics.Raycast(pos, frameMove, out _hitInfo, frameMove.magnitude);
        if (DidHit) {
          HitTeleportObject = _hitInfo.collider.GetComponentInParent<TeleportationTarget>();
          if (HitTeleportObject != null) {
            HitTeleportObject.OnTeleportTargeterHit(_hitInfo.point, inputRotation * Vector3.forward);
          }

          break;
        }

        pos += frameMove;
        _trajectoryPositions.Add(pos);
      }

      if (!_enableLineRenderer) return;

      _lineRenderer.positionCount = _trajectoryPositions.Count;
      _lineRenderer.SetPositions(_trajectoryPositions.ToArray());

      _lineRenderer.startColor = ValidTarget ? _validColors[0] : _invalidColors[0];
      _lineRenderer.endColor = ValidTarget ? _validColors[1] : _invalidColors[1];
    }

    public override void Kill() {
      _lineRenderer.enabled = false;
      _initialized = false;
    }

    public override void Clean() {
      _trajectoryPositions.Clear();
      base.Clean();
    }
  }
}
