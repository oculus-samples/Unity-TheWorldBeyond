/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using Oculus.Interaction.HandPosing;
using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace Oculus.Interaction.DistanceReticles
{
    public class ReticleGhostDrawer : InteractorReticle<ReticleDataGhost>
    {
        [SerializeField]
        private DistanceHandGrabInteractor _distanceInteractor;
        protected override IDistanceInteractor DistanceInteractor
        {
            get
            {
                return _distanceInteractor;
            }
            set { }
        }

        [FormerlySerializedAs("_modifier")]
        [SerializeField]
        private SyntheticHand _syntheticHand;

        [SerializeField]
        private HandVisual _visualHand;

        private bool _areFingersFree = true;
        private bool _isWristFree = true;

        private ITrackingToWorldTransformer Transformer;

        protected override void Start()
        {
            this.BeginStart(ref _started, base.Start);
            Assert.IsNotNull(_visualHand);
            Assert.IsNotNull(_syntheticHand);
            Transformer = _syntheticHand.GetData().Config.TrackingToWorldTransformer;
            Assert.IsNotNull(Transformer);
            this.EndStart(ref _started);
        }

        private void UpdateHandPose(ISnapper snapper)
        {
            ISnapData snap = snapper.SnapData;

            if (snap == null || snap.HandPose == null)
            {
                FreeFingers();
                FreeWrist();
                return;
            }

            UpdateFingers(snap.HandPose, snapper.SnappingFingers());
            Pose wristLocalPose = GetWristPose(snap.WorldSnapPose, snapper.WristToSnapOffset);
            Pose wristPose = Transformer.ToTrackingPose(wristLocalPose);
            _syntheticHand.LockWristPose(wristPose, 1f);

            _isWristFree = false;
            _areFingersFree = false;
        }

        private void UpdateFingers(HandPose handPose, HandFingerFlags grabbingFingers)
        {
            Quaternion[] desiredRotations = handPose.JointRotations;
            _syntheticHand.OverrideAllJoints(desiredRotations, 1f);

            for (int fingerIndex = 0; fingerIndex < Constants.NUM_FINGERS; fingerIndex++)
            {
                int fingerFlag = 1 << fingerIndex;
                JointFreedom fingerFreedom = handPose.FingersFreedom[fingerIndex];
                if (fingerFreedom == JointFreedom.Constrained
                    && ((int)grabbingFingers & fingerFlag) != 0)
                {
                    fingerFreedom = JointFreedom.Locked;
                }
                _syntheticHand.SetFingerFreedom((HandFinger)fingerIndex, fingerFreedom);
            }
        }

        private Pose GetWristPose(Pose gripPoint, Pose offset)
        {
            Pose wristOffset = offset;
            wristOffset.Invert();
            gripPoint.Premultiply(wristOffset);
            return gripPoint;
        }

        private bool FreeFingers()
        {
            if (!_areFingersFree)
            {
                _syntheticHand.FreeAllJoints();
                _areFingersFree = true;
                return true;
            }
            return false;
        }

        private bool FreeWrist()
        {
            if (!_isWristFree)
            {
                _syntheticHand.FreeWrist();
                _isWristFree = true;
                return true;
            }
            return false;
        }

        protected override void Align(ReticleDataGhost data, ConicalFrustum frustum)
        {
            UpdateHandPose(_distanceInteractor);
            _syntheticHand.MarkInputDataRequiresUpdate();
        }

        protected override void Draw(ReticleDataGhost data)
        {
            _visualHand.ForceOffVisibility = false;
        }

        protected override void Hide()
        {
            _visualHand.ForceOffVisibility = true;
        }

        #region Inject

        public void InjectAllReticleGhostDrawer(DistanceHandGrabInteractor interactor,
            SyntheticHand syntheticHand, HandVisual visualHand)
        {
            InjectDistanceInteractor(interactor);
            InjectSyntheticHand(syntheticHand);
            InjectVisualHand(visualHand);
        }

        public void InjectDistanceInteractor(DistanceHandGrabInteractor interactor)
        {
            _distanceInteractor = interactor;
        }

        public void InjectSyntheticHand(SyntheticHand syntheticHand)
        {
            _syntheticHand = syntheticHand;
        }

        public void InjectVisualHand(HandVisual visualHand)
        {
            _visualHand = visualHand;
        }
        #endregion
    }
}
