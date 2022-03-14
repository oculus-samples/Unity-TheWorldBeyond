using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ResizableDependent : MonoBehaviour
{
  private Resizer resizer;
  private Resizable resizable;

  public Vector3 defaultSize;
  public bool useBoundsSizeAsDefault = false;

  public Vector3 customSize;
  public Vector3 customPosition;

  [Header("RESIZING")]
  public bool keepSizeX;
  public bool keepSizeY;
  public bool keepSizeZ;
  private float sizeX;
  private float sizeY;
  private float sizeZ;
  public Vector3 newSize;

  [Header("ANCHORING")]
  [Space(15)]
  public Anchoring anchorX = Anchoring.Zero;
  [Range(-1f, 1f)] public float anchorXAmount = 0.5f;
  public Anchoring anchorY = Anchoring.Zero;
  [Range(-1f, 1f)] public float anchorYAmount = 0.5f;
  public Anchoring anchorZ = Anchoring.Zero;
  [Range(-1f, 1f)] public float anchorZAmount = 0.5f;

  public enum Anchoring
  {
    Sides,
    Zero,
    Custom
  }

  private float posX;
  private float posY;
  private float posZ;

  [Header("PADDING")]
  public PaddingReference[] references;

  [System.Serializable]
  public class PaddingReference
  {
    public GameObject reference;
    public TransformFrom from;
    public TransformTo to;
    [Range(-2, 2)] public float amount = 1.0f;
    public bool move = true;
    public bool addDefaultSize = false;
  }

  public enum TransformFrom
  {
    ScaleX,
    ScaleY,
    ScaleZ,
    PositionX,
    PositionY,
    PositionZ,
  }

  public enum TransformTo
  {
    PaddingLeft,
    PaddingRight,
    PaddingBottom,
    PaddingTop,
    PaddingFront,
    PaddingBack,
    ScaleX,
    ScaleY,
    ScaleZ,
  }

  // Start is called before the first frame update
  void Start()
  {
    resizable = GetComponent<Resizable>();
    if (useBoundsSizeAsDefault)
      defaultSize = resizable.defaultSize;
    customSize = defaultSize;
    AdaptSubMesh();
  }

  // Update is called once per frame
  void Update()
  {

  }

  public void AdaptSubMesh()
  {
    if (transform.GetComponentInParent<Resizer>())
    {
      resizer = transform.GetComponentInParent<Resizer>();
      if (GetComponent<Resizable>())
      {
        ResizeObject();
        AnchorObject();
        CalculatePadding(references);
        resizer.ApplyMesh(resizable, gameObject, newSize, resizable.pivotPosition);
      }
      else
      {
        AnchorObject();
      }

    }
  }

  public void ResizeObject()
  {
    sizeX = keepSizeX ? resizer.newSize.x : customSize.x;
    sizeY = keepSizeY ? resizer.newSize.y : customSize.y;
    sizeZ = keepSizeZ ? resizer.newSize.z : customSize.z;
    newSize = new Vector3(sizeX, sizeY, sizeZ);
  }

  public void AnchorObject()
  {
    switch (anchorX)
    {
      case Anchoring.Sides:
        posX = resizer.newSize.x * anchorXAmount;
        break;
      case Anchoring.Zero:
        posX = 0;
        break;
      case Anchoring.Custom:
        posX = customPosition.x;
        break;
    }

    switch (anchorY)
    {
      case Anchoring.Sides:
        posY = resizer.newSize.y * anchorYAmount;
        break;
      case Anchoring.Zero:
        posY = 0;
        break;
      case Anchoring.Custom:
        posY = customPosition.y;
        break;
    }

    switch (anchorZ)
    {
      case Anchoring.Sides:
        posZ = resizer.newSize.z * anchorZAmount;
        break;
      case Anchoring.Zero:
        posZ = 0;
        break;
      case Anchoring.Custom:
        posZ = customPosition.z;
        break;
    }

    transform.localPosition = new Vector3(posX, posY, posZ);
  }

  public void CalculatePadding(PaddingReference[] paddingReference)
  {
    if (paddingReference.Length > 0)
    {
      foreach (PaddingReference paddingRef in paddingReference)
      {
        if (paddingRef.reference)
        {
          Resizable refResizable = paddingRef.reference.GetComponent<Resizable>();
          Vector3 refScale = refResizable.newSize;
          Vector3 refPos = paddingRef.reference.transform.localPosition;
          float padding = 0;
          float toPaddingX = transform.localPosition.x;
          float toPaddingY = transform.localPosition.y;
          float toPaddingZ = transform.localPosition.z;

          switch (paddingRef.from)
          {
            case TransformFrom.ScaleX:
              padding = refScale.x;
              break;
            case TransformFrom.ScaleY:
              padding = refScale.y;
              break;
            case TransformFrom.ScaleZ:
              padding = refScale.z;
              break;
          }

          padding *= paddingRef.amount;

          switch (paddingRef.to)
          {
            case TransformTo.PaddingLeft:
              newSize.x -= padding;
              toPaddingX = transform.localPosition.x + padding * 0.5f;
              break;
            case TransformTo.PaddingRight:
              newSize.x -= padding;
              toPaddingX = transform.localPosition.x - padding * 0.5f;
              break;
            case TransformTo.PaddingBottom:
              newSize.y -= padding;
              toPaddingY = transform.localPosition.y - padding * 0.5f;
              break;
            case TransformTo.PaddingTop:
              newSize.y -= padding;
              toPaddingY = transform.localPosition.y + padding * 0.5f;
              break;
            case TransformTo.PaddingFront:
              newSize.z -= padding;
              toPaddingZ = transform.localPosition.z + padding * 0.5f;
              break;
            case TransformTo.PaddingBack:
              newSize.z -= padding;
              toPaddingZ = transform.localPosition.z - padding * 0.5f;
              break;
            case TransformTo.ScaleX:
              newSize.x = refScale.x * paddingRef.amount;
              if (paddingRef.addDefaultSize)
                newSize.x += defaultSize.x;
              break;
            case TransformTo.ScaleY:
              newSize.y = refScale.y * paddingRef.amount;
              if (paddingRef.addDefaultSize)
                newSize.y += defaultSize.y;
              break;
            case TransformTo.ScaleZ:
              newSize.z = refScale.z * paddingRef.amount;
              if (paddingRef.addDefaultSize)
                newSize.z += defaultSize.z;
              break;
          }

          if (paddingRef.move)
            transform.localPosition = new Vector3(toPaddingX, toPaddingY, toPaddingZ);
          resizable.newSize = newSize;
        }
      }
    }
  }
}
