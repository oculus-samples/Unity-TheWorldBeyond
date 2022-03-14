using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TwoJointsIK))]
public class SmartLeg : MonoBehaviour
{
  [Tooltip("A game object used to drive the character")]
  public Transform bodyTarget;
  [Tooltip("A game object used to give the direction where the leg will try to step toward")]
  public Transform footPlacementDirection;
  [Tooltip("A game object used to visualize the ideal foot position")]
  public Transform idealFoot;
  [Tooltip("The FootHysteresis of this leg's foot")]
  public FootHysteresis footHysteresis;

  private TwoJointsIK ik;
  private float raycastDistance = 0.0f;

  void Start()
  {
    ik = GetComponent<TwoJointsIK>();
  }

  void Update()
  {
    // We want to know if there is a valid position for this leg to step toward. We do a simple
    // raycast in a given direction an see if we hit a surface within a sensible distance.
    footHysteresis.hasValidTarget = false;
    raycastDistance = ik.femurLength + ik.tibiaLength + ik.footProjectionPadding;

    // Layer 8 is used for Scene
    int layerMask = 1 << 8;
    RaycastHit info;
    if (Physics.Raycast(bodyTarget.position,
      (footPlacementDirection.position - bodyTarget.position).normalized,
      out info,
      Mathf.Infinity,
      layerMask))
    {
      float actualDistance = (info.point - ik.hip.position).magnitude;
      if(actualDistance < ik.femurLength + ik.tibiaLength + ik.footProjectionPadding)
      {
        raycastDistance = info.distance;
        idealFoot.position = info.point;
        Vector3 up = info.normal;
        Vector3 right = Vector3.Cross(idealFoot.forward, up);
        Vector3 forward = Vector3.Cross(up, right);
        idealFoot.rotation = Quaternion.LookRotation(forward, up);
        footHysteresis.hasValidTarget = true;
      }
    }
  }
  private void OnDrawGizmos()
  {
    if(ik)
    {
      Gizmos.color = Color.blue;
      Gizmos.DrawLine(
        bodyTarget.position,
        bodyTarget.position + (footPlacementDirection.position - bodyTarget.position).normalized *
        raycastDistance);

      Gizmos.DrawWireSphere(ik.idealKnee.position, 0.01f);
    }
  }
}
