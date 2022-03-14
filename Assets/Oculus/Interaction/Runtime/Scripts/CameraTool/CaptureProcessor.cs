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
using UnityEngine.Assertions;
using System;

namespace Oculus.Interaction.CameraTool
{
    public class CaptureProcessor : MonoBehaviour,
        IThumbnailProvider, IMetadataProvider
    {
        public event Action<IThumbnail> WhenThumbnailProvided = delegate { };

        public event Action<IMetadata> WhenMetadataProvided = delegate { };

        [SerializeField, Interface(typeof(ICaptureCamera))]
        private MonoBehaviour _captureCamera;

        [SerializeField, Optional]
        private ImageWriter _imageWriter;

        [SerializeField, Optional]
        private ThumbnailFactory _thumbnailFactory;

        private ICaptureCamera CaptureCamera;

        protected bool _started = false;

        private void HandleCaptured()
        {
            string imageId = Guid.NewGuid().ToString("N");

            if (_thumbnailFactory != null)
            {
                IThumbnail thumbnail = _thumbnailFactory.
                CreateThumbnail(CaptureCamera.Texture, imageId);
                if (thumbnail != null)
                {
                    WhenThumbnailProvided.Invoke(thumbnail);
                }
            }

            if (_imageWriter != null)
            {
                _imageWriter.WriteImage(CaptureCamera.Texture,
                    imageId, (metadata) =>
                {
                    if (metadata != null)
                    {
                        WhenMetadataProvided.Invoke(metadata);
                    }
                });
            }
        }

        protected virtual void Awake()
        {
            CaptureCamera = _captureCamera as ICaptureCamera;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(CaptureCamera);
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                CaptureCamera.WhenCaptured += HandleCaptured;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                CaptureCamera.WhenCaptured -= HandleCaptured;
            }
        }
    }
}
