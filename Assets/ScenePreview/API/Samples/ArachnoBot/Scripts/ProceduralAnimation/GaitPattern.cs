using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BodyMovement))]
public class GaitPattern : MonoBehaviour
{
  [Tooltip("The first group of legs")]
  public List<SmartLeg> legsFirstGroup;
  [Tooltip("The second group of legs")]
  public List<SmartLeg> legsSecondGroup;
  [Tooltip("Determines how quickly the walking pattern progresses")]
  public AnimationCurve strideVsSurfaceDistance;
  float strideLenght;

  private BodyMovement bodyMovement;

  private void Start()
  {
    bodyMovement = GetComponent<BodyMovement>();
  }

  void Update()
  {
    bool fistAllGrounded = AllLegsResting(legsFirstGroup);
    bool secondAllGrounded = AllLegsResting(legsSecondGroup);
    strideLenght = strideVsSurfaceDistance.Evaluate(bodyMovement.surfaceDistance);
    float stride = (bodyMovement.spaceTraveled + bodyMovement.arcLength) % (2.0f * strideLenght);
    foreach (SmartLeg leg in legsSecondGroup)
    {
      // A leg in this group is allowed to lift only when the legs in the other group are grounded
      if (fistAllGrounded)
      {
        leg.footHysteresis.isAllowedToMove =
          stride >= strideLenght * 1.05f && stride < 1.5f * strideLenght;
      }
    }
    foreach (SmartLeg leg in legsFirstGroup)
    {
      if (secondAllGrounded)
      {
        leg.footHysteresis.isAllowedToMove =
          stride > 0.05 * strideLenght && stride < strideLenght * 0.5f;
      }
    }
  }

  bool AllLegsResting(List<SmartLeg> legs)
  {
    foreach (SmartLeg leg in legs)
    {
      if (leg.footHysteresis.state != FootHysteresis.State.Rest)
      {
        return false;
      }
    }
    return true;
  }
}
