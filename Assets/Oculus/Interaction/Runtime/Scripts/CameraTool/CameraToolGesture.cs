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

namespace Oculus.Interaction.CameraTool
{
    public class CameraToolGesture : MonoBehaviour
    {
        private enum Facing
        {
            None,
            Forward,
            Backward,
        }

        private const float FOV_MIN = 20f;
        private const float FOV_MAX = 90f;

        [SerializeField, Interface(typeof(IActiveState))]
        private MonoBehaviour _startOnActive;

        [SerializeField, Interface(typeof(IActiveState))]
        private MonoBehaviour _captureOnActive;

        [SerializeField]
        private Transform _visuals;

        [SerializeField, Min(0)]
        private float _captureCooldownSeconds = 0.5f;

        [Tooltip("If provided, the camera visuals will align " +
            "to always be visible to this transform")]
        [SerializeField, Optional]
        private Transform _alwaysFace;

        [Header("Camera")]
        [SerializeField, Interface(typeof(ICaptureCamera))]
        private MonoBehaviour _captureCamera;

        [Tooltip("Camera will use the base FOV when photo rect " +
            "is at this width, in world units")]
        [SerializeField, Range(0.1f, 1f)]
        private float _isBaseFOVAtWidth = 0.3f;

        [Tooltip("The camera FOV at the base width")]
        [SerializeField, Range(FOV_MIN, FOV_MAX)]
        private float _baseFOV = 60f;

        private ICaptureCamera CaptureCamera;
        private IActiveState StartOnActive;
        private IActiveState CaptureOnActive;

        private Facing _initialFacing;
        private bool _isControllingCamera;
        private bool _allowCameraControl;
        private bool _captureStateActive;
        private float _lastCaptureStartTime;
        private float _lastCaptureTime;

        protected bool _started = false;

        private void UpdateIsControlling()
        {
            bool wasControllingCamera = _isControllingCamera;

            if (!StartOnActive.Active)
            {
                _allowCameraControl = true;
            }

            _isControllingCamera = _allowCameraControl &&
                                   StartOnActive.Active;

            if (_isControllingCamera && !wasControllingCamera)
            {
                _visuals.gameObject.SetActive(true);
            }
            else if (!_isControllingCamera)
            {
                _visuals.gameObject.SetActive(false);
            }
        }

        private void UpdateFacing()
        {
            if (!_isControllingCamera)
            {
                _initialFacing = Facing.None;
                return;
            }

            _visuals.transform.localRotation = Quaternion.LookRotation(Vector3.forward);
            if (_alwaysFace != null)
            {
                Facing newFacing = Facing.Forward;
                Vector3 visualsWorldForward = _visuals.transform.forward;
                Vector3 alwaysFaceToVisuals = _visuals.transform.position - _alwaysFace.position;

                if (Vector3.Dot(visualsWorldForward, alwaysFaceToVisuals) < 0)
                {
                    _visuals.transform.localRotation = Quaternion.LookRotation(Vector3.back);
                    newFacing = Facing.Backward;
                }

                if (_initialFacing == Facing.None)
                {
                    _initialFacing = newFacing;
                }
                else if (newFacing != _initialFacing)
                {
                    _captureCamera.transform.localRotation =
                        Quaternion.LookRotation(Vector3.back);
                }
                else
                {
                    _captureCamera.transform.localRotation =
                        Quaternion.LookRotation(Vector3.forward);
                }
            }
        }

        private void UpdateCamera()
        {
            if (!_isControllingCamera)
            {
                return;
            }

            float calcFOV = _baseFOV * (_visuals.transform.lossyScale.x / _isBaseFOVAtWidth);
            CaptureCamera.FieldOfView = Mathf.Clamp(calcFOV, FOV_MIN, FOV_MAX);
        }

        private void UpdateCapture()
        {
            if (!_isControllingCamera || CaptureOnActive == null)
            {
                _captureStateActive = false;
                return;
            }

            bool wasCaptureStateActive = _captureStateActive;
            _captureStateActive = CaptureOnActive.Active;

            if (_captureStateActive && !wasCaptureStateActive) // Pinch
            {
                _lastCaptureStartTime = Time.time;
            }
            else if (!_captureStateActive && wasCaptureStateActive) // Release
            {
                if (_lastCaptureStartTime - _lastCaptureTime > _captureCooldownSeconds)
                {
                    CaptureCamera.Capture();
                    _lastCaptureTime = Time.time;
                }
            }
        }

        protected virtual void Awake()
        {
            StartOnActive = _startOnActive as IActiveState;
            CaptureOnActive = _captureOnActive as IActiveState;
            CaptureCamera = _captureCamera as ICaptureCamera;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(_visuals);
            Assert.IsNotNull(StartOnActive);
            Assert.IsNotNull(CaptureOnActive);
            Assert.IsNotNull(CaptureCamera);
            this.EndStart(ref _started);
        }

        protected virtual void Update()
        {
            UpdateIsControlling();
            UpdateFacing();
            UpdateCamera();
            UpdateCapture();
        }
    }
}
