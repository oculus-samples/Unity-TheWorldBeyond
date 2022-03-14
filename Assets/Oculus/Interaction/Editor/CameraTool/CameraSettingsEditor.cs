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
using UnityEditor;

namespace Oculus.Interaction.CameraTool
{
    [CustomEditor(typeof(CameraSettings))]
    public class CameraSettingsEditor : UnityEditor.Editor
    {
        private class BaseProperties
        {
            private const string kScreenshotWidth = nameof(CameraSettings.BaseSettings.ScreenshotWidth);
            private const string kScreenshotHeight = nameof(CameraSettings.BaseSettings.ScreenshotHeight);
            private const string kThumbnailDownscale = nameof(CameraSettings.BaseSettings.ThumbnailDownscale);
            private const string kImageFormat = nameof(CameraSettings.BaseSettings.ImageFormat);
            private const string kFileName = nameof(CameraSettings.BaseSettings.FileName);
            private const string kPreferAsyncReadback = nameof(CameraSettings.BaseSettings.PreferAsyncReadback);

            public readonly SerializedProperty ScreenshotWidth;
            public readonly SerializedProperty ScreenshotHeight;
            public readonly SerializedProperty ThumbnailDownscale;
            public readonly SerializedProperty ImageFormat;
            public readonly SerializedProperty FileName;
            public readonly SerializedProperty PreferAsyncReadback;

            public BaseProperties(SerializedProperty baseSettings)
            {
                ScreenshotWidth = baseSettings.FindPropertyRelative(kScreenshotWidth);
                ScreenshotHeight = baseSettings.FindPropertyRelative(kScreenshotHeight);
                ThumbnailDownscale = baseSettings.FindPropertyRelative(kThumbnailDownscale);
                ImageFormat = baseSettings.FindPropertyRelative(kImageFormat);
                FileName = baseSettings.FindPropertyRelative(kFileName);
                PreferAsyncReadback = baseSettings.FindPropertyRelative(kPreferAsyncReadback);
            }
        }

        private class AndroidProperties : BaseProperties
        {
            public AndroidProperties(SerializedProperty settings) : base(settings) { }
        }

        private class StandaloneProperties : BaseProperties
        {
            public StandaloneProperties(SerializedProperty settings) : base(settings) { }
        }

        private AndroidProperties _androidProperties;
        private StandaloneProperties _standaloneProperties;

        public override void OnInspectorGUI()
        {
            BuildTargetGroup buildGroup = EditorGUILayout.BeginBuildTargetSelectionGrouping();

            if (buildGroup == BuildTargetGroup.Android)
            {
                if (_androidProperties == null)
                {
                    var _androidSettings = serializedObject.FindProperty("_androidSettings");
                    _androidProperties = new AndroidProperties(_androidSettings);
                }
                DrawBaseSettings(_androidProperties);
            }
            else if (buildGroup == BuildTargetGroup.Standalone)
            {
                if (_standaloneProperties == null)
                {
                    var _standaloneSettings = serializedObject.FindProperty("_standaloneSettings");
                    _standaloneProperties = new StandaloneProperties(_standaloneSettings);
                }
                DrawBaseSettings(_standaloneProperties);
            }

            EditorGUILayout.EndBuildTargetSelectionGrouping();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawBaseSettings(BaseProperties baseProps)
        {
            EditorGUILayout.PropertyField(baseProps.ScreenshotWidth);
            EditorGUILayout.PropertyField(baseProps.ScreenshotHeight);
            EditorGUILayout.PropertyField(baseProps.ThumbnailDownscale);
            EditorGUILayout.PropertyField(baseProps.ImageFormat);
            EditorGUILayout.PropertyField(baseProps.FileName);
            EditorGUILayout.PropertyField(baseProps.PreferAsyncReadback);

            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("Aspect Ratio", $"{GetAspectRatio(baseProps).ToString("#.##")}");
            EditorGUILayout.LabelField("Resolution", $"{GetResolution(baseProps).x}x{GetResolution(baseProps).y}");
            EditorGUILayout.LabelField("Thumbnail Size", $"{GetThumbnailSize(baseProps).x}x" +
                                                         $"{GetThumbnailSize(baseProps).y}");
            EditorGUILayout.EndVertical();
        }

        private float GetAspectRatio(BaseProperties props)
        {
            Vector2Int resolution = GetResolution(props);
            return (float)resolution.x / resolution.y;
        }

        private Vector2Int GetResolution(BaseProperties props)
        {
            return new Vector2Int(props.ScreenshotWidth.intValue,
                                  props.ScreenshotHeight.intValue);
        }

        private Vector2Int GetThumbnailSize(BaseProperties props)
        {
            Vector2Int resolution = GetResolution(props);
            resolution.x = Mathf.RoundToInt(resolution.x / props.ThumbnailDownscale.floatValue);
            resolution.x = Mathf.Max(resolution.x, 1);
            resolution.y = Mathf.RoundToInt(resolution.y / props.ThumbnailDownscale.floatValue);
            resolution.y = Mathf.Max(resolution.y, 1);
            return resolution;
        }
    }
}
