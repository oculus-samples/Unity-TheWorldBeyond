// Copyright 2014-Present Oculus VR, LLC. Proprietary and Confidential.

using UnityEngine;

namespace Modules.Teleporter {
  public abstract class Targeter : MonoBehaviour {
    protected const float SlopeToleranceRadians = Mathf.Deg2Rad * 45f;

    protected bool _initialized = false;

    protected RaycastHit _hitInfo;

    public RaycastHit HitInfo {
      get { return _hitInfo; }
    }

    public TeleportationTarget HitTeleportObject { get; protected set; }

    public bool DidHit { get; protected set; }

    public abstract bool ValidTarget { get; }

    public Vector3 TargetPosition {
      get {
        if (!DidHit) return Vector3.zero;
        return GetHitGeometryTargetPosition();
      }
    }

    public Transform Origin { get; protected set; }

    protected virtual void OnEnable() { }

    protected virtual void OnDisable() {
      Clean();
    }

    public abstract void Init(Hand targetingHand);

    /// <summary>
    /// Custom Targeting Logic to be implemented by children. Updates DitHit, _hitInfo, and HitTeleportObject.
    /// </summary>
    public abstract void Tick(Quaternion inputRotation);
    public abstract void Kill();

    public virtual void Clean() {
      _hitInfo = default(RaycastHit);
      HitTeleportObject = null;
      DidHit = false;
      Origin = null;
      _initialized = false;
    }

    protected virtual Vector3 GetHitGeometryTargetPosition() {
      if (!DidHit) return Vector3.zero;

      // We hit a regular surface with no associated "Teleportation Target" component.
      // TODO: should the target position be raised by (0, 0.05, 0) like it was in GanymedeEyeTargeter?
      if (Vector3.Dot(_hitInfo.normal, Vector3.up) < Mathf.Cos(SlopeToleranceRadians)) {
        // Hit surface is at a slope greater than 45deg.
        // In this case, we'll snap the target to the top of the surface's collider.
        return new Vector3(
          x: _hitInfo.point.x,
          y: _hitInfo.collider.bounds.max.y,
          z: _hitInfo.point.z);
      }
      return _hitInfo.point;
    }
  }
}
