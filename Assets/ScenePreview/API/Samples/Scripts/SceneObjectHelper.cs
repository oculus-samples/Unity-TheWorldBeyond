/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using UnityEngine;

public class SceneObjectHelper
{
  /// <summary>
  /// Given a cubic volume, returns the transform information for the top face
  /// </summary>
  /// <param name="volume">The transform of the cubic volume</param>
  /// <param name="dimensions">The dimensions, in the volume local space, of the cube</param>
  /// <param name="position">The position of the top plane</param>
  /// <param name="rotation">The rotation of the top plane</param>
  /// <param name="localScale">The local scale of the top plane</param>
  public static void GetTopPlaneFromVolume(
   Transform volume,
   Vector3 dimensions,
   out Vector3 position,
   out Quaternion rotation,
   out Vector3 localScale)
  {
    float halfHeight = dimensions.y / 2.0f;
    position = volume.position + Vector3.up * halfHeight;
#if UNITY_EDITOR
    // When loading Scene in editor, a temporary backend is used. This backend has
    // a different assumption about the orientation of planes and volumes, which is addressed
    // here. This will go away once we move to the final API.
    rotation = Quaternion.LookRotation(Vector3.up, volume.forward);
#else
    rotation = Quaternion.LookRotation(Vector3.up, -volume.forward);
#endif
    localScale = new Vector3(dimensions.x, dimensions.z, dimensions.y);
  }

  /// <summary>
  /// Given the top plane, returns the volume from that plane down to the floor
  /// </summary>
  /// <param name="plane">The transform of the plane</param>
  /// <param name="dimensions">The dimensions, in the local plane space, of the plane</param>
  /// <param name="position">The position of the volume</param>
  /// <param name="rotation">The rotation of the volume</param>
  /// <param name="localScale">The local scale of the volume</param>
  public static void GetVolumeFromTopPlane(
    Transform plane,
    Vector3 dimensions,
    out Vector3 position,
    out Quaternion rotation,
    out Vector3 localScale)
  {
    // We assume we can project the top plane to the ground
    float groundHeight = 0.0f;
    if (OVRSpatialAnchor.floorAnchor != null)
    {
      groundHeight = OVRSpatialAnchor.floorAnchor.transform.position.y;
    }
    float halfHeight = (plane.position.y - groundHeight) / 2.0f;
    position = plane.position - Vector3.up * halfHeight;
#if UNITY_EDITOR
    // When loading Scene in editor, a temporary backend is used. This backend has
    // a different assumption about the orientation of planes and volumes, which is addressed
    // here. This will go away once we move to the final API.
    rotation = Quaternion.LookRotation(plane.up, Vector3.up);
#else
    rotation = Quaternion.LookRotation(-plane.up, Vector3.up);
#endif
    localScale = new Vector3(dimensions.x, halfHeight * 2.0f, dimensions.y);
  }
}
