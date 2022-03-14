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
    using SnapAddress = SnapAddress<HandGrabInteractable>;

    /// <summary>
    /// The HandGrabInteractor allows grabbing objects while having the hands snap to them
    /// adopting a previously authored HandPose.
    /// There are different snapping techniques available, and when None is selected it will
    /// behave as a normal GrabInteractor.
    /// </summary>
    public class HandGrabInteractor : PointerInteractor<HandGrabInteractor, HandGrabInteractable>,
        ISnapper, IRigidbodyRef, IHandGrabber
    {
        [SerializeField, Interface(typeof(IHand))]
        private MonoBehaviour _hand;
        public IHand Hand { get; private set; }

        [SerializeField]
        private Rigidbody _rigidbody;

        [SerializeField]
        private HandGrabAPI _handGrabApi;

        [SerializeField]
        private GrabTypeFlags _supportedGrabTypes = GrabTypeFlags.All;

        [SerializeField]
        private HandWristOffset _gripPoint;
        [SerializeField, Optional]
        private Transform _pinchPoint;

        [SerializeField, Interface(typeof(IVelocityCalculator)), Optional]
        private MonoBehaviour _velocityCalculator;
        public IVelocityCalculator VelocityCalculator { get; set; }

        private SnapAddress _currentSnap = new SnapAddress();
        private HandPose _cachedBestHandPose = new HandPose();
        private Pose _cachedBestSnapPoint = Pose.identity;

        private IMovement _movement;

        private Pose _wristToSnapOffset;
        private Pose _snapOffset;
        private Pose _trackedGripPose;
        private Pose _trackedPinchPose;

        private Grab.HandGrabbableData _lastInteractableData = new Grab.HandGrabbableData();

        #region IHandGrabber
        public HandGrabAPI HandGrabApi => _handGrabApi;
        public GrabTypeFlags SupportedGrabTypes => _supportedGrabTypes;
        public IHandGrabbable TargetInteractable => Interactable;
        #endregion

        #region ISnapper

        public virtual bool IsSnapping => HasSelectedInteractable
            && (_movement == null || _movement.Stopped);

        public float SnapStrength { get; private set; }

        public HandFingerFlags SnappingFingers()
        {
            return HandGrab.GrabbingFingers(this, SelectedInteractable);
        }

        public Pose WristToSnapOffset => _wristToSnapOffset;

        public ISnapData SnapData { get; private set; }
        public System.Action<ISnapper> WhenSnapStarted { get; set; } = delegate { };
        public System.Action<ISnapper> WhenSnapEnded { get; set; } = delegate { };
        #endregion

        #region IRigidbodyRef
        public Rigidbody Rigidbody => _rigidbody;
        #endregion

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

            Assert.IsNotNull(Rigidbody);
            Collider[] colliders = Rigidbody.GetComponentsInChildren<Collider>();
            Assert.IsTrue(colliders.Length > 0,
                "The associated Rigidbody must have at least one Collider.");
            foreach (Collider collider in colliders)
            {
                Assert.IsTrue(collider.isTrigger,
                    "Associated Colliders must be marked as Triggers.");
            }
            Assert.IsNotNull(_handGrabApi);
            Assert.IsNotNull(Hand);
            if (_velocityCalculator != null)
            {
                Assert.IsNotNull(VelocityCalculator);
            }

            this.EndStart(ref _started);
        }

        #region life cycle

        /// <summary>
        /// During the update event, move the current interactor (containing also the
        /// trigger for detecting nearby interactableS) to the tracked position of the grip.
        ///
        /// That is the tracked wrist plus a pregenerated position and rotation offset.
        /// </summary>
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
                    Pose gripToPinchOffset = PoseUtils.RelativeOffset(_trackedPinchPose, _trackedGripPose);
                    _wristToSnapOffset.Premultiply(gripToPinchOffset);
                }
            }

            this.transform.SetPose(_trackedGripPose);
        }

        /// <summary>
        /// Each call while the interactor is hovering, it checks whether there is an interaction
        /// being hovered and sets the target snapping address to it. In the HandToObject snapping
        /// behaviors this is relevant as the hand can approach the object progressively even before
        /// a true grab starts.
        /// </summary>
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

        /// <summary>
        /// Each call while the hand is selecting/grabbing an interactable, it moves the item to the
        /// new position while also attracting it towards the hand if the snapping mode requires it.
        ///
        /// In some cases the parameter can be null, for example if the selection was interrupted
        /// by another hand grabbing the object. In those cases it will come out of the release
        /// state once the grabbing gesture properly finishes.
        /// </summary>
        /// <param name="interactable">The selected item</param>
        protected override void DoSelectUpdate()
        {
            HandGrabInteractable interactable = _selectedInteractable;
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

        /// <summary>
        /// When a new interactable is selected, start the grab at the ideal point. When snapping is
        /// involved that can be a point in the interactable offset from the hand
        /// which will be stored to progressively reduced it in the next updates,
        /// effectively attracting the object towards the hand.
        /// When no snapping is involved the point will be the grip point of the hand directly.
        /// Note: ideally this code would be in InteractableSelected but it needs
        /// to be called before the object is marked as active.
        /// </summary>
        /// <param name="snap">The selected Snap Data </param>
        protected override void InteractableSelected(HandGrabInteractable interactable)
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
        }

        /// <summary>
        /// When releasing an active interactable, calculate the releasing point in similar
        /// fashion to  InteractableSelected
        /// </summary>
        /// <param name="interactable">The released interactable</param>
        protected override void InteractableUnselected(HandGrabInteractable interactable)
        {
            base.InteractableUnselected(interactable);

            _movement = null;

            ReleaseVelocityInformation throwVelocity = VelocityCalculator != null ?
                VelocityCalculator.CalculateThrowVelocity(interactable.transform) :
                new ReleaseVelocityInformation(Vector3.zero, Vector3.zero, Vector3.zero);
            interactable.ApplyVelocities(throwVelocity.LinearVelocity, throwVelocity.AngularVelocity);
        }

        protected override void InteractableSet(HandGrabInteractable interactable)
        {
            base.InteractableSet(interactable);
            WhenSnapStarted.Invoke(this);
        }

        protected override void InteractableUnset(HandGrabInteractable interactable)
        {
            base.InteractableUnset(interactable);
            WhenSnapEnded.Invoke(this);
        }

        protected override void HandlePointerEventRaised(PointerArgs args)
        {
            base.HandlePointerEventRaised(args);
            if (SelectedInteractable == null)
            {
                return;
            }

            if (args.Identifier != Identifier &&
                (args.PointerEvent == PointerEvent.Select || args.PointerEvent == PointerEvent.Unselect))
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
        }

        protected override Pose ComputePointerPose()
        {
            if (SelectedInteractable != null)
            {
                return _movement.Pose;
            }

            return transform.GetPose();
        }

        #endregion

        #region grab detection

        private bool CanSnapToPinchPoint(HandGrabInteractable interactable, GrabTypeFlags grabTypes)
        {
            return _pinchPoint != null
                && !interactable.UsesHandPose()
                && (grabTypes & GrabTypeFlags.Pinch) != 0;
        }

        #endregion

        /// <summary>
        /// Compute the best interactable to snap to. In order to do it the method measures
        /// the score from the current grip pose to the closes pose in the surfaces
        /// of each one of the interactables in the registry.
        /// Even though it returns the best interactable, it also saves the entire SnapAddress to
        /// it in which the exact pose within the surface is already recorded to avoid recalculations
        /// within the same frame.
        /// </summary>
        /// <returns>The best interactable to snap the hand to.</returns>
        protected override HandGrabInteractable ComputeCandidate()
        {
            ComputeBestSnapAddress(ref _currentSnap);
            return _currentSnap.Interactable;
        }

        protected virtual void ComputeBestSnapAddress(ref SnapAddress snapAddress)
        {
            IEnumerable<HandGrabInteractable> interactables = HandGrabInteractable.Registry.List(this);
            float bestFingerScore = -1f;
            float bestPoseScore = -1f;

            foreach (HandGrabInteractable interactable in interactables)
            {
                float fingerScore = 1.0f;
                if (!HandGrab.ComputeShouldSelect(this, interactable, out GrabTypeFlags selectingGrabTypes))
                {
                    fingerScore = HandGrab.ComputeHandGrabScore(this, interactable, out selectingGrabTypes);
                }
                if (fingerScore < bestFingerScore)
                {
                    continue;
                }

                bool usePinchPoint = CanSnapToPinchPoint(interactable, selectingGrabTypes);
                Pose grabPoint = usePinchPoint ? _trackedPinchPose : _trackedGripPose;
                bool poseFound = interactable.CalculateBestPose(grabPoint, Hand.Scale, Hand.Handedness,
                    ref _cachedBestHandPose, ref _cachedBestSnapPoint,
                    out bool usesHandPose, out float poseScore);

                if (!poseFound)
                {
                    continue;
                }

                if (fingerScore > bestFingerScore
                    || poseScore > bestPoseScore)
                {
                    bestFingerScore = fingerScore;
                    bestPoseScore = poseScore;
                    HandPose handPose = usesHandPose ? _cachedBestHandPose : null;
                    snapAddress.Set(interactable, handPose, _cachedBestSnapPoint, usePinchPoint);
                }

            }

            if (bestFingerScore < 0)
            {
                snapAddress.Clear();
            }
        }

        #region Inject

        public void InjectAllHandGrabInteractor(HandGrabAPI handGrabApi,
            IHand hand, Rigidbody rigidbody, GrabTypeFlags supportedGrabTypes, HandWristOffset gripPoint)
        {
            InjectHandGrabApi(handGrabApi);
            InjectHand(hand);
            InjectRigidbody(rigidbody);
            InjectSupportedGrabTypes(supportedGrabTypes);
            InjectGripPoint(gripPoint);
        }

        public void InjectHandGrabApi(HandGrabAPI handGrabAPI)
        {
            _handGrabApi = handGrabAPI;
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as MonoBehaviour;
            Hand = hand;
        }

        public void InjectRigidbody(Rigidbody rigidbody)
        {
            _rigidbody = rigidbody;
        }

        public void InjectSupportedGrabTypes(GrabTypeFlags supportedGrabTypes)
        {
            _supportedGrabTypes = supportedGrabTypes;
        }

        public void InjectGripPoint(HandWristOffset gripPoint)
        {
            _gripPoint = gripPoint;
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
