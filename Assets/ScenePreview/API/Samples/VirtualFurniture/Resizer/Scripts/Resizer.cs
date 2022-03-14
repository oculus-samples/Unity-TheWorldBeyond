using System.Collections;
using UnityEngine;

[ExecuteInEditMode]
public class Resizer : MonoBehaviour
{
  public ResizableObjectsList resizableObjectsList;
  [Range(1, 20)] public int pieceNumber;
  [HideInInspector]
  public ResizableObjectsList.FurniturePiece piece;

  [Space(15)]
  public Vector3 newSize = Vector3.one;

  [Space(15)]
  public bool createMesh;
  public bool resizeInEditMode;
  public bool cleanUpAfterInstantiation;

  private GameObject prefab;
  private Resizable resizable;
  private Mesh startingMesh;
  private Mesh resizedMesh;

  private void Update()
  {
    if (resizable)
    {
      //Shader.SetGlobalVector("_Pivot_Offset", resizable.pivotPosition);
    }

    pieceNumber = Mathf.Clamp(pieceNumber, 1, resizableObjectsList.objects.Count);

    if (createMesh)
    {
      CreateResizedObject(pieceNumber);
      createMesh = false;
      if (cleanUpAfterInstantiation)
      {
        StartCoroutine(DelayedCleanup());
      }
    }

    if (resizeInEditMode && resizable)
    {
      ApplyMesh(resizable, prefab, newSize, resizable.pivotPosition);
    }
  }

  public void CreateResizedObject()
  {
    CreateResizedObject(Mathf.Clamp(pieceNumber, 1, resizableObjectsList.objects.Count));
  }

  public void CreateResizedObject(int objectID)
  {
    if (transform.childCount != 0)
    {
      for (int i = transform.childCount - 1; i >= 0; i--)
      {
        DestroyImmediate(transform.GetChild(i).gameObject);
      }
    }

    piece = resizableObjectsList.objects[objectID - 1];

    prefab = Instantiate(piece.prefab);
    prefab.name = piece.objectName;
    prefab.transform.position = Vector3.zero;
    prefab.transform.rotation = Quaternion.identity;
    resizable = prefab.GetComponent<Resizable>();
    if (resizable != null)
    {
      startingMesh = resizable.mesh;
      ApplyMesh(resizable, prefab, newSize, resizable.pivotPosition);
    }

    // child it after creation so the bounds math plays nicely
    prefab.transform.parent = transform;
    prefab.transform.localPosition = Vector3.zero;
    prefab.transform.localRotation = Quaternion.identity;

    // make sure the new mesh is in the Furniture layer
    // (for proper rendering during relocalization view)
    prefab.layer = this.gameObject.layer;

    if (resizable != null)
    {
      Shader.SetGlobalVector("_Pivot_Offset", resizable.pivotPosition);
    }
  }

  public IEnumerator DelayedCleanup()
  {
    yield return new WaitForSeconds(0.3f);

    CleanUp();
  }

  public void CleanUp()
  {
    resizable.gameObject.transform.parent = this.transform.parent;
    foreach (Resizable r in resizable.GetComponentsInChildren<Resizable>())
    {
      Destroy(r);
    }
    foreach (ResizableDependent rd in resizable.GetComponentsInChildren<ResizableDependent>())
    {
      Destroy(rd);
    }
    Destroy(resizable);
    Destroy(this.gameObject);
  }

  public void ApplyMesh(Vector3 _newSize)
  {
    ApplyMesh(resizable, prefab, _newSize, resizable.pivotPosition);
  }

  public void ApplyMesh(Resizable resizable, GameObject prefab, Vector3 newSize, Vector3 newPivot)
  {
    if (resizable.scalingX == Resizable.Method.None)
      newSize.x = resizable.defaultSize.x * resizable.meshScale;

    if (resizable.scalingY == Resizable.Method.None)
      newSize.y = resizable.defaultSize.y * resizable.meshScale;

    if (resizable.scalingZ == Resizable.Method.None)
      newSize.z = resizable.defaultSize.z * resizable.meshScale;

    resizable.newSize = newSize;
    resizedMesh = AdaptedMesh(resizable, newSize, newPivot);
    MeshFilter mf = prefab.GetComponentInChildren<MeshFilter>();
    mf.sharedMesh = resizedMesh;
    mf.sharedMesh.RecalculateBounds();
  }

  private Mesh ScaledMesh(Resizable resizable)
  {
    Vector3[] scaledOriginalVertices = resizable.mesh.vertices;

    for (int i = 0; i < scaledOriginalVertices.Length; i++)
    {
      Vector3 pos = scaledOriginalVertices[i];
      pos.x = Mathf.Abs(pos.x) * Mathf.Sign(pos.x) * resizable.meshScale;
      pos.y = Mathf.Abs(pos.y) * Mathf.Sign(pos.y) * resizable.meshScale;
      pos.z = Mathf.Abs(pos.z) * Mathf.Sign(pos.z) * resizable.meshScale;
      scaledOriginalVertices[i] = pos;
    }

    Mesh scaledOriginalMesh = Instantiate(resizable.mesh);
    scaledOriginalMesh.vertices = scaledOriginalVertices;
    scaledOriginalMesh.RecalculateBounds();
    return scaledOriginalMesh;
  }

  private Mesh AdaptedMesh(Resizable resizable, Vector3 newSize, Vector3 newPivot)
  {
    Mesh originalMesh = ScaledMesh(resizable);
    Vector3[] resizedVertices = originalMesh.vertices;
    Vector3 originalBounds = originalMesh.bounds.size;

    // Force scaling if newSize is smaller than the original mesh
    Resizable.Method methodX = (originalBounds.x < newSize.x)
      ? resizable.scalingX : Resizable.Method.Scale;
    Resizable.Method methodY = (originalBounds.y < newSize.y)
      ? resizable.scalingY : Resizable.Method.Scale;
    Resizable.Method methodZ = (originalBounds.z < newSize.z)
      ? resizable.scalingZ : Resizable.Method.Scale;

    if (resizable.useYRatio)
    {
      newSize.x = newSize.y * resizable.YRatio;
    }
    else if (resizable.useXRatio)
    {
      newSize.y = newSize.x * resizable.XRatio;
    }

    for (int i = 0; i < resizedVertices.Length; i++)
    {
      Vector3 pos = resizedVertices[i];
      pos.x = AdaptVertices(
        methodX,
        pos.x, originalBounds.x, newSize.x,
        resizable.paddingX, resizable.paddingXMax,
        resizable.outerPaddingLeft, resizable.outerPaddingRight,
        newPivot.x);
      pos.y = AdaptVertices(
        methodY, pos.y, originalBounds.y, newSize.y,
        resizable.paddingY, resizable.paddingYMax,
        resizable.outerPaddingBottom, resizable.outerPaddingTop,
        newPivot.y);
      pos.z = AdaptVertices(
        methodZ, pos.z, originalBounds.z, newSize.z,
        resizable.paddingZ, resizable.paddingZMax,
        resizable.outerPaddingFront, resizable.outerPaddingBack,
        newPivot.z);
      resizedVertices[i] = pos;
    }

    Mesh clonedMesh = Instantiate(originalMesh);
    clonedMesh.vertices = resizedVertices;
    return clonedMesh;
  }

  private float AdaptVertices(
    Resizable.Method method,
    float pos,
    float currentSize,
    float newSize,
    float padding,
    float paddingMax,
    float outerPadding,
    float outerPaddingMax,
    float pivot)
  {

    float resizedRatio = currentSize / 2
      * (newSize / 2 * (1 / (currentSize / 2)))
      - currentSize / 2;
    float outPadMin = 0;
    float outPadMax = 0;

    switch (method)
    {
      case Resizable.Method.Adapt:
        if (pos < padding)
          pos -= outerPadding;
        if (pos > padding)
          pos += outerPaddingMax;

        if (Mathf.Abs(pos) >= padding)
          pos = resizedRatio * Mathf.Sign(pos) + pos;
        break;

      case Resizable.Method.AdaptWithAsymmetricalPadding:
        if (pos < padding)
          pos -= outerPadding;
        if (pos > paddingMax)
          pos += outerPaddingMax;

        if (pos >= padding)
          pos = resizedRatio * Mathf.Sign(pos) + pos;
        if (pos <= paddingMax)
          pos = resizedRatio * Mathf.Sign(pos) + pos;
        break;

      case Resizable.Method.Scale:
        pos = (newSize + outerPadding + outerPaddingMax) / (currentSize / pos);
        break;

      case Resizable.Method.None:
        break;
    }

    float pivotPos = newSize * (-pivot * 0.5f);
    pos += pivotPos;
    return pos;
  }

  public void CreateMeshFromFurniture(Vector3 cubeSize)
  {
    newSize = cubeSize;
    CreateResizedObject(pieceNumber);
  }
}
