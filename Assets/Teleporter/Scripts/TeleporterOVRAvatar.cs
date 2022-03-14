using System;
using UnityEngine;

namespace Modules.Teleporter {
  public class TeleporterOVRAvatar : MonoBehaviour {
    private const float SnapAngle = 1f;

    [SerializeField] private Animator transition;
    [SerializeField] private bool showTransition = true;
    [SerializeField] private TeleporterMode _defaultMode;
    [SerializeField] private bool _allowStrafing = true;

    // members used for non-teleportation locomotion logic.
    [SerializeField] private float _rotateBy = 25.0f;
    [SerializeField] private AudioSource teleporterSound;
    [SerializeField] private AudioClip teleportClip;
    public event Action<Pose> OnTeleport;

    public enum TeleporterState { begin, cancel, invalid, complete, strafe };
    public event Action<TeleporterState> OnTeleporterStateChanged;

    private TeleporterMode _mode;
    private GameObject user;
    private bool _aiming;
    private Pose _startPose = new Pose();
    private Pose _fromPose;
    private Pose _toPose;
    private Hand _activeHand = Hand.None;
    private Vector3 _initialHandRotation;
    private OVRCameraRig ovrCamera;

    // Convenience accessors.
    private Input Input => _mode.Input;
    private Targeter Targeter => _mode.Targeter;
    private TeleportTelegraph Telegraph => _mode.Telegraph;

    public bool IsTeleporterActive { set; get; }

    #region Unity Messages
    private void Awake() {
      IsTeleporterActive = true;
      ovrCamera = OVRManager.instance.gameObject.GetComponent<OVRCameraRig>();

      _mode = _defaultMode;
      _mode.Enable();

      // find root
      user = OVRManager.instance.gameObject;
      if (user != null) {
        _startPose.Set(user.transform);
      }
    }

    private void OnEnable() {
      Input.Init();
      if (user == null) {
        Debug.LogWarning("No _user has been set. Disabling teleporter.");
        this.enabled = false;
      }
    }

    private void Update() {
      if (!IsTeleporterActive && !_aiming) return;
      // Using Init->Tick->Kill cycle to ensure the order of updates.
      Input.Tick();
      if (!_aiming) {
        if (Input.TeleportInit) {
          OnTeleporterStateChanged?.Invoke(TeleporterState.begin);
          StartAiming();
        } else if (Input.Strafe && _allowStrafing) {
          OnTeleporterStateChanged?.Invoke(TeleporterState.strafe);
          TryStrafe();
        }
      } else {
        if (Input.TeleportExecute) {
          ExecuteTeleport();
        } else if (Input.TeleportCancel) {
          OnTeleporterStateChanged?.Invoke(TeleporterState.cancel);
          EndAiming();
        } else {
          UpdateAiming();
        }
      }
    }
    #endregion // Unity Messages

    #region Aiming
    private void StartAiming() {
      _aiming = true;
      Targeter.Init(Input.ActiveHand);
      _activeHand = Input.ActiveHand;
      // grab a snapshot of handrotation as reference
      _initialHandRotation = OVRInput.GetLocalControllerRotation(_activeHand == Hand.Left
        ? OVRInput.Controller.LTouch
        : OVRInput.Controller.RTouch).eulerAngles;
    }

    private void UpdateAiming() {
      // Handle rotation.
      var inputRotationRaw = Mathf.Rad2Deg * Mathf.Atan2(
        Input.ActiveJoystickPosition.x,
        Input.ActiveJoystickPosition.y);

      var rotation = Quaternion.Euler(0, Targeter.Origin.rotation.eulerAngles.y, 0) *
        Quaternion.Euler(0, inputRotationRaw, 0);

      // Handle targeting.
      Targeter.Tick(rotation);

      var teleportTarget = Targeter.HitTeleportObject;
      Telegraph.transform.position =
        teleportTarget != null && teleportTarget.OverrideTelegraphPosition
          ? teleportTarget.TelegraphPosition
          : Targeter.TargetPosition;
      Telegraph.transform.rotation =
        teleportTarget != null && teleportTarget.OverrideTelegraphRotation
          ? teleportTarget.TelegraphRotation
          : Quaternion.Euler(0, Mathf.Round(rotation.eulerAngles.y / SnapAngle) * SnapAngle, 0);
      Telegraph.Renderer.enabled = teleportTarget != null
        ? !teleportTarget.TelegraphIsHidden
        : Targeter.ValidTarget;
    }

    private void EndAiming() {
      _aiming = false;
      Telegraph.Renderer.enabled = false;
      Targeter.Kill();
    }
    #endregion

    private void ExecuteTeleport() {
      if (!_aiming) {
        OnTeleporterStateChanged?.Invoke(TeleporterState.cancel);
        return;
      }
      EndAiming();
      if (!Targeter.ValidTarget) {
        OnTeleporterStateChanged?.Invoke(TeleporterState.invalid);
        return;
      }
      if (showTransition) transition.SetTrigger("Start");
      var teleportTarget = Targeter.HitTeleportObject;
      var teleportPosition = teleportTarget != null && teleportTarget.OverrideTargetPosition
        ? teleportTarget.TargetPosition
        : Targeter.TargetPosition;
      var teleportRotation = teleportTarget != null && teleportTarget.OverrideTargetRotation
        ? teleportTarget.TargetRotation
        : Telegraph.transform.rotation;
      var pose = new Pose(teleportPosition, teleportRotation);
      Targeter.HitTeleportObject?.OnTeleport();
      Teleport(pose);
      _activeHand = Hand.None;
      _initialHandRotation = Vector3.zero;
      teleporterSound.PlayOneShot(teleportClip);
      OnTeleporterStateChanged?.Invoke(TeleporterState.complete);
    }

    private void TryStrafe() {
      if ((Input.TeleportAction == Action.MoveLeft || Input.TeleportAction == Action.MoveRight)) {
        float rotateDir = 0f;
        switch (Input.TeleportAction) {
          case Action.MoveLeft:
            rotateDir = _rotateBy * -1;
            break;
          case Action.MoveRight:
            rotateDir = _rotateBy;
            break;
          default:
            rotateDir = 0f;
            break;
        }
        user.transform.RotateAround(ovrCamera.centerEyeAnchor.position, Vector3.up, rotateDir);
        Targeter.Clean();
        var pose = new Pose(user.transform.position, user.transform.rotation);
        OnTeleport?.Invoke(pose);
      }
    }

    public void Teleport(Pose targetPose) {
      //We are only calculating the rotation around the Y axes which would affect a Character relative to a horizontal floor.
      //We don't want pitch and roll of the headset to affect the new position.
      float deltaYRotation = targetPose.rot.eulerAngles.y - ovrCamera.centerEyeAnchor.rotation.eulerAngles.y;
      Vector3 targetToHmdDeltaRotation = Vector3.up * deltaYRotation;
      Quaternion newRootRotation = Quaternion.Euler(targetToHmdDeltaRotation) * user.transform.rotation;
      Vector3 rootToHmdDeltaPos = ovrCamera.centerEyeAnchor.position - user.transform.position;
      //Ignore the delta height between the headset and the user root node in calculation of the delta.
      rootToHmdDeltaPos.y = 0;
      Vector3 deltaPosRotated = Quaternion.Euler(targetToHmdDeltaRotation) * rootToHmdDeltaPos;
      Pose userPose = new Pose(targetPose.pos - deltaPosRotated, newRootRotation);
      user.transform.position = userPose.pos;
      user.transform.rotation = userPose.rot;
      OnTeleport?.Invoke(userPose);
    }
  }
}
