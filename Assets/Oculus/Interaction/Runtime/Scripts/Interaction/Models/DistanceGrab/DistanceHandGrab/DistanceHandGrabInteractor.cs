/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using Oculus.Interaction.Grab;
using Oculus.Interaction.GrabAPI;
using Oculus.Interaction.Input;
using Oculus.Interaction.Throw;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction.HandPosing
{
    using SnapAddress = SnapAddress<DistanceHandGrabInteractable>;

    /// <summary>
    /// The DistanceHandGrabInteractor allows grabbing DistanceHandGrabInteractables at a distance.
    /// Similarly to the HandGrabInteractor it operates with HandGrabPoints to specify the final pose of the hand
    /// and as well as attracting objects at a distance it will held them in the same manner the HandGrabInteractor does.
    /// The DistanceHandGrabInteractor does not need a collider and uses conical frustums to detect far-away objects.
    /// </summary>
    public class DistanceHandGrabInteractor :
        PointerInteractor<DistanceHandGrabInteractor, DistanceHandGrabInteractable>
        , ISnapper, IHandGrabber, IDistanceInteractor
    {
        [SerializeField]
        private HandGrabAPI _handGrabApi;

        [SerializeField, Interface(typeof(IHand))]
        private MonoBehaviour _hand;

        public IHand Hand { get; private set; }

        [Header("Distance selection volumes")]
        [SerializeField]
        private DistantPointDetectorFrustums _detectionFrustums;

        [Header("Grabbing")]
        [SerializeField]
        private GrabTypeFlags _supportedGrabTypes = GrabTypeFlags.Pinch;

        [SerializeField]
        private HandWristOffset _gripPoint;

        [SerializeField, Optional]
        private Transform _pinchPoint;

        [SerializeField]
        private Transform _pointer;

        [SerializeField]
        private float _detectionDelay = 0f;

        [SerializeField, Interface(typeof(IVelocityCalculator)), Optional]
        private MonoBehaviour _velocityCalculator;

        public IVelocityCalculator VelocityCalculator { get; set; }

        private SnapAddress _currentSnap = new SnapAddress();
        private HandPose _cachedBestHandPose = new HandPose();
        private Pose _cachedBestSnapPoint = Pose.identity;

        private IMovement _movement;

        private SnapAddress _immediateAddress = new SnapAddress();
        private DistanceHandGrabInteractable _hoverCandidate;
        private float _hoverStartTime;

        private Pose _wristToSnapOffset;
        private Pose _snapOffset;
        private Pose _trackedGripPose;
        private Pose _trackedPinchPose;

        private HandGrabbableData _lastInteractableData =
            new HandGrabbableData();

        #region IHandGrabber

        public HandGrabAPI HandGrabApi => _handGrabApi;
        public GrabTypeFlags SupportedGrabTypes => _supportedGrabTypes;
        public IHandGrabbable TargetInteractable => Interactable;

        #endregion

        public ConicalFrustum PointerFrustum => _detectionFrustums.SelectionFrustum;

        #region ISnapper

        public virtual bool IsSnapping => HasSelectedInteractable
            && (_movement == null || _movement.Stopped);

        public float SnapStrength { get; private set; }
        public Pose WristToSnapOffset => _wristToSnapOffset;

        public HandFingerFlags SnappingFingers() =>
            HandGrab.GrabbingFingers(this, SelectedInteractable);

        public ISnapData SnapData { get; private set; }
        public System.Action<ISnapper> WhenSnapStarted { get; set; } = delegate { };
        public System.Action<ISnapper> WhenSnapEnded { get; set; } = delegate { };

        #endregion

        private DistantPointDetector _detector;

        #region editor events

        protected virtual void Reset()
        {
            _hand = this.GetComponentInParent<IHand>() as MonoBehaviour;
            _handGrabApi = this.GetComponentInParent<HandGrabAPI>();
        }

        #endregion

        protected override void Awake()
        {
            base.Awake();
            Hand = _hand as IHand;
            VelocityCalculator = _velocityCalculator as IVelocityCalculator;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, base.Start);
            Assert.IsNotNull(_pointer);
            Assert.IsNotNull(Hand);
            Assert.IsNotNull(_handGrabApi);
            Assert.IsNotNull(PointerFrustum);
            if (_velocityCalculator != null)
            {
                Assert.IsNotNull(VelocityCalculator);
            }

            _detector = new DistantPointDetector(_detectionFrustums);
            this.EndStart(ref _started);
        }

        #region life cycle

        protected override void DoEveryUpdate()
        {
            base.DoEveryUpdate();
            _gripPoint.GetWorldPose(ref _trackedGripPose);
            _gripPoint.GetOffset(ref _wristToSnapOffset);

            _trackedPinchPose = _pinchPoint.GetPose();

            if (!SnapAddress.IsNullOrInvalid(_currentSnap)
                && _currentSnap.SnappedToPinch)
            {
                if (State == InteractorState.Select)
                {
                    _wristToSnapOffset.Premultiply(_snapOffset);
                }
                else
                {
                    Pose gripToPinchOffset =
                        PoseUtils.RelativeOffset(_trackedPinchPose, _trackedGripPose);
                    _wristToSnapOffset.Premultiply(gripToPinchOffset);
                }
            }

            this.transform.SetPose(_trackedGripPose);
        }

        protected override void DoSelectUpdate()
        {
            DistanceHandGrabInteractable interactable = _selectedInteractable;
            if (interactable == null)
            {
                _currentSnap.Clear();
                ShouldUnselect = true;
                return;
            }

            Pose grabbingPoint = PoseUtils.Multiply(_trackedGripPose, _snapOffset);
            _movement.UpdateTarget(grabbingPoint);
            _movement.Tick();

            HandGrab.StoreGrabData(this, interactable, ref _lastInteractableData);
            ShouldUnselect = HandGrab.ComputeShouldUnselect(this, interactable);
        }

        protected override void DoHoverUpdate()
        {
            base.DoHoverUpdate();

            if (_currentSnap.IsValidAddress)
            {
                SnapStrength = HandGrab.ComputeHandGrabScore(this, Interactable,
                    out GrabTypeFlags hoverGrabTypes);
                SnapData = _currentSnap;
            }
            else
            {
                SnapStrength = 0f;
                SnapData = null;
            }

            if (Interactable != null)
            {
                ShouldSelect = HandGrab.ComputeShouldSelect(this, Interactable,
                    out GrabTypeFlags selectingGrabTypes);
            }
        }

        protected override void InteractableSelected(DistanceHandGrabInteractable interactable)
        {
            if (SnapAddress.IsNullOrInvalid(_currentSnap))
            {
                base.InteractableSelected(interactable);
                return;
            }

            if (_currentSnap.SnappedToPinch)
            {
                _snapOffset = PoseUtils.RelativeOffset(_trackedPinchPose, _trackedGripPose);
            }
            else
            {
                _snapOffset = Pose.identity;
            }

            Pose handGrabStartPose = PoseUtils.Multiply(_trackedGripPose, _snapOffset);
            Pose interactableGrabStartPose = _currentSnap.WorldSnapPose;
            _movement = interactable.GenerateMovement(interactableGrabStartPose, handGrabStartPose);
            base.InteractableSelected(interactable);
            interactable.WhenPointerEventRaised += HandleOtherPointerEventRaised;
        }

        protected override void InteractableUnselected(DistanceHandGrabInteractable interactable)
        {
            interactable.WhenPointerEventRaised -= HandleOtherPointerEventRaised;
            _movement?.StopAndSetPose(_movement.Pose);
            base.InteractableUnselected(interactable);
            _movement = null;

            ReleaseVelocityInformation throwVelocity = VelocityCalculator != null ?
                VelocityCalculator.CalculateThrowVelocity(interactable.transform) :
                new ReleaseVelocityInformation(Vector3.zero, Vector3.zero, Vector3.zero);
            interactable.ApplyVelocities(throwVelocity.LinearVelocity, throwVelocity.AngularVelocity);
        }

        protected override void InteractableSet(DistanceHandGrabInteractable interactable)
        {
            base.InteractableSet(interactable);
            WhenSnapStarted.Invoke(this);
        }

        protected override void InteractableUnset(DistanceHandGrabInteractable interactable)
        {
            base.InteractableUnset(interactable);
            WhenSnapEnded.Invoke(this);
        }

        protected override Pose ComputePointerPose()
        {
            if (SelectedInteractable != null)
            {
                return _movement.Pose;
            }

            return transform.GetPose();
        }

        protected virtual void HandleOtherPointerEventRaised(PointerArgs args)
        {
            if (SelectedInteractable == null)
            {
                return;
            }

            if (args.PointerEvent == PointerEvent.Select || args.PointerEvent == PointerEvent.Unselect)
            {
                Pose toPose = PoseUtils.Multiply(_trackedGripPose, _snapOffset);
                if (SelectedInteractable.ResetGrabOnGrabsUpdated)
                {
                    if (SelectedInteractable.CalculateBestPose(toPose, Hand.Scale, Hand.Handedness,
                        ref _cachedBestHandPose, ref _cachedBestSnapPoint,
                        out bool usesHandPose, out float poseScore))
                    {
                        bool usePinchPoint = _currentSnap.SnappedToPinch;
                        HandPose handPose = usesHandPose ? _cachedBestHandPose : null;
                        _currentSnap.Set(SelectedInteractable, handPose, _cachedBestSnapPoint, usePinchPoint);
                    }
                }

                Pose fromPose = _currentSnap.WorldSnapPose;
                _movement = SelectedInteractable.GenerateMovement(fromPose, toPose);
                SelectedInteractable.PointableElement.ProcessPointerEvent(
                    new PointerArgs(Identifier, PointerEvent.Move, fromPose));
            }

            if (args.Identifier == Identifier && args.PointerEvent == PointerEvent.Cancel)
            {
                SelectedInteractable.WhenPointerEventRaised -= HandleOtherPointerEventRaised;
            }
        }

        #endregion

        private bool CanSnapToPinchPoint(DistanceHandGrabInteractable interactable, GrabTypeFlags grabTypes)
        {
            return _pinchPoint != null
                && !interactable.UsesHandPose()
                && (grabTypes & GrabTypeFlags.Pinch) != 0;
        }

        protected override DistanceHandGrabInteractable ComputeCandidate()
        {
            bool keepCurrent = _immediateAddress.IsValidAddress
                && _detector.IsPointingWithoutAid(_immediateAddress.Interactable.Colliders);

            if (!keepCurrent)
            {
                ComputeBestSnapAddress(ref _immediateAddress);
            }

            if (_immediateAddress.Interactable != _hoverCandidate)
            {
                _hoverStartTime = Time.realtimeSinceStartup;
                _hoverCandidate = _immediateAddress.Interactable;
            }

            if (_immediateAddress.Interactable == _currentSnap.Interactable
                || (_immediateAddress.Interactable != _currentSnap.Interactable
                    && Time.realtimeSinceStartup - _hoverStartTime >= _detectionDelay))
            {
                _currentSnap.Set(_immediateAddress);
            }

            return _currentSnap.Interactable;
        }

        protected void ComputeBestSnapAddress(ref SnapAddress snapAddress)
        {
            DistanceHandGrabInteractable closestInteractable = null;
            float bestScore = float.NegativeInfinity;
            float bestFingerScore = float.NegativeInfinity;

            IEnumerable<DistanceHandGrabInteractable> interactables = DistanceHandGrabInteractable.Registry.List(this);

            foreach (DistanceHandGrabInteractable interactable in interactables)
            {
                float fingerScore = 1.0f;
                if (!HandGrab.ComputeShouldSelect(this, interactable, out GrabTypeFlags selectingGrabTypes))
                {
                    fingerScore = HandGrab.ComputeHandGrabScore(this, interactable, out selectingGrabTypes);
                    if (selectingGrabTypes == GrabTypeFlags.None)
                    {
                        selectingGrabTypes = _supportedGrabTypes;
                    }
                }
                if (fingerScore < bestFingerScore)
                {
                    continue;
                }

                if (!_detector.ComputeIsPointing(interactable.Colliders,
                        snapAddress.Interactable != null, out float score, out Vector3 hitPoint)
                    || score < bestScore)
                {
                    continue;
                }

                bool usePinchPoint = CanSnapToPinchPoint(interactable, selectingGrabTypes);
                Pose grabPoint = usePinchPoint ? _trackedPinchPose : _trackedGripPose;

                Pose worldPose = new Pose(hitPoint, grabPoint.rotation);
                bool poseFound = interactable.CalculateBestPose(worldPose, Hand.Scale, Hand.Handedness,
                    ref _cachedBestHandPose, ref _cachedBestSnapPoint,
                    out bool usesHandPose, out float poseScore);

                if (!poseFound)
                {
                    continue;
                }

                bestScore = score;
                closestInteractable = interactable;
                HandPose handPose = usesHandPose ? _cachedBestHandPose : null;
                snapAddress.Set(interactable, handPose, _cachedBestSnapPoint, usePinchPoint);
            }

            if (closestInteractable == null
                && snapAddress.IsValidAddress
                && !_detector.IsWithinDeselectionRange(snapAddress.Interactable.Colliders))
            {
                snapAddress.Clear();
            }
        }

        #region Inject
        public void InjectAllDistanceHandGrabInteractor(HandGrabAPI handGrabApi, DistantPointDetectorFrustums frustums,
            IHand hand, GrabTypeFlags supportedGrabTypes, HandWristOffset gripPoint, Transform pointer)
        {
            InjectHandGrabApi(handGrabApi);
            InjectDetectionFrustums(frustums);
            InjectHand(hand);
            InjectSupportedGrabTypes(supportedGrabTypes);
            InjectGripPoint(gripPoint);
            InjectPointer(pointer);
        }

        public void InjectHandGrabApi(HandGrabAPI handGrabApi)
        {
            _handGrabApi = handGrabApi;
        }

        public void InjectDetectionFrustums(DistantPointDetectorFrustums frustums)
        {
            _detectionFrustums = frustums;
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as MonoBehaviour;
            Hand = hand;
        }

        public void InjectSupportedGrabTypes(GrabTypeFlags supportedGrabTypes)
        {
            _supportedGrabTypes = supportedGrabTypes;
        }
        public void InjectGripPoint(HandWristOffset gripPoint)
        {
            _gripPoint = gripPoint;
        }
        public void InjectPointer(Transform pointer)
        {
            _pointer = pointer;
        }

        public void InjectOptionalPinchPoint(Transform pinchPoint)
        {
            _pinchPoint = pinchPoint;
        }

        public void InjectOptionalVelocityCalculator(IVelocityCalculator velocityCalculator)
        {
            _velocityCalculator = velocityCalculator as MonoBehaviour;
            VelocityCalculator = velocityCalculator;
        }
        #endregion
    }
}
