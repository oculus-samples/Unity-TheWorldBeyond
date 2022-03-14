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
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine;
using System.IO;
using System;

namespace Oculus.Interaction.CameraTool
{
    public enum ImageFormat
    {
        JPG,
        PNG,
    }

    internal partial class ImageWriter : MonoBehaviour
    {
        private class WriteRequest
        {
            public bool isComplete { get; private set; }
            public RenderTexture Texture { get; private set; }

            public readonly string Path;
            public readonly int Width;
            public readonly int Height;
            public readonly ImageFormat ImageFormat;
            public readonly GraphicsFormat GraphicsFormat;

            private Action<ImageMetadata> _callback;

            public WriteRequest(RenderTexture texture, string path,
                ImageFormat imageFormat, Action<ImageMetadata> callback)

            {
                Path = path;
                Width = texture.width;
                Height = texture.height;
                ImageFormat = imageFormat;
                GraphicsFormat = texture.graphicsFormat;
                Texture = RenderTexture.GetTemporary(texture.descriptor);
                Graphics.CopyTexture(texture, Texture);
                _callback = callback;
            }

            public void Complete(ImageMetadata metadata)
            {
                RenderTexture.ReleaseTemporary(Texture);
                Texture = null;
                isComplete = true;
                _callback?.Invoke(metadata);
            }

            public void Cancel()
            {
                if (Texture != null)
                {
                    RenderTexture.ReleaseTemporary(Texture);
                    Texture = null;
                }
            }
        }

        private interface IImageWriter
        {
            void WriteImage(WriteRequest request);

            void Destroy();
        }

        private class ImageMetadata : IMetadata
        {
            public string ImageID { get; set; }
            public string Path { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public int FileSize { get; set; }
        }

        [SerializeField]
        private CameraSettings _cameraSettings;

        private IImageWriter _writer;

        private Queue<WriteRequest> _pendingRequests = new Queue<WriteRequest>();
        private WriteRequest _currentRequest;

        private string _lastFileName = string.Empty;
        private int _fileNameSuffix = 1;

        public void WriteImage(RenderTexture texture, string imageId, Action<IMetadata> callback)
        {
            if (!isActiveAndEnabled)
            {
                callback(null);
                return;
            }

            void TagAndCallback(ImageMetadata metadata)
            {
                metadata.ImageID = imageId;
                callback.Invoke(metadata);
            }

            WriteRequest request = new WriteRequest(texture, GetPath(),
                _cameraSettings.ImageFormat, TagAndCallback);

            _pendingRequests.Enqueue(request);
            HandleRequests();
        }

        protected string GetPath()
        {
            string fileName = $"{_cameraSettings.FileName}-" +
                              $"{DateTime.Now.ToString("yyyyMMdd-HHmmss")}";

            bool fileNameIsUnique = fileName != _lastFileName;
            _lastFileName = fileName;

            if (!fileNameIsUnique)
            {
                fileName += $"-{_fileNameSuffix++}";
            }
            else
            {
                _fileNameSuffix = 1;
            }

            fileName = Path.ChangeExtension(fileName, GetFileExtension());
            return Path.Combine(Application.persistentDataPath, fileName);
        }

        private string GetFileExtension()
        {
            switch (_cameraSettings.ImageFormat)
            {
                default:
                case ImageFormat.JPG:
                    return "jpg";
                case ImageFormat.PNG:
                    return "png";
            }
        }

        private void HandleRequests()
        {
            if (_currentRequest == null || _currentRequest.isComplete)
            {
                if (_pendingRequests.Count > 0)
                {
                    _currentRequest = _pendingRequests.Dequeue();
                    _writer.WriteImage(_currentRequest);
                }
                else
                {
                    _currentRequest = null;
                }
            }
        }

        protected virtual void Start()
        {
            Assert.IsNotNull(_cameraSettings);

            if (_cameraSettings.PreferAsyncReadback &&
                SystemInfo.supportsAsyncGPUReadback)
            {
                _writer = new ImageWriterAsync();
            }
            else
            {
                _writer = new ImageWriterSync();
            }
        }

        protected virtual void Update()
        {
            HandleRequests();
        }

        protected virtual void OnDestroy()
        {
            if (_writer != null)
            {
                _writer.Destroy();
            }

            while (_pendingRequests.Count > 0)
            {
                _pendingRequests.Dequeue().Cancel();
            }
        }
    }
}
