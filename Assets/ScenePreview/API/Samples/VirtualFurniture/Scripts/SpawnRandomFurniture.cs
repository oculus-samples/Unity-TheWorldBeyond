using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(OVRSceneObject))]
public class SpawnRandomFurniture : MonoBehaviour
{
  private OVRSceneObject sceneObject;
  public List<ResizableObjectsList> objectList = new List<ResizableObjectsList>();
  public GameObject roomLightPrefab;
  public Resizer resizerPrefab;

  void Start()
  {
    sceneObject = GetComponent<OVRSceneObject>();
    ResizableObjectsList list;
    if (GetSuitableList(out list))
    {
      SpawnRandomEntry(list);
    }

    // Add a light to the ceiling
    if (SemanticClassificationHelper.HasLabel("CEILING", sceneObject.classification))
    {
      Instantiate(roomLightPrefab, sceneObject.transform);
    }
  }

  bool GetSuitableList(out ResizableObjectsList list)
  {
    list = null;
    foreach (ResizableObjectsList l in objectList)
    {
      if (SemanticClassificationHelper.HaveCommonLabel(
        l.classification,
        sceneObject.classification))
      {
        list = l;
        return true;
      }
    }
    return false;
  }

  void SpawnRandomEntry(ResizableObjectsList list)
  {
    GameObject resizerGameObject = Instantiate(resizerPrefab.gameObject, sceneObject.transform);
    Resizer resizer = resizerGameObject.GetComponent<Resizer>();

    Vector3 dimensions = sceneObject.dimensions;
    Vector3 position = this.transform.position;
    Quaternion rotation = this.transform.rotation;
    Vector3 localScale = this.transform.localScale;
    // These surfaces are defind with the top plane, but we are interested in the volume below
    if (sceneObject.classification.labels[0] == "DESK" ||
      sceneObject.classification.labels[0] == "TABLETOP" ||
      sceneObject.classification.labels[0] == "COUCH")
    {
      SceneObjectHelper.GetVolumeFromTopPlane(
        transform,
        sceneObject.dimensions,
        out position,
        out rotation,
        out localScale);
      dimensions = localScale;
      // The pivot for the resizer is at the top
      position.y += localScale.y / 2.0f;
    }
    resizer.transform.position = position;
    resizer.transform.rotation = rotation;
    resizer.resizableObjectsList = list;
    resizer.pieceNumber = Random.Range(0, list.objects.Count);
    resizer.newSize = dimensions;
    resizer.createMesh = true;
  }
}
