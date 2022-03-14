/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using Unity.Collections;
using UnityEngine;
using System.Threading.Tasks;
using System.IO;

namespace Oculus.Interaction.CameraTool
{
    internal partial class ImageWriter
    {
        private class ImageWriterSync : IImageWriter
        {
            private Texture2D _texBuffer;

            public async void WriteImage(WriteRequest writeRequest)
            {
                ImageMetadata metadata = await WriteImageAsync(writeRequest);
                writeRequest.Complete(metadata);
            }

            private async Task<ImageMetadata> WriteImageAsync(WriteRequest writeRequest)
            {
                int width = writeRequest.Width;
                int height = writeRequest.Height;

                if (_texBuffer == null || _texBuffer.width != width || _texBuffer.height != height)
                {
                    if (_texBuffer != null)
                    {
                        UnityEngine.Object.Destroy(_texBuffer);
                    }

                    _texBuffer = new Texture2D(width, height);
                    _texBuffer.hideFlags = HideFlags.HideAndDontSave;
                }

                var prevActive = RenderTexture.active;
                RenderTexture.active = writeRequest.Texture;
                _texBuffer.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                RenderTexture.active = prevActive;
                _texBuffer.Apply();

                NativeArray<byte> toEncode = _texBuffer.GetRawTextureData<byte>();
                NativeArray<byte> encoded = await Task.Run(() =>
                {
                    switch (writeRequest.ImageFormat)
                    {
                        default:
                        case ImageFormat.JPG:
                            return ImageConversion.EncodeNativeArrayToJPG(toEncode,
                                writeRequest.GraphicsFormat, (uint)width, (uint)height);
                        case ImageFormat.PNG:
                            return ImageConversion.EncodeNativeArrayToPNG(toEncode,
                                writeRequest.GraphicsFormat, (uint)width, (uint)height);
                    }
                });

                int fileSizeBytes = encoded.Length;
                byte[] byteArray = encoded.ToArray();
                string path = writeRequest.Path;

                await Task.Run(() =>
                {
                    File.WriteAllBytes(path, byteArray);
                });

                toEncode.Dispose();
                encoded.Dispose();

                ImageMetadata metadata = new ImageMetadata()
                {
                    FileSize = fileSizeBytes,
                    Path = path,
                    Width = width,
                    Height = height,
                };

                return metadata;
            }

            public void Destroy()
            {
                if (_texBuffer != null)
                {
                    UnityEngine.Object.Destroy(_texBuffer);
                }
            }
        }
    }
}
