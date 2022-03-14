// Copyright 2014-Present Oculus VR, LLC. Proprietary and Confidential.

using System;

namespace Modules.Teleporter {
  [Serializable]
  public class TeleporterMode {
    public Input Input;
    public Targeter Targeter;
    public RotationMode RotationMode;
    public TeleportTelegraph Telegraph;

    public void Enable() {
      this.Input.enabled = true;
      this.Targeter.enabled = true;
      this.Telegraph.enabled = true;
    }

    public void Disable() {
      this.Input.enabled = false;
      this.Targeter.enabled = false;
      this.Telegraph.enabled = false;
    }

    public bool IsValid() {
      return Input != null && Targeter != null && Telegraph != null;
    }
  }
}
