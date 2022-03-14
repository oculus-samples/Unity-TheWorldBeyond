/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

#if OVR_INTERNAL_CODE
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OVRControllerNoPredTest : MonoBehaviour
{
	public OVRPlugin.Node node;

	void Update()
	{
		OVRPlugin.PoseStatef poseState = OVRPlugin.GetNodePoseStateImmediate(node);
		OVRPose pose = poseState.Pose.ToOVRPose();
		transform.localPosition = pose.position;
		transform.localRotation = pose.orientation;
	}
}
#endif
