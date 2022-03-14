using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class FootHysteresis : MonoBehaviour
{
  public enum State
  {
    Rest,       // The foot is resting on the ground
    Flight,     // The foot is stepping toward the target
    Suspended,  // The foot has no place to go, and is kept in the air
  }
  [Tooltip("Expresses the current state of the foot")]
  public State state = State.Rest;
  [Tooltip("Will be true when the leg has found a good position to step toward")]
  public bool hasValidTarget = false;
  [Tooltip("Will be true when the gait pattern allows this leg to step")]
  public bool isAllowedToMove = true;
  [Tooltip("The target where this foot will step toward")]
  public Transform target;
  [Tooltip("The TwoJointsIK component for the leg this foot belongs to")]
  public TwoJointsIK ik;
  [Tooltip("The BodyMovement component of the body")]
  public BodyMovement bodyMovement;
  [Tooltip("The seek radius determines how far the target has to be for this foot to move toward " +
    "it. The surface distance is the height of the body above from the terrain.")]
  public AnimationCurve seekRadiusVsSurfaceDistance;
  [Tooltip("The lerp factor determines how fast the step will be. The surface distance is the " +
    "height of the body above from the terrain.")]
  public AnimationCurve lerpFactorVsSurfaceDistance;
  [Tooltip("When stepping, the foot will stop once it is within this distance to the target.")]
  public float stopRadius = 0.1f;

  private float currentSeekRadius;
  private float currentLerpFactor;

  void Update()
  {
    currentSeekRadius = seekRadiusVsSurfaceDistance.Evaluate(bodyMovement.surfaceDistance);
    currentLerpFactor = lerpFactorVsSurfaceDistance.Evaluate(bodyMovement.surfaceDistance);
    float sqrDistanceToHip = (ik.hip.position - this.transform.position).sqrMagnitude;
    float sqrDistance = (target.position - this.transform.position).sqrMagnitude;
    float sqrMaxLenght = (ik.femurLength + ik.tibiaLength + 0.3f) * (ik.femurLength + ik.tibiaLength + 0.3f);
    switch (state)
    {
      case State.Rest:
        if (sqrDistanceToHip > sqrMaxLenght)
        {
          state = State.Suspended;
        }
        else if (sqrDistance > currentSeekRadius * currentSeekRadius &&
          hasValidTarget &&
          isAllowedToMove)
        {
          state = State.Flight;
        }
        break;
      case State.Flight:
        // Go close to the target
        this.transform.position = Vector3.Lerp(
          this.transform.position,
          target.position,
          currentLerpFactor * Time.deltaTime);
        this.transform.rotation = Quaternion.Lerp(
          this.transform.rotation,
          target.rotation,
          currentLerpFactor * Time.deltaTime);

        if (sqrDistance <= stopRadius * stopRadius)
        {
          state = State.Rest;
        }
        // If mid-movement the target is invalid, go to suspended
        if (!hasValidTarget)
        {
          state = State.Suspended;
        }
        break;
      case State.Suspended:
        // Go to a comfortable position under the hip
        this.transform.position = Vector3.Lerp(
          this.transform.position,
          ik.hip.position - ik.transform.parent.up * ik.tibiaLength * 0.5f,
          currentLerpFactor * Time.deltaTime);
        // Wait to have a valid target
        if (hasValidTarget && sqrDistanceToHip < sqrMaxLenght)
        {
          state = State.Flight;
        }
        break;
      default:
        break;
    }
  }
  private void OnDrawGizmos()
  {
    Gizmos.color = Color.white;
    Gizmos.DrawLine(this.transform.position, target.position);
#if UNITY_EDITOR
    if (hasValidTarget)
    {
      if (isAllowedToMove)
      {
        Handles.color = Color.green;
      }
      else
      {
        Handles.color = Color.yellow;
      }
    }
    else
    {
      Handles.color = Color.red;
    }
    Handles.DrawWireArc(
      this.transform.position,
      this.transform.up,
      this.transform.forward,
      360,
      currentSeekRadius);
#endif
  }
}
