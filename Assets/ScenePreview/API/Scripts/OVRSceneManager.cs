/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/
using System;
using System.Reflection;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class OVRSceneManager : MonoBehaviour
{
  [Tooltip("A prefab that will be used to instantiate any Plane found when querying the Scene model")]
  public OVRSceneObject planePrefab;

  [Tooltip("A prefab that will be used to instantiate any Volume found when querying the Scene model")]
  public OVRSceneObject volumePrefab;

  #region Private Vars

  public struct GOSpatialEntity
  {
    public string Name;
    public string UuidString;
    public GameObject GO;
  }

  public enum QueryMode
  {
    QueryAllAnchors,                                // Get entire Scene
    QueryByUuid,                                    // Get a specific spatial entity
    QueryAllBounded2DEnabled,                       // Get all planar entities
    QueryAllRoomLayoutEnabledForAllEntitiesInside,  // Get Ceiling/Floor/Walls + other spatial entities in Spatial Entity Container.
    QueryAllRoomLayoutEnabledForRoomBox,            // Get Ceiling/Floor/Walls only.
  }

  // Maintain Scene Model Anchors currently loaded on the app space.
  private Dictionary<UInt64, GOSpatialEntity> sceneSpatialEntities;

  // Maintain UUIDs to be used.
  private List<string> uuidsToQuery;

  private QueryMode currentQueryMode = QueryMode.QueryAllAnchors;

  private static readonly Dictionary<string, Color> semanticColors = new Dictionary<string, Color> {
        {"DESK", new Color(1.0f, 0, 0, 0.2f)},
        {"TABLE", new Color(1.0f, 0.2f, 0, 0.2f)},
        {"TABLETOP", new Color(1.0f, 0.2f, 0, 0.2f)},
        {"COUCH", new Color(0, 1.0f, 0, 0.2f)},
        {"WHITEBOARD", new Color(1.0f, 1.0f, 0.8f, 0.2f)},
        {"FLOOR", new Color(0.2f, 0.2f, 0.8f, 1.0f)},
        {"CEILING", new Color(0.3f, 0.3f, 0.3f, 1.0f)},
        {"WALL_FACE", new Color(0.5f, 0.5f, 0, 1.0f)},
        {"DOOR_FRAME", new Color(0.3f, 0.3f, 0, 0.2f)},
        {"WINDOW_FRAME", new Color(0.3f, 0.3f, 0, 0.2f)},
        {"WALL_ART", new Color(1.0f, 0.3f, 0, 0.2f)},
        {"CABINET", new Color(1.0f, 0.3f, 0, 0.2f)},
        {"SHELF", new Color(1.0f, 1.0f, 0.8f, 0.2f)},
        {"WARDROBE", new Color(1.0f, 1.0f, 0.8f, 0.2f)},
        {"TV", new Color(1.0f, 1.0f, 0.8f, 0.2f)},
        {"LAMP", new Color(1.0f, 1.0f, 0.8f, 0.2f)},
        {"PLANT", new Color(0, 1.0f, 0.0f, 0.2f)},
        {"OTHER", new Color(1.0f, 0, 1.0f, 0.2f)},
    };

  #endregion

  /// <summary>
  /// Gets the singleton instance.
  /// </summary>
  public static OVRSceneManager instance { get; private set; }

  public void Awake()
  {
    // Only allow one instance at runtime.
    if (instance != null)
    {
      enabled = false;
      DestroyImmediate(this);
      return;
    }

    instance = this;
  }

  private void OnEnable()
  {
#if OVR_SCENE_PREVIEW
    // Bind events
    OVRManager.SpatialEntityQueryComplete += OVRManager_SpatialEntityQueryComplete;
    OVRManager.SpatialEntityQueryResults += OVRManager_SpatialEntityQueryResults;
    OVRManager.SpatialEntitySetComponentEnabled += OVRManager_SpatialEntitySetComponentEnabled;
    OVRManager.SceneCaptureComplete += OVRManager_SceneCaptureComplete;
#endif
  }

  private void OnDisable()
  {
#if OVR_SCENE_PREVIEW
    // Unbind events
    //unbind functions
    OVRManager.SpatialEntityQueryComplete -= OVRManager_SpatialEntityQueryComplete;
    OVRManager.SpatialEntityQueryResults -= OVRManager_SpatialEntityQueryResults;
    OVRManager.SpatialEntitySetComponentEnabled -= OVRManager_SpatialEntitySetComponentEnabled;
    OVRManager.SceneCaptureComplete -= OVRManager_SceneCaptureComplete;
#endif
  }

  void Start()
  {
#if UNITY_EDITOR
    Debug.LogWarning("Scene API does not work in Editor. Make a build and deploy it to Quest.");
    return;
#else
    sceneSpatialEntities = new Dictionary<UInt64, GOSpatialEntity>();
    LoadSceneModel();
#endif
  }

  /// <summary>
  /// When running in Editor, emulates the real behavior by lading the Scene Model from disk.
  /// When running on Quest, uses the actual Scene API backend.
  /// </summary>
  /// <returns> Return true if the query was successfully registered</returns>
  public bool LoadSceneModel()
  {
    currentQueryMode = QueryMode.QueryAllRoomLayoutEnabledForAllEntitiesInside;
    return LoadSpatialEntities(out sceneLoadRequestId);
  }

#if OVR_SCENE_PREVIEW

  #region Helpers
  // We use this to store the request id when attempting to load the scene
  UInt64 sceneLoadRequestId = UInt64.MaxValue;
  UInt64 captureFlowId = UInt64.MaxValue;

  #endregion

  #region Internal Methods
  private bool LoadSpatialEntities(out UInt64 requestId)
  {
    // Remove all the spatial entities on memory. Update with spatial entities from new query.
    foreach (var kv in sceneSpatialEntities)
    {
      Destroy(kv.Value.GO);
    }
    sceneSpatialEntities.Clear();

    var queryInfo = new OVRPlugin.SpatialEntityQueryInfo2()
    {
      QueryType = OVRPlugin.SpatialEntityQueryType.Action,
      MaxQuerySpaces = 30,
      Timeout = 0,
      Location = OVRPlugin.SpatialEntityStorageLocation.Local,
      ActionType = OVRPlugin.SpatialEntityQueryActionType.Load,
      FilterType = OVRPlugin.SpatialEntityQueryFilterType.None
    };

    if (currentQueryMode == QueryMode.QueryByUuid)
    {
      queryInfo.FilterType = OVRPlugin.SpatialEntityQueryFilterType.Ids;
      queryInfo.IdInfo = new OVRPlugin.SpatialEntityFilterInfoIds
      {
        NumIds = Math.Min(OVRPlugin.SpatialEntityFilterInfoIdsMaxSize, uuidsToQuery.Count),
        Ids = new OVRPlugin.SpatialEntityUuid[OVRPlugin.SpatialEntityFilterInfoIdsMaxSize]
      };
      for (int i = 0; i < queryInfo.IdInfo.NumIds; ++i)
      {
        queryInfo.IdInfo.Ids[i] = StringToUuid(uuidsToQuery[i]);
        Debug.Log($"[OVRSceneManager] {MethodBase.GetCurrentMethod().Name} UUID to query [{uuidsToQuery[i]}]");
      }
    }
    else if (currentQueryMode == QueryMode.QueryAllRoomLayoutEnabledForAllEntitiesInside || currentQueryMode == QueryMode.QueryAllBounded2DEnabled || currentQueryMode == QueryMode.QueryAllRoomLayoutEnabledForRoomBox)
    {
      queryInfo.FilterType = OVRPlugin.SpatialEntityQueryFilterType.Components;
      queryInfo.ComponentsInfo = new OVRPlugin.SpatialEntityFilterInfoComponents
      {
        Components = new OVRPlugin.SpatialEntityComponentType[OVRPlugin.SpatialEntityFilterInfoComponentsMaxSize],
        NumComponents = 1,
      };
      if (currentQueryMode == QueryMode.QueryAllRoomLayoutEnabledForAllEntitiesInside || currentQueryMode == QueryMode.QueryAllRoomLayoutEnabledForRoomBox)
      {
        queryInfo.ComponentsInfo.Components[0] = OVRPlugin.SpatialEntityComponentType.RoomLayout;
      }
      else
      {
        queryInfo.ComponentsInfo.Components[0] = OVRPlugin.SpatialEntityComponentType.Bounded2D;
      }
    }

    requestId = UInt64.MinValue;
    return OVRPlugin.SpatialEntityQuerySpatialEntity2(queryInfo, ref requestId);
  }

  private bool RequestCaptureFlow()
  {
    var requestString = "";
    return OVRPlugin.RequestSceneCapture(ref requestString, ref captureFlowId);
  }

  private string UuidToString(OVRPlugin.SpatialEntityUuid uuid)
  {
    return uuid.Value_0.ToString() + "-" + uuid.Value_1.ToString();
  }

  private OVRPlugin.SpatialEntityUuid StringToUuid(string uuidString)
  {
    var uuid = new OVRPlugin.SpatialEntityUuid()
    {
      Value_0 = 0,
      Value_1 = 0
    };

    int found = uuidString.IndexOf("-");
    if (found < 0 || found != uuidString.LastIndexOf("-"))
    {
      Debug.Log($"[OVRSceneManager] [{uuidString}] is not a valid UUID");
      return uuid;
    }

    uuid.Value_0 = UInt64.Parse(uuidString.Substring(0, found));
    uuid.Value_1 = UInt64.Parse(uuidString.Substring(found + 1));

    return uuid;
  }

  private void EnableComponentIfNecessary(UInt64 space, OVRPlugin.SpatialEntityComponentType componentType)
  {
    OVRPlugin.SpatialEntityGetComponentEnabled(ref space, componentType, out bool enabled, out _);
    if (enabled)
    {
      Debug.Log($"[OVRSceneManager] {MethodBase.GetCurrentMethod().Name} component [{componentType}] is already enabled for space [{space}]");
      return;
    }

    double dTimeout = 10 * 1000f;
    UInt64 requestId = UInt64.MinValue;
    OVRPlugin.SpatialEntitySetComponentEnabled(ref space, componentType, true, dTimeout, ref requestId);
    Debug.Log($"[OVRSceneManager] {MethodBase.GetCurrentMethod().Name} component [{componentType}] requested for space [{space}] with requestId [{requestId}]");
  }

  private bool IsValid(OVRPlugin.SpatialEntityUuid uuid)
  {
    return uuid.Value_0 != 0 || uuid.Value_1 != 0;
  }
  #endregion

  #region ActionFunctions

  private void OVRManager_SpatialEntitySetComponentEnabled(UInt64 requestId, bool result, OVRPlugin.SpatialEntityComponentType componentType, UInt64 space)
  {
    Debug.Log($"[OVRSceneManager] {MethodBase.GetCurrentMethod().Name} requestId: [{requestId}] result: [{result}] componentType: [{componentType}] space: [{space}]");
  }

  private void OVRManager_SceneCaptureComplete(UInt64 requestId, bool result)
  {
    Debug.Log($"[OVRSceneManager] {MethodBase.GetCurrentMethod().Name} requestId: [{requestId}] result: [{result}]");
    if (result)
    {
      Debug.Log("[OVRSceneManager] User returned from Capture Flow. Attempting to Load the Scene Model");
      LoadSceneModel();
    }
    else
    {
      Debug.LogError("[OVRSceneManager] An error occurred when sending the user to the capture flow. The Scene" +
        " Model will not load.");
    }
  }

  private void OVRManager_SpatialEntityQueryResults(UInt64 requestId, int numResults, OVRPlugin.SpatialEntityQueryResult[] results)
  {
    Debug.Log($"[OVRSceneManager] {MethodBase.GetCurrentMethod().Name} requestId: [{requestId}] numResults: [{numResults}]");

    // When running in editor, we use the backend, and emulate these events
    for (int i = 0; i < numResults; i++)
    {
      UInt64 space = results[i].space;
      OVRPlugin.SpatialEntityUuid uuid = results[i].uuid;

      // Enable Storable and Locatable components, as they are not enabled when the space is loaded from the storage for the first time.
      EnableComponentIfNecessary(space, OVRPlugin.SpatialEntityComponentType.Storable);
      EnableComponentIfNecessary(space, OVRPlugin.SpatialEntityComponentType.Locatable);


      OVRPlugin.SpatialEntityGetComponentEnabled(ref space, OVRPlugin.SpatialEntityComponentType.Bounded2D, out bool bounded2dEnabled, out _);
      OVRPlugin.SpatialEntityGetComponentEnabled(ref space, OVRPlugin.SpatialEntityComponentType.RoomLayout, out bool roomLayoutEnabled, out _);
      if (bounded2dEnabled && roomLayoutEnabled)
      {
        Debug.LogError($"{MethodBase.GetCurrentMethod().Name} space: [{space}] both Bounded2D and RoomLayout enabled!");
        return;
      }
      if (bounded2dEnabled)
      {
        Debug.Log($"[OVRSceneManager] {MethodBase.GetCurrentMethod().Name} space: [{space}] Bounded2D enabled");

        GameObject planeObject = Instantiate(planePrefab.gameObject, Vector3.zero, Quaternion.identity);

        OVRSceneObject sceneObject = planeObject.GetComponent<OVRSceneObject>();

        // Set the positions/rotation and ensure it does not drift over time
        planeObject.AddComponent<OVRSpatialAnchor>();
        planeObject.GetComponent<OVRSpatialAnchor>().Handle = space;
        planeObject.GetComponent<OVRSpatialAnchor>().SetEnable = true;

        // Let's make sure the transform is updated now
        planeObject.GetComponent<OVRSpatialAnchor>().UpdateTransform();

        // Set the dimensions
        if (OVRPlugin.SpatialEntityGetBounded2D(ref space, out OVRPlugin.Rectf rect2f))
        {
          sceneObject.dimensions = new Vector3(rect2f.Size.w, rect2f.Size.h, 1);
        }
        else
        {
          Debug.LogError($"[OVRSceneManager] {MethodBase.GetCurrentMethod().Name} failed to retrieve Bounded2D data");
        }
        // Set the labels
        if (OVRPlugin.SpatialEntityGetSemanticLabels(ref space, out string labelString))
        {
          string[] labels = labelString.Split(',');
          for (int j = 0; j < labels.Length && j < sceneObject.classification.labels.Length; j++)
          {
            if (labels[j] == "FLOOR")
            {
              OVRSpatialAnchor.floorAnchor = planeObject.GetComponent<OVRSpatialAnchor>();
            }
            sceneObject.classification.labels[j] = labels[j];
          }
        }
        else
        {
          Debug.LogError($"[OVRSceneManager] {MethodBase.GetCurrentMethod().Name} failed to retrieve Semantic Label data");
        }
        sceneSpatialEntities[space] = new GOSpatialEntity() { GO = planeObject, Name = "", UuidString = UuidToString(uuid) };
      }
      else if (roomLayoutEnabled)
      {
        bool hasValidCeiling = false;
        bool hasValidFloor = false;
        bool hasValidWalls = false;

        Debug.Log($"[OVRSceneManager] {MethodBase.GetCurrentMethod().Name} space: [{space}] RoomLayout enabled");
        bool roomLayoutSuccess = OVRPlugin.SpatialEntityGetRoomLayout(ref space, out OVRPlugin.RoomLayout roomLayout);
        Debug.Log($"[OVRSceneManager] SpatialEntityGetRoomLayout success [{roomLayoutSuccess}]");
        if (roomLayoutSuccess)
        {
          var uuidSet = new HashSet<string>();

          Debug.Log($"[OVRSceneManager] SpatialEntityGetRoomLayout: floor [{UuidToString(roomLayout.uuidFloor)}]");
          if (IsValid(roomLayout.uuidFloor))
          {
            uuidSet.Add(UuidToString(roomLayout.uuidFloor));
            hasValidFloor = true;
          }

          Debug.Log($"[OVRSceneManager] SpatialEntityGetRoomLayout: ceiling [{UuidToString(roomLayout.uuidCeiling)}]");
          if (IsValid(roomLayout.uuidCeiling))
          {
            uuidSet.Add(UuidToString(roomLayout.uuidCeiling));
            hasValidCeiling = true;
          }

          int validWallsCount = 0;
          Debug.Log($"[OVRSceneManager] SpatialEntityGetRoomLayout: wall count [{roomLayout.uuidWalls.Length}]");
          foreach (var wallUuid in roomLayout.uuidWalls)
          {
            Debug.Log($"[OVRSceneManager] SpatialEntityGetRoomLayout: wall [{UuidToString(wallUuid)}]");
            if (IsValid(wallUuid))
            {
              uuidSet.Add(UuidToString(wallUuid));
              validWallsCount++;
            }
          }
          hasValidWalls = validWallsCount == roomLayout.uuidWalls.Length && validWallsCount >= 3;

          bool roomIsValid = hasValidCeiling && hasValidFloor && hasValidWalls;
          if (roomIsValid)
          {
            bool containerSuccess = OVRPlugin.SpatialEntityGetContainer(ref space, out OVRPlugin.SpatialEntityUuid[] containerUuids);
            Debug.Log($"[OVRSceneManager] SpatialEntityGetContainer: success [{containerSuccess}], count [{containerUuids.Length}]");
            if (containerSuccess)
            {
              foreach (var containerUuid in containerUuids)
              {
                Debug.Log($"[OVRSceneManager] SpatialEntityGetContainer: UUID [{UuidToString(containerUuid)}]");
                if (IsValid(containerUuid))
                {
                  uuidSet.Add(UuidToString(containerUuid));
                }
              }
            }

            uuidsToQuery = uuidSet.ToList<string>();
            currentQueryMode = QueryMode.QueryByUuid;
            UInt64 id;
            LoadSpatialEntities(out id);
          }
          else
          {
            Debug.Log("[OVRSceneManager] Invalid Scene found in the device. Requesting Capture Flow.");
            RequestCaptureFlow();
          }
        }
      }
    }
  }

  private void OVRManager_SpatialEntityQueryComplete(UInt64 requestId, bool result, int numFound)
  {
    Debug.Log($"[OVRSceneManager] {MethodBase.GetCurrentMethod().Name} requestId: [{requestId}] result: [{result}] numFound: [{numFound}]");

    if (requestId == sceneLoadRequestId && result && numFound == 0)
    {
      Debug.Log("[OVRSceneManager] No Scene found in the device. Requesting Capture Flow.");
      RequestCaptureFlow();
    }
  }
  #endregion

#endif
}
