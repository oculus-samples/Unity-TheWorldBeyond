using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "ResizableObjectsList", menuName = "Resizable Objects List", order = 1)]
public class ResizableObjectsList : ScriptableObject
{
  [SerializeField]
  public OVRSceneObject.SemanticClassification classification;
  public List<FurniturePiece> objects;
  public Dictionary<string, FurniturePiece> book;
  [System.Serializable]
  public class FurniturePiece
  {
    public string objectName = "Object 1";
    public GameObject prefab;
  }

  public void PopulateBook()
  {
    if (book != null && book.Count > 0)
    {
      Debug.LogWarning("Book was already populated");
      book.Clear();
    }
    else
    {
      book = new Dictionary<string, FurniturePiece>();
    }
  }
}
