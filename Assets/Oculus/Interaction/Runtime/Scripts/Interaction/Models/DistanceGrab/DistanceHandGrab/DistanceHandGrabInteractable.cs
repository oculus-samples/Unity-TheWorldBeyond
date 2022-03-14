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
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction.HandPosing
{
    /// <summary>
    /// The DistanceHandGrabInteractable allows grabbing the marked object from far away.
    /// Internally it uses HandGrabPoints to specify not only the poses of the hands but the
    /// required gestures to perform the grab. It is possible (and recommended) to reuse the same
    /// HandGrabPoints used by the HandGrabInteractable, and even select just a few so they become
    /// the default poses when distant grabbing.
    /// </summary>
    [Serializable]
    public class DistanceHandGrabInteractable : PointerInteractable<DistanceHandGrabInteractor, DistanceHandGrabInteractable>,
        ISnappable, IRigidbodyRef, IHandGrabbable, IDistanceInteractable
    {
        [Header("Grab")]
        /// <summary>
        /// The transform of the object this HandGrabInteractable refers to.
        /// Typically the parent.
        /// </summary>
        [SerializeField]
        private Transform _relativeTo;

        [SerializeField]
        private Rigidbody _rigidbody;
        public Rigidbody Rigidbody => _rigidbody;

        [SerializeField]
        private bool _resetGrabOnGrabsUpdated = true;
        public bool ResetGrabOnGrabsUpdated
        {
            get
            {
                return _resetGrabOnGrabsUpdated;
            }
            set
            {
                _resetGrabOnGrabsUpdated = value;
            }
        }

        [SerializeField, Optional]
        private PhysicsGrabbable _physicsGrabbable = null;

        [Space]
        /// <summary>
        /// The available grab types dictates the available gestures for grabbing
        /// this interactable.
        /// </summary>
        [SerializeField]
        private GrabTypeFlags _supportedGrabTypes = GrabTypeFlags.Pinch;
        [SerializeField]
        private GrabbingRule _pinchGrabRules = GrabbingRule.DefaultPinchRule;
        [SerializeField]
        private GrabbingRule _palmGrabRules = GrabbingRule.DefaultPalmRule;

        [Header("Snap")]
        [SerializeField, Optional, Interface(typeof(IMovementProvider))]
        private MonoBehaviour _movementProvider;
        public IMovementProvider MovementProvider { get; set; }

        [SerializeField]
        private HandAlignType _handAligment = HandAlignType.AlignOnGrab;
        public HandAlignType HandAlignment
        {
            get
            {
                return _handAligment;
            }
            set
            {
                _handAligment = value;
            }
        }

        [SerializeField, Optional]
        private List<HandGrabPoint> _handGrabPoints = new List<HandGrabPoint>();
        /// <summary>
        /// General getter for the transform of the object this interactable refers to.
        /// </summary>
        public Transform RelativeTo => _relativeTo != null ? _relativeTo : this.transform.parent;

        public GrabTypeFlags SupportedGrabTypes => _supportedGrabTypes;
        public GrabbingRule PinchGrabRules => _pinchGrabRules;
        public GrabbingRule PalmGrabRules => _palmGrabRules;

        public List<HandGrabPoint> GrabPoints => _handGrabPoints;
        public Collider[] Colliders { get; private set; }

        private GrabPointsPoseFinder _grabPointsPoseFinder;

        private readonly PoseMeasureParameters _distanceScoreModifier = new PoseMeasureParameters(0f, 1f);

        #region editor events
        protected virtual void Reset()
        {
            _relativeTo = this.transform.parent;
            if (this.TryGetComponent(out HandGrabInteractable handGrabInteractable))
            {
                _relativeTo = handGrabInteractable.RelativeTo;
                _pinchGrabRules = handGrabInteractable.PinchGrabRules;
                _palmGrabRules = handGrabInteractable.PalmGrabRules;
                _supportedGrabTypes = handGrabInteractable.SupportedGrabTypes;
                _handGrabPoints = new List<HandGrabPoint>(handGrabInteractable.GrabPoints);
                _rigidbody = handGrabInteractable.Rigidbody;
            }
            else
            {
                _rigidbody = this.GetComponentInParent<Rigidbody>();
                _relativeTo = _rigidbody.transform;
                _physicsGrabbable = this.GetComponentInParent<PhysicsGrabbable>();
            }
        }
        #endregion

        protected override void Awake()
        {
            base.Awake();
            MovementProvider = _movementProvider as IMovementProvider;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, base.Start);
            Assert.IsNotNull(Rigidbody);
            Colliders = Rigidbody.GetComponentsInChildren<Collider>();
            Assert.IsTrue(Colliders.Length > 0,
                "The associated Rigidbody must have at least one Collider.");
            if (MovementProvider == null)
            {
                MoveTowardsTargetProvider movementProvider = this.gameObject.AddComponent<MoveTowardsTargetProvider>();
                InjectOptionalMovementProvider(movementProvider);
            }
            _grabPointsPoseFinder = new GrabPointsPoseFinder(_handGrabPoints, _relativeTo, this.transform);
            this.EndStart(ref _started);
        }

        public IMovement GenerateMovement(in Pose from, in Pose to)
        {
            IMovement movement = MovementProvider.CreateMovement();
            movement.StopAndSetPose(from);
            movement.MoveTo(to);
            return movement;
        }

        public bool CalculateBestPose(in Pose userPose, float handScale, Handedness handedness,
            ref HandPose result, ref Pose snapPoint,
            out bool usesHandPose, out float score)
        {
            return _grabPointsPoseFinder.FindBestPose(userPose, handScale, handedness,
                ref result, ref snapPoint, _distanceScoreModifier,
                out usesHandPose, out score);
        }

        public bool UsesHandPose()
        {
            return _grabPointsPoseFinder.UsesHandPose();
        }

        public void ApplyVelocities(Vector3 linearVelocity, Vector3 angularVelocity)
        {
            if (_physicsGrabbable == null)
            {
                return;
            }
            _physicsGrabbable.ApplyVelocities(linearVelocity, angularVelocity);
        }

        #region Inject

        public void InjectAllDistanceHandGrabInteractable(Transform relativeTo,
            Rigidbody rigidbody,
            GrabTypeFlags supportedGrabTypes,
            GrabbingRule pinchGrabRules, GrabbingRule palmGrabRules)
        {
            InjectRelativeTo(relativeTo);
            InjectRigidbody(rigidbody);
            InjectSupportedGrabTypes(supportedGrabTypes);
            InjectPinchGrabRules(pinchGrabRules);
            InjectPalmGrabRules(palmGrabRules);
        }

        public void InjectRelativeTo(Transform relativeTo)
        {
            _relativeTo = relativeTo;
        }

        public void InjectOptionalPhysicsObject(PhysicsGrabbable physicsObject)
        {
            _physicsGrabbable = physicsObject;
        }

        public void InjectRigidbody(Rigidbody rigidbody)
        {
            _rigidbody = rigidbody;
        }

        public void InjectSupportedGrabTypes(GrabTypeFlags supportedGrabTypes)
        {
            _supportedGrabTypes = supportedGrabTypes;
        }

        public void InjectPinchGrabRules(GrabbingRule pinchGrabRules)
        {
            _pinchGrabRules = pinchGrabRules;
        }

        public void InjectPalmGrabRules(GrabbingRule palmGrabRules)
        {
            _palmGrabRules = palmGrabRules;
        }

        public void InjectOptionalHandGrabPoints(List<HandGrabPoint> handGrabPoints)
        {
            _handGrabPoints = handGrabPoints;
        }

        public void InjectOptionalMovementProvider(IMovementProvider provider)
        {
            _movementProvider = provider as MonoBehaviour;
            MovementProvider = provider;
        }
        #endregion
    }
}
