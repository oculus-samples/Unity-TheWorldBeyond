using UnityEngine;

public class BodyMovement : MonoBehaviour
{
  [Tooltip("The target the character will try to follow")]
  [SerializeField] private Transform target;
  [Tooltip("How fast the body will follow the target position")]
  [SerializeField] private float linearLerpFactor = 1.0f;
  [Tooltip("How fast the body will follow the target rotation")]
  [SerializeField] private float angularLerpFactor = 1.0f;
  [SerializeField] private float arcLengthRadius = 2.0f;

  public float spaceTraveled = 0.0f;
  public float arcLength = 0.0f;
  [HideInInspector]
  public float surfaceDistance = 0.0f;
  private Vector3 oldPosition;
  private Quaternion oldRotation;

  private void Start()
  {
    oldPosition = target.position;
    oldRotation = target.rotation;
  }

  private void Update()
  {
    UpdateProperties();
    FollowTarget();
  }

  void UpdateProperties()
  {
    // We track how much we have traveled. This is used to drive the gait pattern,
    // keeping the number of steps proportional to the distance traveled.
    spaceTraveled += (target.position - oldPosition).magnitude;
    oldPosition = target.position;

    // Similarly, we keep track of how much we rotated.
    arcLength += Quaternion.Angle(oldRotation, target.rotation) *
      Mathf.Deg2Rad / (Mathf.PI * 2) *
      arcLengthRadius;
    oldRotation = target.rotation;

    // We also keep track of the body height over the terrain. We use this to guide the length and
    // speed of the steps, making faster and shorter steps when "crouched" and longer, slower steps
    // when "standing".

    // Layer 8 is used for the colliders of the Scene
    int layerMask = 1 << 8;
    RaycastHit info;
    if (Physics.Raycast(this.transform.position,
      -target.up,
      out info,
      Mathf.Infinity,
      layerMask))
    {
      surfaceDistance = info.distance;
    }
  }

  void FollowTarget()
  {


    // Follow the target
    this.transform.position = Vector3.Lerp(
     this.transform.position,
     target.position - target.up * 0.2f,
     linearLerpFactor * Time.deltaTime);

    // We orient the body so that the up vector points at the target. This introduces a nice
    // dynamic, where the body leans forward /backward/sideways and makes the animation cooler
    Vector3 up = target.position - this.transform.position;
    Vector3 right = Vector3.Cross(target.forward, up);
    Vector3 forward = Vector3.Cross(up, right);
    Quaternion desiredRot = Quaternion.LookRotation(forward, up);
    this.transform.rotation = Quaternion.Lerp(
      this.transform.rotation,
      desiredRot,
      angularLerpFactor * Time.deltaTime);
  }

  private void OnDrawGizmos()
  {
    Gizmos.color = Color.white;
    Gizmos.DrawLine(this.transform.position, target.position);
  }
}
