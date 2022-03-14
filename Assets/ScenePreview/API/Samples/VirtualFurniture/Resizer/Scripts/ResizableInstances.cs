using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ResizableInstances : MonoBehaviour
{
  private Resizer resizer;
  private Resizable resizable;
  private Vector3 sizeWithPadding;
  private Vector3 instanceSize;
  private Vector3 finalSize;
  private Vector3 instanceOffset;
  private Vector3 containerOffset;

  public Vector3 newSize;
  public GameObject elementToSpawn;

  [Header("INSTANCES")]
  public int instancesX = 1;
  [Range(0.0f, 0.5f)]
  public float spacingX = 0.005f;
  public float customSizeX;
  public bool spawnOnPaddingX;
  public float fixedDistanceX;

  [Space(10)]
  public int instancesY = 1;
  [Range(0.0f, 0.5f)]
  public float spacingY = 0.005f;
  public float customSizeY;
  public bool spawnOnPaddingY;
  public float fixedDistanceY;

  [Space(10)]
  public int instancesZ = 1;
  [Range(0.0f, 0.5f)]
  public float spacingZ = 0.005f;
  public float customSizeZ;
  public bool spawnOnPaddingZ;
  public float fixedDistanceZ;

  private int configuredInstancesX = 0;
  private int configuredInstancesY = 0;
  private int configuredInstancesZ = 0;

  [Header("PADDING")]
  [Range(-1, 1)] public float paddingLeft;
  [Range(-1, 1)] public float paddingRight;
  [Range(-1, 1)] public float paddingTop;
  [Range(-1, 1)] public float paddingBottom;
  [Range(-1, 1)] public float paddingFront;
  [Range(-1, 1)] public float paddingBack;

  WaitForSeconds startDelay = new WaitForSeconds(0.05f);

  // Setup an event for when the instances numbers are updated from the inspector
  public delegate void LayoutUpdated();
  public LayoutUpdated layoutUpdatedDelegate;

  private void OnValidate()
  {
    if (Application.isPlaying)
    {
      UpdateLayout();
    }
  }

  private void Start()
  {
    StartCoroutine(DelayedStart());
    resizer = SearchForResizer(transform);
    Debug.Assert(resizer != null, "MyHome: Furniture resizer initialized incorrectly");

    resizable = GetComponent<Resizable>();
    paddingLeft *= resizable.meshScale;
    paddingRight *= resizable.meshScale;
    paddingTop *= resizable.meshScale;
    paddingBottom *= resizable.meshScale;
    paddingFront *= resizable.meshScale;
    paddingBack *= resizable.meshScale;
  }

  private void Update()
  {
    // check if the size changed
    if (newSize != resizable.newSize)
    {
      newSize = resizable.newSize;
      UpdateLayout();
    }
  }

  public IEnumerator DelayedStart()
  {
    yield return startDelay;
    UpdateLayout();
  }

  private Transform[] elementInstances;

  public Transform GetElement(int x, int y, int z)
  {
    if (x < 0 || x >= instancesX
      || y < 0 || y >= instancesY
      || z < 0 || z >= instancesZ
      || elementInstances.Length != instancesX * instancesY * instancesZ)
    {
      Debug.LogWarning("Requested element is out of bounds");
      return null;
    }
    return elementInstances[x + y * instancesX + z * instancesX * instancesY];
  }

  // Instantiates the elements
  public void InstatiateElements()
  {
    if (elementToSpawn == null)
      return;

    elementInstances = new Transform[instancesX * instancesY * instancesZ];

    for (int z = 0; z < instancesZ; z++)
    {
      for (int y = 0; y < instancesY; y++)
      {
        for (int x = 0; x < instancesX; x++)
        {
          var g = Instantiate(elementToSpawn, this.transform);
          elementInstances[x + y * instancesX + z * instancesX * instancesY] = g.transform;
        }
      }
    }
  }

  public void Clear()
  {
    if (elementInstances == null)
      return;
    for (int i = 0; i < elementInstances.Length; i++)
    {
      Destroy(elementInstances[i].gameObject);
      elementInstances[i] = null;
    }
  }

  public void UpdateLayout()
  {
    // avoid some start condition problems
    if (resizable == null)
    {
      return;
    }

    newSize = resizable.newSize;

    // take the container size and remove the given padding
    sizeWithPadding.x = newSize.x - paddingLeft - paddingRight;
    sizeWithPadding.y = newSize.y - paddingBottom - paddingTop;
    sizeWithPadding.z = newSize.z - paddingFront - paddingBack;

    // set number of instances by custom int or by a minimum fixed distance
    instancesX = fixedDistanceX > 0
      ? Mathf.FloorToInt(sizeWithPadding.x / fixedDistanceX) : instancesX;
    instancesY = fixedDistanceY > 0
      ? Mathf.FloorToInt(sizeWithPadding.y / fixedDistanceY) : instancesY;
    instancesZ = fixedDistanceZ > 0
      ? Mathf.FloorToInt(sizeWithPadding.z / fixedDistanceZ) : instancesZ;
    instancesX = Mathf.Clamp(instancesX, 1, 30);
    instancesY = Mathf.Clamp(instancesY, 1, 30);
    instancesZ = Mathf.Clamp(instancesZ, 1, 30);

    if (elementInstances == null
      || elementInstances.Length != instancesX * instancesY * instancesZ
      || configuredInstancesX != instancesX
      || configuredInstancesY != instancesY
      || configuredInstancesZ != instancesZ)
    {
      Clear();
      InstatiateElements();
    }

    finalSize = CalculateInstanceSize();

    for (int z = 0; z < instancesZ; z++)
    {
      for (int y = 0; y < instancesY; y++)
      {
        for (int x = 0; x < instancesX; x++)
        {
          int index = x + y * instancesX + z * instancesX * instancesY;
          elementInstances[index].gameObject.SetActive(true);
          instanceOffset = CalculateInstanceOffset(x, y, z);
          elementInstances[index].localPosition = instanceOffset;
          elementInstances[index].localRotation = Quaternion.identity;
          // adapt the instance by using its Resizable settings
          GameObject instanceObj = elementInstances[index].gameObject;
          Resizable resizable = instanceObj.GetComponent<Resizable>();
          resizable.newSize = finalSize;
          resizer.ApplyMesh(resizable, instanceObj, finalSize, Vector3.zero);
        }
      }
    }
    elementToSpawn.SetActive(false);
    if (spawnOnPaddingX || spawnOnPaddingY || spawnOnPaddingZ)
      elementInstances[0].gameObject.SetActive(false);
    configuredInstancesX = instancesX;
    configuredInstancesY = instancesY;
    configuredInstancesZ = instancesZ;
    layoutUpdatedDelegate?.Invoke();
  }

  private Vector3 CalculateInstanceSize()
  {
    // set the single instance size based on the number of instance + padding
    instanceSize.x = instancesX > 1 ? (sizeWithPadding.x - (instancesX - 1)
      * spacingX) / (float)instancesX : sizeWithPadding.x;
    instanceSize.y = instancesY > 1 ? (sizeWithPadding.y - (instancesY - 1)
      * spacingY) / (float)instancesY : sizeWithPadding.y;
    instanceSize.z = instancesZ > 1 ? (sizeWithPadding.z - (instancesZ - 1)
      * spacingZ) / (float)instancesZ : sizeWithPadding.z;
    // set the size to a custom one if the custom value is > 0
    finalSize.x = (customSizeX > 0) ? customSizeX : instanceSize.x;
    finalSize.y = (customSizeY > 0) ? customSizeY : instanceSize.y;
    finalSize.z = (customSizeZ > 0) ? customSizeZ : instanceSize.z;

    return finalSize;
  }

  private Vector3 CalculateInstanceOffset(int x, int y, int z)
  {
    // calculate container new center based on padding and pivot position
    containerOffset.x = paddingLeft * 0.5f - paddingRight * 0.5f
      - (newSize.x * resizable.pivotPosition.x) * 0.5f;
    containerOffset.y = paddingBottom * 0.5f - paddingTop * 0.5f
      - (newSize.y * resizable.pivotPosition.y) * 0.5f;
    containerOffset.z = paddingFront * 0.5f - paddingBack * 0.5f
      - (newSize.z * resizable.pivotPosition.z) * 0.5f;

    Vector3 offset = Vector3.zero;

    // get the center of the instance based on the container bounding box
    offset.x = -sizeWithPadding.x * 0.5f + x * spacingX + x * instanceSize.x;
    offset.y = -sizeWithPadding.y * 0.5f + y * spacingY + y * instanceSize.y;
    offset.z = -sizeWithPadding.z * 0.5f + z * spacingZ + z * instanceSize.z;
    offset.x += spawnOnPaddingX ? 0 : instanceSize.x * 0.5f;
    offset.y += spawnOnPaddingY ? 0 : instanceSize.y * 0.5f;
    offset.z += spawnOnPaddingZ ? 0 : instanceSize.z * 0.5f;

    offset.x = instancesX > 1 ? offset.x : 0;
    offset.y = instancesY > 1 ? offset.y : 0;
    offset.z = instancesZ > 1 ? offset.z : 0;

    offset.x += containerOffset.x;
    offset.y += containerOffset.y;
    offset.z += containerOffset.z;

    return offset;
  }

  // climb the hierarchy to find the first Resizer script
  // we do this because the furniture could be a child of many other objects in the scene
  private Resizer SearchForResizer(Transform startTransform)
  {
    if (startTransform.parent == null)
    {
      Debug.Log("MyHome: Furniture doesn't have resizer component");
      return null;
    }
    else if (startTransform.parent.GetComponent<Resizer>())
    {
      return startTransform.parent.GetComponent<Resizer>();
    }
    else
    {
      return SearchForResizer(startTransform.parent);
    }
  }

#if (UNITY_EDITOR)
  private void OnDrawGizmosSelected()
  {
    Gizmos.matrix = this.transform.localToWorldMatrix;
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireCube(containerOffset, sizeWithPadding);
    Gizmos.color = Color.black;

    CalculateInstanceSize();
    for (int z = 0; z < instancesZ; z++)
    {
      for (int y = 0; y < instancesY; y++)
      {
        for (int x = 0; x < instancesX; x++)
        {
          instanceOffset = CalculateInstanceOffset(x, y, z);
          Gizmos.DrawWireCube(instanceOffset, instanceSize);
        }
      }
    }
  }
#endif
}
