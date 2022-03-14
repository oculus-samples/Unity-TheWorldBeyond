using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Resizable : MonoBehaviour
{
  public Vector3 pivotPosition = Vector3.zero;
  [Space(15)]
  [Range(0.001f, 10)] public float meshScale = 1;

  [Space(15)]
  public Method scalingX;
  [Range(0, 0.5f)] public float paddingX;
  [Range(-0.5f, 0)] public float paddingXMax;
  [Range(-0.5f, 0.5f)] public float outerPaddingLeft;
  [Range(-0.5f, 0.5f)] public float outerPaddingRight;
  public bool useYRatio;
  [Range(0, 10f)] public float YRatio = 1.0f;

  [Space(15)]
  public Method scalingY;
  [Range(0, 0.5f)] public float paddingY;
  [Range(-0.5f, 0)] public float paddingYMax;
  [Range(-0.5f, 0.5f)] public float outerPaddingTop;
  [Range(-0.5f, 0.5f)] public float outerPaddingBottom;
  [Range(0, 10f)] public float scaleAmountY = 1.0f;
  public bool useXRatio;
  [Range(0, 10f)] public float XRatio = 1.0f;

  [Space(15)]
  public Method scalingZ;
  [Range(0, 0.5f)] public float paddingZ;
  [Range(-0.5f, 0)] public float paddingZMax;
  [Range(-0.5f, 0.5f)] public float outerPaddingFront;
  [Range(-0.5f, 0.5f)] public float outerPaddingBack;
  [Range(0, 10f)] public float scaleAmountZ = 1.0f;

  public enum Method
  {
    Adapt,
    AdaptWithAsymmetricalPadding,
    Scale,
    None
  }

  [Space(15)]
  public Vector3 newSize;
  public Vector3 defaultSize;

  public Mesh mesh;
  public Bounds bounds;

  private void Start()
  {
    defaultSize = mesh.bounds.size * meshScale;
  }

#if (UNITY_EDITOR)
  void OnDrawGizmosSelected()
  {
    if (this.GetComponent<MeshFilter>().sharedMesh == null)
    {
      // The furniture piece was not customized yet, nothing to do here
      return;
    }

    bounds = this.GetComponent<MeshFilter>().sharedMesh.bounds;
    Gizmos.matrix = transform.localToWorldMatrix;
    Vector3 outerPaddingMin = new Vector3(
      outerPaddingLeft * 0.5f, outerPaddingBottom * 0.5f, outerPaddingFront * 0.5f);
    Vector3 outerPaddingMax = new Vector3(
      outerPaddingRight * 0.5f, outerPaddingTop * 0.5f, outerPaddingBack * 0.5f);
    Vector3 newCenter = bounds.center + outerPaddingMin - outerPaddingMax;

    Gizmos.color = new Color(1, 0, 0, 0.5f);
    switch (scalingX)
    {
      case Method.Adapt:
        Gizmos.DrawWireCube(newCenter, new Vector3(
          newSize.x * paddingX * 2, newSize.y, newSize.z));
        break;
      case Method.AdaptWithAsymmetricalPadding:
        Gizmos.DrawWireCube(newCenter + new Vector3(
          newSize.x * paddingX, 0, 0), new Vector3(0, newSize.y, newSize.z));
        Gizmos.DrawWireCube(newCenter + new Vector3(
          newSize.x * paddingXMax, 0, 0), new Vector3(0, newSize.y, newSize.z));
        break;
      case Method.None:
        Gizmos.DrawWireCube(newCenter, newSize);
        break;
    }

    Gizmos.color = new Color(0, 1, 0, 0.5f);
    switch (scalingY)
    {
      case Method.Adapt:
        Gizmos.DrawWireCube(newCenter, new Vector3(
          newSize.x, newSize.y * paddingY * 2, newSize.z));
        break;
      case Method.AdaptWithAsymmetricalPadding:
        Gizmos.DrawWireCube(newCenter + new Vector3(0, newSize.y * paddingY, 0),
          new Vector3(newSize.x, 0, newSize.z));
        Gizmos.DrawWireCube(newCenter + new Vector3(0, newSize.y * paddingYMax, 0),
          new Vector3(newSize.x, 0, newSize.z));
        break;
      case Method.None:
        Gizmos.DrawWireCube(newCenter, newSize);
        break;
    }

    Gizmos.color = new Color(0, 0, 1, 0.5f);
    switch (scalingZ)
    {
      case Method.Adapt:
        Gizmos.DrawWireCube(newCenter, new Vector3(
          newSize.x, newSize.y, newSize.z * paddingZ * 2));
        break;
      case Method.AdaptWithAsymmetricalPadding:
        Gizmos.DrawWireCube(newCenter + new Vector3(0, 0, newSize.z * paddingZ),
          new Vector3(newSize.x, newSize.y, 0));
        Gizmos.DrawWireCube(newCenter + new Vector3(0, 0, newSize.z * paddingZMax),
          new Vector3(newSize.x, newSize.y, 0));
        break;
      case Method.None:
        Gizmos.DrawWireCube(newCenter, newSize);
        break;
    }

    Gizmos.color = new Color(0, 1, 1, 1);
    Gizmos.DrawWireCube(newCenter, newSize);
  }
#endif
}
