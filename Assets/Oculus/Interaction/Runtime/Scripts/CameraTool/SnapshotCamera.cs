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
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;

namespace Oculus.Interaction.CameraTool
{
    public class SnapshotCamera : MonoBehaviour, ICaptureCamera
    {
        [SerializeField]
        private Camera _camera;

        [SerializeField, Interface(typeof(IResolutionProvider))]
        private MonoBehaviour _resolutionProvider;

        public event Action WhenCaptured = delegate { };

        public float FieldOfView
        {
            get
            {
                return _camera.fieldOfView;
            }
            set
            {
                UpdateCamera();
                _camera.fieldOfView = value;
            }
        }

        public RenderTexture Texture => _textureData.Texture;

        private IResolutionProvider ResolutionProvider;
        private RenderTextureData _textureData;

        protected bool _started = false;

        public void Capture()
        {
            WhenCaptured();
        }

        private void UpdateCamera()
        {
            if (_textureData.HasTexture &&
               (ResolutionProvider.PixelWidth != _textureData.Width) ||
               (ResolutionProvider.PixelHeight != _textureData.Height))
            {
                ReleaseTexture();
            }

            if (!_textureData.HasTexture)
            {
                RebuildTexture();
            }

            _camera.targetTexture = _textureData.Texture;
            _camera.aspect = _textureData.AspectRatio;
            _camera.enabled = true;
        }

        private void RebuildTexture()
        {
            ReleaseTexture();
            _textureData.Create(ResolutionProvider.PixelWidth,
                                ResolutionProvider.PixelHeight);
        }

        private void ReleaseTexture()
        {
            _camera.targetTexture = null;
            _textureData.Release();
        }

        protected virtual void Awake()
        {
            ResolutionProvider = _resolutionProvider as IResolutionProvider;

            _textureData = new RenderTextureData();
            _camera.enabled = false;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(_camera);
            Assert.IsNotNull(ResolutionProvider);
            this.EndStart(ref _started);
        }

        protected virtual void Update()
        {
            UpdateCamera();
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                ReleaseTexture();
            }
        }

        private class RenderTextureData
        {
            public bool HasTexture => Texture != null;
            public RenderTexture Texture { get; private set; }
            public float Width { get; private set; }
            public float Height { get; private set; }
            public float AspectRatio { get; private set; }

            public void Create(int width, int height)
            {
                Release();

                Assert.IsTrue(width > 0 && height > 0, "Camera " +
                    "target texture resolution must be at least 1x1 pixels");

                Width = width;
                Height = height;
                AspectRatio = (float)width / height;
                Texture = RenderTexture.GetTemporary(width,
                                                     height,
                                                     32,
                                                     GraphicsFormat.R8G8B8A8_UNorm);
            }

            public void Release()
            {
                if (Texture != null)
                {
                    RenderTexture.ReleaseTemporary(Texture);
                    Texture = null;
                }
            }
        }
    }
}
