/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

public class SemanticClassificationHelper
{
  /// <summary>
  /// Labels we can expect for Plane Anchors used for the boundaries of the room
  /// </summary>
  public static string[] roomBoundaryLabels = {
    "FLOOR",
    "CEILING",
    "WALL_FACE",
  };

  /// <summary>
  /// Labels we can expect for generic planes within the room
  /// </summary>
  public static string[] planeLabels = {
    "GENERIC",
    "DESK",
    "TABLETOP",
    "COUCH",
    "WHITEBOARD",
    "DOOR_FRAME",
    "WINDOW_FRAME",
    "WALL_ART",
  };

  /// <summary>
  /// Labels we can expect for volumes within the room
  /// </summary>
  public static string[] volumeLabels = {
    "GENERIC",
    "TABLE",
    "CABINET",
    "SHELF",
    "WARDROBE",
    "MONITOR",
    "TV",
    "LAMP",
    "PLANT",
  };

  /// <summary>
  /// Returns true if the input label is present in the input classification
  /// </summary>
  /// <param name="label">The label we are searching</param>
  /// <param name="classification">The classification to search within</param>
  /// <returns></returns>
  public static bool HasLabel(string label, OVRSceneObject.SemanticClassification classification)
  {
    foreach (string label_a in classification.labels)
    {
      if (label_a == "")
      {
        continue;
      }
      if (label == label_a)
      {
        return true;
      }
    }
    return false;
  }

  /// <summary>
  /// Returns true if the two classification have at least one common label
  /// </summary>
  /// <param name="a">The first classification</param>
  /// <param name="b">The second classification</param>
  /// <returns></returns>
  public static bool HaveCommonLabel(
    OVRSceneObject.SemanticClassification a,
    OVRSceneObject.SemanticClassification b)
  {
    foreach (string label_a in a.labels)
    {
      if (label_a == "")
      {
        continue;
      }
      foreach (string label_b in b.labels)
      {
        if (label_b == "")
        {
          continue;
        }
        if (label_a.Equals(label_b))
        {
          return true;
        }
      }
    }
    return false;
  }
}
