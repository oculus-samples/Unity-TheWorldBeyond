using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlignToClosestSurfaces : MonoBehaviour
{
  [Tooltip("When true the target will keep the desired distance above the body")]
  public bool enforceDesiredDistance = true;
  [Tooltip("The desired body height over the terrain")]
  [Range(0.15f, 0.55f)]
  [SerializeField] private float desiredSurfaceDistance = 0.5f;
  [SerializeField] private Transform body;
  [SerializeField] private Transform shadow;
  [SerializeField] float lerpFactor = 0.1f;
  [SerializeField] float searchRadius = 1.0f;
  private Vector3 normal = Vector3.up;
  bool canChange = true;
  List<Vector3> nearestPoints = new List<Vector3>();
  List<Vector3> nearestNormals = new List<Vector3>();
  private void Update()
  {
    if (canChange)
    {
      // Layer 8 is used for the colliders of the Scene
      int layerMask = 1 << 8;
      Collider[] hitColliders = Physics.OverlapSphere(
        body.position,
        searchRadius,
        layerMask);
      List<Vector3> normals = new List<Vector3>();
      List<float> weights = new List<float>();
      List<float> distances = new List<float>();
      float weightSum = 0.0f;
      nearestPoints.Clear();
      nearestNormals.Clear();
      foreach (var hitCollider in hitColliders)
      {
        if (hitCollider.gameObject.layer == 8)
        {
          OVRSceneObject sceneObject = hitCollider.transform.parent.GetComponent<OVRSceneObject>();
          string label = sceneObject.classification.labels[0];
          if (label == "WALL_ART" || label == "DOOR_FRAME" || label == "WINDOW_FRAME")
          {
            // These surfaces don't really add any useful information for the navigation
            // As the wall underneath already captures the plane
            continue;
          }
          Vector3 closestPoint = hitCollider.ClosestPoint(body.position);
          Vector3 displacement = (body.position - closestPoint);
          nearestPoints.Add(closestPoint);
          Vector3 surfaceNormal;
          float weightOffset = 0.0f;
          if (label == "WALL_FACE" || label == "FLOOR" || label == "CEILING")
          {
            // This should ensure that he bot never steps outside of the room, as the normal
            // will always point inward.
            surfaceNormal = hitCollider.transform.parent.forward;

            // In case we are outside, push the bot inward
            if (Vector3.Dot(surfaceNormal, displacement) < -0.1f)
            {
              this.transform.position = closestPoint + surfaceNormal * desiredSurfaceDistance;
            }

            // Increase the weight associated with these surfaces
            weightOffset = 0.5f;
          }
          else
          {
            // For convex shapes this works as normal.
            surfaceNormal = displacement.normalized;
          }
          nearestNormals.Add(surfaceNormal);
          if (Vector3.Dot(surfaceNormal, body.up) > -0.2f)
          {
            float distance = (closestPoint - body.position).magnitude;
            float weight = Mathf.Clamp01(1.0f - distance / searchRadius) + weightOffset;
            weightSum += weight;
            distances.Add(distance);
            weights.Add(weight);
            normals.Add(surfaceNormal);
          }
        }
      }

      float averageDistance = 0.0f;
      if (weightSum > 0.0f)
      {
        normal = Vector3.zero;
        for (int i = 0; i < weights.Count; i++)
        {
          normal += normals[i] * weights[i] / weightSum;
          averageDistance += distances[i] * weights[i] / weightSum;
        }
        normal.Normalize();
      }
      body.GetComponent<BodyMovement>().surfaceDistance = averageDistance;
      shadow.transform.localPosition = -Vector3.up * averageDistance;
      float alpha = Mathf.InverseLerp(0.45f, 0.05f, averageDistance);
      shadow.GetComponent<MeshRenderer>().material.SetFloat("_Alpha", alpha);
    }

    Vector3 right = Vector3.Cross(this.transform.forward, normal);
    Vector3 forward = Vector3.Cross(normal, right);
    this.transform.rotation = Quaternion.Lerp(
      this.transform.rotation,
      Quaternion.LookRotation(forward, normal),
      lerpFactor);

    // Add a little up/down motion
    float desDist = 0.35f;
    if (Vector3.Dot(transform.up, Vector3.up) < -0.5f)
    {
      // Stretch the legs when hanging from the ceiling
      desDist = 0.55f;
    }
    else if (Vector3.Dot(transform.up, Vector3.up) > -0.1f)
    {
      desDist = 0.3f;
    }
    desiredSurfaceDistance = Mathf.Lerp(
      desiredSurfaceDistance,
      Mathf.Sin(Time.time) * 0.05f + desDist,
      lerpFactor);


    if (enforceDesiredDistance)
    {
      // We move the target position to hover the body by a desired distance
      this.transform.position = Vector3.Lerp(
        this.transform.position,
        this.transform.position + normal *
        (desiredSurfaceDistance - body.GetComponent<BodyMovement>().surfaceDistance),
        0.05f);
    }
  }

  private void OnDrawGizmos()
  {
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(body.position, searchRadius);

    Gizmos.color = Color.yellow;
    for(int i = 0; i < nearestPoints.Count; i++)
    {
      Gizmos.DrawSphere(nearestPoints[i], 0.05f);
      Gizmos.DrawLine(nearestPoints[i], nearestPoints[i] + nearestNormals[i]);
    }
  }

  IEnumerator Pause()
  {
    canChange = false;
    yield return new WaitForSeconds(0.6f);
    canChange = true;
  }
}
