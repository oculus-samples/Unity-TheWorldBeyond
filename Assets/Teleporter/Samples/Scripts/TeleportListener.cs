using UnityEngine;
using Modules.Teleporter;

public class TeleportListener : MonoBehaviour {
  [SerializeField] private TeleporterOVRAvatar teleporterOVRAvatar;

  private void OnEnable() {
    teleporterOVRAvatar.OnTeleport += TeleporterOVRAvatar_OnTeleport;
    teleporterOVRAvatar.OnTeleporterStateChanged +=
      TeleporterOVRAvatar_OnTeleporterStateChanged;
  }

  private void OnDisable() {
    teleporterOVRAvatar.OnTeleport += TeleporterOVRAvatar_OnTeleport;
    teleporterOVRAvatar.OnTeleporterStateChanged -=
      TeleporterOVRAvatar_OnTeleporterStateChanged;
  }

  private void TeleporterOVRAvatar_OnTeleport(Modules.Teleporter.Pose pose) {
    // use this to keep track of the pose for e.g. networked experiences
    Debug.Log("Rig position:" + pose.pos);
    Debug.Log("Rig rotation" + pose.rot);
  }

  private void TeleporterOVRAvatar_OnTeleporterStateChanged(TeleporterOVRAvatar.TeleporterState state) {
    if (state == TeleporterOVRAvatar.TeleporterState.begin) {
      Debug.Log("teleport: begin");
    } else if (state == TeleporterOVRAvatar.TeleporterState.cancel) {
      Debug.Log("teleport: cancel");
    } else if (state == TeleporterOVRAvatar.TeleporterState.complete) {
      Debug.Log("teleport: invalid");
    } else if (state == TeleporterOVRAvatar.TeleporterState.invalid) {
      Debug.Log("teleporter: invalid");
    } else if (state == TeleporterOVRAvatar.TeleporterState.strafe) {
      Debug.Log("teleport: strafe");
    }
  }
}
