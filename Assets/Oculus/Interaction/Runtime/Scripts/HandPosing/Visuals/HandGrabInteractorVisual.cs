/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using Oculus.Interaction.Input;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace Oculus.Interaction.HandPosing.Visuals
{
    /// <summary>
    /// This component is used to drive the HandGrabModifier.
    /// It sets the desired fingers and wrist positions of the hand structure
    /// in the SyntheticHand, informing it of any changes coming from the HandGrabInteractor.
    /// </summary>
    public class HandGrabInteractorVisual : MonoBehaviour
    {
        /// <summary>
        /// The HandGrabInteractor, to override the tracked hand
        /// when snapping to something.
        /// </summary>
        [SerializeField]
        [Interface(typeof(ISnapper))]
        private List<MonoBehaviour> _snappers;
        private List<ISnapper> Snappers;

        /// <summary>
        /// The syntheticHand is part of the InputDataStack and this
        /// class will set its values each frame.
        /// </summary>
        [SerializeField]
        private SyntheticHand _syntheticHand;

        private bool _areFingersFree = true;
        private bool _isWristFree = true;

        private ITrackingToWorldTransformer Transformer;
        private ISnapper _currentSnapper;

        protected bool _started = false;

        protected virtual void Awake()
        {
            Snappers = _snappers.ConvertAll(mono => mono as ISnapper);
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            foreach (ISnapper snapper in Snappers)
            {
                Assert.IsNotNull(snapper);
            }

            Assert.IsNotNull(_syntheticHand);
            Transformer = _syntheticHand.GetData().Config.TrackingToWorldTransformer;
            Assert.IsNotNull(Transformer);

            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                foreach (ISnapper snapper in _snappers)
                {
                    snapper.WhenSnapStarted += HandleSnapStarted;
                    snapper.WhenSnapEnded += HandleSnapEnded;
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                foreach (ISnapper snapper in _snappers)
                {
                    snapper.WhenSnapStarted -= HandleSnapStarted;
                    snapper.WhenSnapEnded -= HandleSnapEnded;
                }
            }
        }

        private void LateUpdate()
        {
            UpdateHand(_currentSnapper);
            _syntheticHand.MarkInputDataRequiresUpdate();
        }

        private void HandleSnapStarted(ISnapper snapper)
        {
            _currentSnapper = snapper;
        }

        private void HandleSnapEnded(ISnapper snapper)
        {
            if (_currentSnapper == snapper)
            {
                _currentSnapper = null;
            }
        }

        private void UpdateHand(ISnapper constrainingSnapper)
        {
            if (constrainingSnapper != null)
            {
                ConstrainingForce(constrainingSnapper, out float fingersConstraint, out float wristConstraint);
                UpdateHandPose(constrainingSnapper, fingersConstraint, wristConstraint);
            }
            else
            {
                FreeFingers();
                FreeWrist();
            }
        }

        private void ConstrainingForce(ISnapper snapper, out float fingersConstraint, out float wristConstraint)
        {
            ISnapData snap = snapper.SnapData;

            fingersConstraint = wristConstraint = 0;
            if (snap == null)
            {
                return;
            }

            bool isSnapping = snapper.IsSnapping;

            if (isSnapping && snap.HandAlignment != HandAlignType.None)
            {
                fingersConstraint = snap.HandPose != null ? 1f : 0f;
                wristConstraint = 1f;
            }
            else if (snap.HandAlignment == HandAlignType.AttractOnHover)
            {
                fingersConstraint = snap.HandPose != null ? snapper.SnapStrength : 0f;
                wristConstraint = snapper.SnapStrength;
            }

            if (fingersConstraint >= 1f && !isSnapping)
            {
                fingersConstraint = 0;
            }

            if (wristConstraint >= 1f && !isSnapping)
            {
                wristConstraint = 0f;
            }
        }

        private void UpdateHandPose(ISnapper snapper, float fingersConstraint, float wristConstraint)
        {
            ISnapData snap = snapper.SnapData;

            if (snap == null)
            {
                FreeFingers();
                FreeWrist();
                return;
            }

            if (fingersConstraint > 0f
                && snap.HandPose != null)
            {
                UpdateFingers(snap.HandPose, snapper.SnappingFingers(), fingersConstraint);
                _areFingersFree = false;
            }
            else
            {
                FreeFingers();
            }

            if (wristConstraint > 0f)
            {
                Pose wristLocalPose = GetWristPose(snap.WorldSnapPose, snapper.WristToSnapOffset);
                Pose wristPose = Transformer.ToTrackingPose(wristLocalPose);
                _syntheticHand.LockWristPose(wristPose, wristConstraint);
                _isWristFree = false;
            }
            else
            {
                FreeWrist();
            }
        }

        /// <summary>
        /// Writes the desired rotation values for each joint based on the provided SnapAddress.
        /// Apart from the rotations it also writes in the syntheticHand if it should allow rotations
        /// past that.
        /// When no snap is provided, it frees all fingers allowing unconstrained tracked motion.
        /// </summary>
        private void UpdateFingers(HandPose handPose, HandFingerFlags grabbingFingers, float strength)
        {
            Quaternion[] desiredRotations = handPose.JointRotations;
            _syntheticHand.OverrideAllJoints(desiredRotations, strength);

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

        #region Inject

        public void InjectAllHandGrabInteractorVisual(List<ISnapper> snappers, SyntheticHand syntheticHand)
        {
            InjectSnappers(snappers);
            InjectSyntheticHand(syntheticHand);
        }

        public void InjectSnappers(List<ISnapper> snappers)
        {
            _snappers = snappers.ConvertAll(mono => mono as MonoBehaviour);
            Snappers = snappers;
        }
        public void InjectSyntheticHand(SyntheticHand syntheticHand)
        {
            _syntheticHand = syntheticHand;
        }

        #endregion
    }
}
