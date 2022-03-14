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
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;

namespace Oculus.Interaction.CameraTool
{
    /// <summary>
    /// Creates lightweight downascaled thumbnails for in-game display
    /// and manages the lifecyle of the created Textures.
    /// </summary>
    public class ThumbnailFactory : MonoBehaviour
    {
        [SerializeField]
        private CameraSettings _cameraSettings;

        public IThumbnail CreateThumbnail(RenderTexture texture, string imageId)
        {
            if (!isActiveAndEnabled)
            {
                return null;
            }

            var thumbnail = Factory.CreateThumbnail(texture,
                _cameraSettings.GetThumbnailWidth(),
                _cameraSettings.GetThumbnailHeight());

            thumbnail.ImageID = imageId;
            return thumbnail;
        }

        protected virtual void Start()
        {
            Assert.IsNotNull(_cameraSettings);
        }

        private static class Factory
        {
            public class Thumbnail : IThumbnail
            {
                public string ImageID { get; set; }

                public Texture2D Texture { get; set; }

                public void Destroy()
                {
                    if (Texture != null)
                    {
                        UnityEngine.Object.Destroy(Texture);
                        Texture = null;
                        PruneLookup();
                    }
                }
            }

            private static Dictionary<WeakReference, Texture2D> _lookup =
                new Dictionary<WeakReference, Texture2D>();

            private static List<Texture2D> _pool = new List<Texture2D>();

            private static void PruneLookup()
            {
                List<WeakReference> toRemove = new List<WeakReference>();
                foreach (WeakReference weakRef in _lookup.Keys)
                {
                    bool isDead = !weakRef.IsAlive || weakRef.Target == null ||
                                   ((Thumbnail)weakRef.Target).Texture == null;

                    if (isDead)
                    {
                        toRemove.Add(weakRef);
                        if (_lookup[weakRef] != null)
                        {
                            _pool.Add(_lookup[weakRef]);
                        }
                    }
                }
                foreach (var weakRef in toRemove)
                {
                    _lookup.Remove(weakRef);
                }
            }

            /// <summary>
            /// Create a thumbnail from a RenderTexture.
            /// NOTE: The returned image will exist only on the GPU,
            /// so cannot be saved to disk or have its pixels read.
            /// </summary>
            /// <param name="source">RenderTexture source</param>
            /// <param name="scaleFactor">Thumbnail size will be 1 / <paramref name="scaleFactor"/></param>
            /// <returns>A Thumbnail containing a Texture2D image</returns>
            public static Thumbnail CreateThumbnail(RenderTexture source, int width, int height)
            {
                PruneLookup();

                Assert.IsTrue(width > 0 && height > 0, "Thumbnail texture " +
                "must be at least 1x1 pixels");

                RenderTexture downscaled = RenderTexture.GetTemporary(width, height,
                                                                      source.depth,
                                                                      source.graphicsFormat);

                Graphics.Blit(source, downscaled);
                Texture2D thumbTex = _pool.Find((tex) => tex.width == downscaled.width &&
                                                         tex.height == downscaled.height);
                if (thumbTex != null)
                {
                    _pool.Remove(thumbTex);
                }
                else
                {
                    thumbTex = new Texture2D(downscaled.width, downscaled.height,
                                                downscaled.graphicsFormat, TextureCreationFlags.None);
                    thumbTex.hideFlags = HideFlags.HideAndDontSave;
                }

                Graphics.CopyTexture(downscaled, thumbTex);
                Thumbnail thumbnail = new Thumbnail() { Texture = thumbTex };
                _lookup.Add(new WeakReference(thumbnail), thumbTex);
                RenderTexture.ReleaseTemporary(downscaled);

                return thumbnail;
            }
        }
    }
}
