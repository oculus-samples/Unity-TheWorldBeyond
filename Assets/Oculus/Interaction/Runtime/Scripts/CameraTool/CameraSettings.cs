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
using System.Text.RegularExpressions;

namespace Oculus.Interaction.CameraTool
{
    public class CameraSettings : MonoBehaviour,
        IAspectRatioProvider, IResolutionProvider
    {
        [System.Serializable]
        public abstract class BaseSettings
        {
            [SerializeField, Min(1)]
            [Tooltip("Width of the high-res screenshot, in pixels")]
            public int ScreenshotWidth = 1600;

            [SerializeField, Min(1)]
            [Tooltip("Height of the high-res screenshot, in pixels")]
            public int ScreenshotHeight = 1200;

            [SerializeField, Min(1f)]
            [Tooltip("Thumbnail size is resolution divided by this factor")]
            public float ThumbnailDownscale = 4f;

            [SerializeField]
            [Tooltip("The format of the screenshot written to storage")]
            public ImageFormat ImageFormat = ImageFormat.JPG;

            [SerializeField, Delayed]
            [Tooltip("Final image name will include date & time appended")]
            public string FileName = "Capture";

            [SerializeField]
            [Tooltip("Should AsyncGPUReadback be used on platforms that support it")]
            public bool PreferAsyncReadback = true;
        }

        [System.Serializable]
        public class AndroidSettings : BaseSettings { }

        [System.Serializable]
        public class StandaloneSettings : BaseSettings { }

        [SerializeField]
        private AndroidSettings _androidSettings;

        [SerializeField]
        private StandaloneSettings _standaloneSettings;

        /// <summary>
        /// The aspect ratio of the high-res screenshots, derived
        /// from <see cref="PixelWidth"/> and <see cref="PixelHeight"/>
        /// </summary>
        public float AspectRatio => (float)PixelWidth / PixelHeight;

        /// <summary>
        /// The pixel width of the high-res screenshot
        /// </summary>
        public int PixelWidth => SharedSettings.ScreenshotWidth;

        /// The pixel height of the high-res screenshot
        public int PixelHeight => SharedSettings.ScreenshotHeight;

        /// <summary>
        /// The name of the written image file
        /// </summary>
        public string FileName => SharedSettings.FileName;

        /// <summary>
        /// The format of the image written to storage
        /// </summary>
        public ImageFormat ImageFormat => SharedSettings.ImageFormat;

        /// <summary>
        /// Should AsyncGPUReadback be used on platforms that support it
        /// </summary>
        public bool PreferAsyncReadback => SharedSettings.PreferAsyncReadback;

        private BaseSettings SharedSettings
        {
            get
            {
                if (Application.platform == RuntimePlatform.Android)
                {
                    return _androidSettings;
                }
                else
                {
                    return _standaloneSettings;
                }
            }
        }

        public int GetThumbnailWidth()
        {
            return Mathf.Max(Mathf.RoundToInt(
                PixelWidth / SharedSettings.ThumbnailDownscale), 1);
        }

        public int GetThumbnailHeight()
        {
            return Mathf.Max(Mathf.RoundToInt(
                PixelHeight / SharedSettings.ThumbnailDownscale), 1);
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            const string INVALID_CHAR_REGEX = @"[^a-zA-Z0-9_-]";
            SharedSettings.FileName =
                Regex.Replace(SharedSettings.FileName, INVALID_CHAR_REGEX, "");
        }
#endif
    }
}
