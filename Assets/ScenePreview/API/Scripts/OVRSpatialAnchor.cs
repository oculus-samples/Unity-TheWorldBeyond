using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OVRSpatialAnchor : MonoBehaviour
{
  public static OVRSpatialAnchor floorAnchor = null;

  public UInt64 Handle { get { return myHandle; } set { myHandle = value; } }

  public bool SetEnable { get; set; }

  private UInt64 myHandle = UInt64.MinValue;


  public void UpdateTransform()
  {
    if (SetEnable && myHandle != UInt64.MinValue)
    {
      var pose = OVRPlugin.LocateSpace(ref myHandle, OVRPlugin.GetTrackingOriginType());
      // NOTE: Here the anchor's orientation is rotated 180 degrees so that the plane's normal
      // is along +z direction. This is because anchors are created in right-hand
      // coordinate and needs to be transformed to Unity's left-hand coordinate, which makes
      // the plane's normal in -z direction before this rotation.
      var worldSpacePose = OVRExtensions.ToWorldSpacePose(pose.ToOVRPose().Rotate180AlongX());
      this.gameObject.transform.position = worldSpacePose.position;
      this.gameObject.transform.rotation = worldSpacePose.orientation;
    }
  }

  void Update()
  {
    UpdateTransform();
  }
}
