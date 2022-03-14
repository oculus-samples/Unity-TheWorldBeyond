// Copyright 2014-Present Oculus VR, LLC. Proprietary and Confidential.

using UnityEngine;

namespace Modules.Teleporter {
  public class JoystickInput : Input {
    public override void Tick() {
      if (!_initialized) return;
      base.Tick();
      if (ActiveController.JoystickDown) {
        UpdateTeleportAction(ActiveController.JoystickPosition);
      } else if (ActiveController.JoystickUp) {
        TeleportExecute = true;
      } else if (!ActiveController.JoystickActive) {
        TeleportAction = Action.None;
      }
      if (OVRInput.Get(OVRInput.RawButton.LThumbstick) && OVRInput.Get(OVRInput.RawButton.RThumbstick)) {
        ResetOrientationButtonDown();
      }
    }

    private void UpdateTeleportAction(Vector2 position) {
      var nPos = position.normalized;
      var degs = Mathf.Acos(nPos.x) * Mathf.Rad2Deg;
      if (degs < 45f) {
        TeleportAction = Action.MoveRight;
        Strafe = true;
      } else if (degs > 135f) {
        TeleportAction = Action.MoveLeft;
        Strafe = true;
      } else if (nPos.y < 0) {
        TeleportAction = Action.MoveBack;
        Strafe = true;
      } else {
        TeleportAction = Action.Teleport;
        TeleportInit = true;
      }
    }
  }
}
