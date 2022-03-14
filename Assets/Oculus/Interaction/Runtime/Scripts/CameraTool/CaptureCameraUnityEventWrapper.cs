/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using UnityEngine.Events;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction.CameraTool
{
    public class CaptureCameraUnityEventWrapper : MonoBehaviour
    {
        [SerializeField, Interface(typeof(ICaptureCamera))]
        private MonoBehaviour _captureCamera;
        private ICaptureCamera CaptureCamera;

        [SerializeField]
        private UnityEvent _whenCaptured;

        protected bool _started = false;

        private void HandleWhenCaptured()
        {
            _whenCaptured.Invoke();
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
                CaptureCamera.WhenCaptured += HandleWhenCaptured;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                CaptureCamera.WhenCaptured -= HandleWhenCaptured;
            }
        }
    }
}
