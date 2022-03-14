// Copyright 2014-Present Oculus VR, LLC. Proprietary and Confidential.

using System;
using UnityEngine;

namespace Modules.Teleporter {
  public class Pose {
    public static Pose Lerp(Pose p1, Pose p2, float factor) {
      var pose = new Pose();
      pose.pos = Vector3.Lerp(p1.pos, p2.pos, factor);
      pose.rot = Quaternion.Lerp(p1.rot, p2.rot, factor);
      return pose;
    }

    public Pose(Vector3 p, Quaternion r) {
      pos = p;
      rot = r;
    }

    public Pose() : this(Vector3.zero, Quaternion.identity) {
    }

    public Pose(Transform t) : this(t.position, t.rotation) {
    }

    public Pose(UnityEngine.Pose p) : this(p.position, p.rotation) {

    }

    public Vector3 pos;
    public Quaternion rot;

    public void Set(Transform t) {
      pos = t.position;
      rot = t.rotation;
    }
  }
}
