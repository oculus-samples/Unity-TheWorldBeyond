// Copyright 2014-Present Oculus VR, LLC. Proprietary and Confidential.

using UnityEngine;

namespace Modules.Teleporter {
  /// <summary>
  /// Base class for teleportation targets. In absence of a teleportation target, geometry collision should be used.
  /// </summary>
  public class TeleportationTarget : MonoBehaviour {
    [SerializeField] private bool _isInteractable = true;

    // Target overrides.
    public virtual bool OverrideTargetPosition => true;
    public virtual Vector3 TargetPosition => this.transform.position;
    public virtual bool OverrideTargetRotation => false;
    public virtual Quaternion TargetRotation => Quaternion.identity;

    // Telegraph overrides.
    public virtual bool TelegraphIsHidden => false;
    public virtual bool OverrideTelegraphPosition => OverrideTargetPosition;
    public virtual Vector3 TelegraphPosition => TargetPosition;
    public virtual bool OverrideTelegraphRotation => OverrideTargetRotation;
    public virtual Quaternion TelegraphRotation => TargetRotation;

    public bool IsOccupied { get; protected set; }

    public bool IsInteractable {
      get { return _isInteractable; }
      protected set { _isInteractable = value; }
    }

    protected virtual void Update() { }

    public void OnTeleportTargeterHit(Vector3 hitPoint, Vector3 forwardVector) {
      OnTeleportHitInternal(hitPoint, forwardVector);
    }

    public virtual void OnTeleport() { }

    protected virtual void OnTeleportHitInternal(Vector3 hitPoint, Vector3 forwardVector) { }

    public void UpdateTargetPose() {
      UpdateTargetPoseInternal();
    }

    protected virtual void UpdateTargetPoseInternal() { }
  }
}
