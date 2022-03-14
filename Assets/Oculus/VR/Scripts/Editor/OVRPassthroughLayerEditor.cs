/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using UnityEditor;
using UnityEngine;

using ColorMapEditorType = OVRPassthroughLayer.ColorMapEditorType;

[CustomEditor(typeof(OVRPassthroughLayer))]
public class OVRPassthroughLayerEditor : Editor
{
	public override void OnInspectorGUI()
	{
		OVRPassthroughLayer layer = (OVRPassthroughLayer)target;

		layer.projectionSurfaceType = (OVRPassthroughLayer.ProjectionSurfaceType)EditorGUILayout.EnumPopup(
			new GUIContent("Projection Surface", "The type of projection surface for this Passthrough layer"),
			layer.projectionSurfaceType);

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Compositing", EditorStyles.boldLabel);
		layer.overlayType = (OVROverlay.OverlayType)EditorGUILayout.EnumPopup(new GUIContent("Placement", "Whether this overlay should layer behind the scene or in front of it"), layer.overlayType);
		layer.compositionDepth = EditorGUILayout.IntField(new GUIContent("Composition Depth", "Depth value used to sort layers in the scene, smaller value appears in front"), layer.compositionDepth);
#if OVR_INTERNAL_CODE
		layer.overrideBlendFactors = EditorGUILayout.Toggle(new GUIContent("Customize blend mode", "Explicitly specify the blend factors used to composite the overlay."), layer.overrideBlendFactors);
		if (layer.overrideBlendFactors)
		{
			layer.srcBlendFactor = (OVRPlugin.BlendFactor)EditorGUILayout.EnumPopup(new GUIContent("Src Factor"), layer.srcBlendFactor);
			layer.dstBlendFactor = (OVRPlugin.BlendFactor)EditorGUILayout.EnumPopup(new GUIContent("Dst Factor"), layer.dstBlendFactor);
		}
#endif

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Style", EditorStyles.boldLabel);

		layer.textureOpacity = EditorGUILayout.Slider("Opacity", layer.textureOpacity, 0, 1);

		EditorGUILayout.Space();

		layer.edgeRenderingEnabled = EditorGUILayout.Toggle(
			new GUIContent("Edge Rendering", "Highlight salient edges in the camera images in a specific color"),
			layer.edgeRenderingEnabled);
		layer.edgeColor = EditorGUILayout.ColorField("Edge Color", layer.edgeColor);

		EditorGUILayout.Space();

		System.Func<System.Enum, bool> hideCustomColorMapOption = option => (ColorMapEditorType)option != ColorMapEditorType.Custom;
		layer.colorMapEditorType = (ColorMapEditorType)EditorGUILayout.EnumPopup(
			new GUIContent("Color Map"),
			layer.colorMapEditorType,
			hideCustomColorMapOption,
			false);

		if (layer.colorMapEditorType == ColorMapEditorType.Controls)
		{
			layer.colorMapEditorContrast = EditorGUILayout.Slider("Contrast", layer.colorMapEditorContrast, -1, 1);
			layer.colorMapEditorBrightness = EditorGUILayout.Slider("Brightness", layer.colorMapEditorBrightness, -1, 1);
			layer.colorMapEditorPosterize = EditorGUILayout.Slider("Posterize", layer.colorMapEditorPosterize, 0, 1);
			layer.colorMapEditorGradient = EditorGUILayout.GradientField("Colorize", layer.colorMapEditorGradient);
		}

		if (GUI.changed)
		{
			EditorUtility.SetDirty(layer);
		}
	}
}
