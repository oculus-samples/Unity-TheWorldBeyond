/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using UnityEngine.Experimental.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using System.Threading.Tasks;

namespace Oculus.Interaction.CameraTool
{
    internal partial class ImageWriter
    {
        private class ImageWriterAsync : IImageWriter
        {
            private NativeArray<byte> _byteBuffer;

            public void WriteImage(WriteRequest writeRequest)
            {
                int length = writeRequest.Width * writeRequest.Height * 4;
                if (!_byteBuffer.IsCreated || _byteBuffer.Length < length)
                {
                    if (_byteBuffer.IsCreated)
                    {
                        _byteBuffer.Dispose();
                    }

                    _byteBuffer = new NativeArray<byte>(length, Allocator.Persistent,
                                                        NativeArrayOptions.UninitializedMemory);
                }

                AsyncGPUReadback.RequestIntoNativeArray(ref _byteBuffer, writeRequest.Texture, 0,
                                                        0, writeRequest.Width,
                                                        0, writeRequest.Height,
                                                        0, 1,
                                                        writeRequest.GraphicsFormat, OnRequestComplete);

                void OnRequestComplete(AsyncGPUReadbackRequest readback)
                {
                    EncodeAndWriteAsync(readback, writeRequest);
                }
            }

            private async void EncodeAndWriteAsync(AsyncGPUReadbackRequest readbackRequest,
                                                   WriteRequest writeRequest)
            {
                if (readbackRequest.hasError)
                {
                    Debug.LogError("Could not read texture from GPU");
                    return;
                }

                int width = writeRequest.Texture.width;
                int height = writeRequest.Texture.height;

                NativeArray<byte> encoded = await Task.Run(() =>
                {
                    switch (writeRequest.ImageFormat)
                    {
                        default:
                        case ImageFormat.JPG:
                            return ImageConversion.EncodeNativeArrayToJPG(_byteBuffer,
                                writeRequest.GraphicsFormat, (uint)width, (uint)height);
                        case ImageFormat.PNG:
                            return ImageConversion.EncodeNativeArrayToPNG(_byteBuffer,
                                writeRequest.GraphicsFormat, (uint)width, (uint)height);
                    }
                });

                int fileSizeBytes = encoded.Length;
                byte[] byteArray = encoded.ToArray();

                string path = writeRequest.Path;

                await Task.Run(() =>
                {
                    System.IO.File.WriteAllBytes(path, byteArray);
                });

                encoded.Dispose();

                ImageMetadata metadata = new ImageMetadata()
                {
                    FileSize = fileSizeBytes,
                    Path = path,
                    Width = width,
                    Height = height,
                };

                writeRequest.Complete(metadata);
            }

            public void Destroy()
            {
                if (_byteBuffer.IsCreated)
                {
                    _byteBuffer.Dispose();
                }
            }
        }
    }
}
