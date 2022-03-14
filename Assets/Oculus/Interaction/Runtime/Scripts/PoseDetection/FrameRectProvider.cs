/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using UnityEngine.Assertions;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Oculus.Interaction.PoseDetection
{
    public class FrameRectProvider : MonoBehaviour,
        IFrameRectProvider, IAspectRatioProvider
    {
        [SerializeField, Interface(typeof(IHand))]
        private MonoBehaviour _leftHand;

        [SerializeField, Interface(typeof(IHand))]
        private MonoBehaviour _rightHand;

        [SerializeField, Optional, Interface(typeof(IAspectRatioProvider))]
        private MonoBehaviour _aspectRatioProvider;

        [SerializeField, Optional, Interface(typeof(IActiveState))]
        private MonoBehaviour _updateIfActive;

        [SerializeField, Range(0.01f, 0.5f)]
        private float _vertRollThreshold = 0.1f;

        [Header("Smoothing")]
        [SerializeField]
        private OneEuroFilterPropertyBlock _filterProps = OneEuroFilterPropertyBlock.Default;

        public bool IsActive { get; private set; }

        public ref readonly FrameRect FrameRect => ref _frameRect;

        public float AspectRatio
        {
            get
            {
                if (AspectRatioProvider != null)
                {
                    return AspectRatioProvider.AspectRatio;
                }
                else
                {
                    return FrameRect.IsValid ? FrameRect.AspectRatio : 1f;
                }
            }
        }

        private IHand LeftHand;
        private IHand RightHand;
        private IActiveState UpdateIfActive;
        private IAspectRatioProvider AspectRatioProvider;

        private IOneEuroFilter<Vector3> _leftFilter;
        private IOneEuroFilter<Vector3> _rightFilter;

        private FrameRect _frameRect;

        protected bool _started;

        protected virtual void Awake()
        {
            LeftHand = _leftHand as IHand;
            RightHand = _rightHand as IHand;
            UpdateIfActive = _updateIfActive as IActiveState;
            AspectRatioProvider = _aspectRatioProvider as IAspectRatioProvider;

            _leftFilter = OneEuroFilter.CreateVector3();
            _rightFilter = OneEuroFilter.CreateVector3();
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(LeftHand);
            Assert.IsNotNull(RightHand);
            this.EndStart(ref _started);
        }

        protected virtual void Update()
        {
            if (CheckCanActivate() &&
                GetHMDPose(out Pose hmdPose))
            {
                bool wasActive = IsActive;
                IsActive = true;

                if (!wasActive)
                {
                    _leftFilter.Reset();
                    _rightFilter.Reset();
                }

                UpdateFrameRect(hmdPose);
            }
            else
            {
                _frameRect = new FrameRect();
                IsActive = false;
            }
        }

        private bool GetHMDPose(out Pose pose)
        {
            if (LeftHand.GetCenterEyePose(out pose) ||
                RightHand.GetCenterEyePose(out pose))
            {
                return true;
            }
            else
            {
                pose = Pose.identity;
                return false;
            }
        }

        private bool CheckCanActivate()
        {
            if (!LeftHand.IsTrackedDataValid || !RightHand.IsTrackedDataValid)
            {
                return false;
            }

            if (UpdateIfActive != null && !UpdateIfActive.Active)
            {
                return false;
            }

            return true;
        }

        private void UpdateFrameRect(Pose hmdPose)
        {
            GetRectCorner(LeftHand, out Vector3 leftMidpoint);
            GetRectCorner(RightHand, out Vector3 rightMidpoint);

            _leftFilter.SetProperties(_filterProps);
            _rightFilter.SetProperties(_filterProps);
            leftMidpoint = _leftFilter.Step(leftMidpoint);
            rightMidpoint = _rightFilter.Step(rightMidpoint);

            Vector3 diagonal = rightMidpoint - leftMidpoint;
            Vector3 diagMidpoint = Vector3.Lerp(leftMidpoint, rightMidpoint, 0.5f);
            Vector3 hmdToMidpoint = (diagMidpoint - hmdPose.position).normalized;

            // If facing down or up, world relative horizontal breaks and hmd relative horizontal is used
            float dot = 1f - Mathf.Clamp01(Mathf.Abs(Vector3.Dot(hmdToMidpoint, Vector3.up)));
            float lerp = dot < _vertRollThreshold ? Mathf.Clamp01(dot / _vertRollThreshold) : 0f;

            Vector3 hmdRelativeHorizontal = Vector3.Cross(CancelY(hmdPose.up), -Vector3.up);
            Vector3 worldRelativeHorizontal = Vector3.Cross(CancelY(hmdToMidpoint), -Vector3.up);
            Vector3 horizontal = Vector3.Lerp(worldRelativeHorizontal, hmdRelativeHorizontal, lerp).normalized;

            float angle = Vector3.Angle(horizontal, diagonal);
            horizontal *= Mathf.Cos(Mathf.Deg2Rad * angle) * diagonal.magnitude;

            _frameRect = new FrameRect(leftMidpoint,
                                       rightMidpoint - horizontal,
                                       rightMidpoint,
                                       leftMidpoint + horizontal);

            if (AspectRatioProvider != null)
            {
                _frameRect = FixAspect(FrameRect, AspectRatioProvider.AspectRatio);
            }
        }

        private FrameRect FixAspect(in FrameRect src, in float aspect)
        {
            float targetHeight = src.Width * (1f / aspect);
            float overshoot = (src.TopLeft - src.BottomLeft).magnitude - targetHeight;
            return new FrameRect(
                Vector3.MoveTowards(src.BottomLeft, src.TopLeft, overshoot / 2f),
                Vector3.MoveTowards(src.TopLeft, src.BottomLeft, overshoot / 2f),
                Vector3.MoveTowards(src.TopRight, src.BottomRight, overshoot / 2f),
                Vector3.MoveTowards(src.BottomRight, src.TopRight, overshoot / 2f),
                aspect
                );
        }

        private Vector3 CancelY(Vector3 vec)
        {
            vec.y = 0;
            return vec.normalized;
        }

        private void GetRectCorner(IHand hand, out Vector3 midpoint)
        {
            // Left hand: Y out from pad, Z toward thumb
            // Right hand: Y out from nail.
            hand.GetJointPose(HandJointId.HandIndex1, out var indexProximal);
            hand.GetJointPose(HandJointId.HandThumb3, out var thumb3);
            midpoint = (indexProximal.position + thumb3.position) * 0.5f;
        }

        #region Inject

        public void InjectAllFrameRectProvider(IHand leftHand,
                                               IHand rightHand)
        {
            InjectLeftHand(leftHand);
            InjectRightHand(rightHand);
        }
        public void InjectLeftHand(IHand leftHand)
        {
            LeftHand = leftHand;
            _leftHand = leftHand as MonoBehaviour;
        }
        public void InjectRightHand(IHand rightHand)
        {
            RightHand = rightHand;
            _rightHand = rightHand as MonoBehaviour;
        }
        public void InjectOptionalUpdateIfActive(IActiveState updateIfActive)
        {
            UpdateIfActive = updateIfActive;
            _updateIfActive = updateIfActive as MonoBehaviour;
        }
        public void InjectOptionalAspectRatioProvider(IAspectRatioProvider aspectRatioProvider)
        {
            AspectRatioProvider = aspectRatioProvider;
            _aspectRatioProvider = aspectRatioProvider as MonoBehaviour;
        }

        #endregion
    }
}
