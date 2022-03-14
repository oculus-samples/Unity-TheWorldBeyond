using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(OVRSceneObject))]
public class VolumeAndPlaneSwitcher : MonoBehaviour
{
  public OVRSceneObject planePrefab;
  public OVRSceneObject volumePrefab;
  public enum GeometryType
  {
    Plane,
    Volume,
  }
  [System.Serializable]
  public struct LabelGeometryPair
  {
    public string label;
    public GeometryType desiredGeometryType;
  }
  public List<LabelGeometryPair> desiredSwitches;
  private OVRSceneObject ovrSceneObject;

  // Start is called before the first frame update
  void Start()
  {
    StartCoroutine(DelayedSwitch());
  }

  IEnumerator DelayedSwitch()
  {
    yield return new WaitForSeconds(0.4f);
    Switch();
  }

  void Switch()
  {
    ovrSceneObject = GetComponent<OVRSceneObject>();
    foreach (LabelGeometryPair pair in desiredSwitches)
    {
      if (pair.label == ovrSceneObject.classification.labels[0])
      {
        Vector3 position = Vector3.zero;
        Quaternion rotation = Quaternion.identity;
        Vector3 localScale = Vector3.zero;
        GameObject g = null;
        switch (pair.desiredGeometryType)
        {
          case GeometryType.Plane:
          {
            Debug.Log($"IN Volume Position {transform.position}, Dimensions: {ovrSceneObject.dimensions}");
            // This object is a volume, but we want a plane instead.
            SceneObjectHelper.GetTopPlaneFromVolume(
              transform,
              ovrSceneObject.dimensions,
              out position,
              out rotation,
              out localScale);
            Debug.Log($"OUT Plane Position {position}, Dimensions: {localScale}");
            g = Instantiate(planePrefab.gameObject, this.transform.parent);
            break;
          }
          case GeometryType.Volume:
          {
            Debug.Log($"IN Plane Position {transform.position}, Dimensions: {ovrSceneObject.dimensions}");
            // This object is a plane, but we want a volume instead.
            SceneObjectHelper.GetVolumeFromTopPlane(
              transform,
              ovrSceneObject.dimensions,
              out position,
              out rotation,
              out localScale);
            Debug.Log($"OUT Volume Position {position}, Dimensions: {localScale}");
            g = Instantiate(volumePrefab.gameObject, this.transform.parent);
            break;
          }
        }
        if (g != null)
        {
          g.transform.position = position;
          g.transform.rotation = rotation;
          g.GetComponent<OVRSceneObject>().dimensions = localScale;
          for (int i = 0; i < ovrSceneObject.classification.labels.Length; i++)
          {
            g.GetComponent<OVRSceneObject>().classification.labels[i] =
              ovrSceneObject.classification.labels[i];
          }
          Destroy(this.gameObject);
        }
      }
    }
    // IF we arrived here, no conversion was needed. Let's remove this component
    Destroy(this);
  }
}
