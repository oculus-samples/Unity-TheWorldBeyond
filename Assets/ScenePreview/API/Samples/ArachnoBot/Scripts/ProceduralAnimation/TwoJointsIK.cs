using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(LineRenderer))]
public class TwoJointsIK : MonoBehaviour
{
  [Tooltip("Increasing this can improve the precision of the knee")]
  public int iterationCount = 1;
  [Tooltip("Length of the upper bone of the leg")]
  public float femurLength;
  [Tooltip("Length of the lower bone of the leg")]
  public float tibiaLength;
  [Tooltip("An extra padding used when raycasting")]
  public float footProjectionPadding;
  [Tooltip("A gameobject representing the hip")]
  public Transform hip;
  [Tooltip("A gameobject representing the knee")]
  public Transform knee;
  [Tooltip("A gameobject representing the foot")]
  public Transform foot;
  [Tooltip("A gameobject representing the ideal position of the knee")]
  public Transform idealKnee;

  private LineRenderer lineRenderer;
  private Vector3[] positions = new Vector3[3];

  void Start()
  {
    lineRenderer = GetComponent<LineRenderer>();
    lineRenderer.positionCount = 3;
  }

  void Update()
  {
    // This is a simple hack to compute a "best effort" inverse kinematic using the previous pose
    // as starting point and correcting slightly.
    for (int i = 0; i < iterationCount; i++)
    {
      // The ideal knee resolves potential ambiguities, and provides a better looking pose.
      knee.position = Vector3.Lerp(knee.position, idealKnee.position, 0.1f);

      // Legs will be able to stretch.
      knee.position = Vector3.Lerp(
        hip.position + (knee.position - hip.position).normalized * femurLength,
        foot.position + (knee.position - foot.position).normalized * tibiaLength, 0.5f);
    }

    // Update the line renderer
    positions[0] = hip.position;
    positions[1] = knee.position;
    positions[2] = foot.position;
    lineRenderer.SetPositions(positions);
  }
}
