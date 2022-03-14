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
using UnityEditor;

[CustomEditor(typeof(OVRBody))]
public class OVRBodyEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawPropertiesExcluding(serializedObject, new string[] { "_bodyConfig" });
		SerializedProperty configProp = serializedObject.FindProperty("_bodyConfig");
		configProp.intValue = (int)(OVRBody.BodyConfigFlags)EditorGUILayout.EnumFlagsField("Configuration", (OVRBody.BodyConfigFlags)configProp.enumValueIndex);

		serializedObject.ApplyModifiedProperties();
	}
}
#endif
