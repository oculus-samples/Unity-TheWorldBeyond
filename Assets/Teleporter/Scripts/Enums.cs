// Copyright 2014-Present Oculus VR, LLC. Proprietary and Confidential.

namespace Modules.Teleporter {
  public enum Hand {
    None,
    Left,
    Right,
  }

  public enum RotationMode {
    None,
    Thumbstick,
    HandRotation,
  }

  public enum Action {
    None,
    MoveForward,
    MoveBack,
    MoveLeft,
    MoveRight,
    RotateLeft,
    RotateRight,
    Teleport,
  }
}
