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
using Oculus.Interaction.PoseDetection;
using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction
{
    /// <summary>
    /// TouchGrabHandInteractor provides a hand-specific grab interaction model
    /// where selection begins when finger tips overlap with an associated interactable.
    /// Upon selection, the distance between the fingers and thumb is cached and is used for
    /// determining the point of release: when fingers are outside of the cached distance.
    /// </summary>
    public class
        TouchHandGrabInteractor : PointerInteractor<TouchHandGrabInteractor, TouchHandGrabInteractable>
    {
        [SerializeField]
        private Transform _hoverLocation;

        [SerializeField]
        private Transform _grabLocation;

        [SerializeField, Interface(typeof(IHand))]
        private MonoBehaviour _hand;
        private IHand Hand { get; set; }

        [SerializeField, Interface(typeof(JointRotationHistoryHand))]
        private MonoBehaviour _historyHand;
        private JointRotationHistoryHand HistoryHand;

        [SerializeField]
        private float _minHoverDistance = 0.05f;

        [SerializeField]
        private int _iterations = 10;

        public Vector3 GrabPosition => _grabLocation.position;
        public Quaternion GrabRotation => _grabLocation.rotation;

        private ShadowJointsHelper _shadowJointsHelper = null;

        public Action WhenFingerLocked = delegate { };

        private class FingerStatus
        {
            public HandFinger HandFinger;
            public HandJointId FingerTipJointId;
            public int FingerTipIndex;
            public ShadowJointsHelper.ShadowJoints ShadowFrom;
            public ShadowJointsHelper.ShadowJoints ShadowTo;
            public bool Touching = false;

            public bool Locked = false;
            public Vector3 LocalLockPosition;
            public Vector3[] JointLockPositions;
            public Quaternion[] JointLockRotations;

            public float CurlValueAtLockTime = 0f;
            public Vector3 CenterAtLockTime;
            public float DistanceToCenterAtLockTime = 0f;
        }

        [SerializeField]
        private float _releaseThreshold = 0.01f;

        private FingerStatus[] _fingerStatuses;

        public bool IsFingerTouching(HandFinger finger)
        {
            if (State == InteractorState.Select && _selectedInteractable == null)
            {
                return false;
            }
            return _fingerStatuses[(int)finger].Touching;
        }

        private static readonly HandJointId[][] FINGER_JOINT_IDS = new[]
        {
            new HandJointId[]
            {
                HandJointId.HandWristRoot,
                HandJointId.HandThumb0,
                HandJointId.HandThumb1,
                HandJointId.HandThumb2,
                HandJointId.HandThumb3,
                HandJointId.HandThumbTip
            },
            new HandJointId[]
            {
                HandJointId.HandWristRoot,
                HandJointId.HandIndex1,
                HandJointId.HandIndex2,
                HandJointId.HandIndex3,
                HandJointId.HandIndexTip
            },
            new HandJointId[]
            {
                HandJointId.HandWristRoot,
                HandJointId.HandMiddle1,
                HandJointId.HandMiddle2,
                HandJointId.HandMiddle3,
                HandJointId.HandMiddleTip
            },
            new HandJointId[]
            {
                HandJointId.HandWristRoot,
                HandJointId.HandRing1,
                HandJointId.HandRing2,
                HandJointId.HandRing3,
                HandJointId.HandRingTip
            },
            new HandJointId[]
            {
                HandJointId.HandWristRoot,
                HandJointId.HandPinky0,
                HandJointId.HandPinky1,
                HandJointId.HandPinky2,
                HandJointId.HandPinky3,
                HandJointId.HandPinkyTip
            }
        };

        // Hard-coded thumb values in an extended-grab position
        private static readonly Quaternion[] THUMB_GRAB_ROTATIONS =
        {
            Quaternion.Euler(37f, -62f, -17f),
            Quaternion.Euler(20f, -35.5f, -29f),
            Quaternion.Euler(-10f, 8f, 7f),
            Quaternion.Euler(9f, -7f, 0f)
        };

        // Minimum z rotation for a pushed-out grab pose on finger joints
        private static readonly float MIN_FINGER_ROTATION_ON_Z = -5f;

        private FingerShapes _fingerShapes = new FingerShapes();

        protected override void Awake()
        {
            base.Awake();
            Hand = _hand as IHand;
            HistoryHand = _historyHand as JointRotationHistoryHand;
        }

        protected override void Start()
        {
            base.Start();
            Assert.IsNotNull(_hoverLocation);
            Assert.IsNotNull(_grabLocation);
            Assert.IsNotNull(Hand);
            Assert.IsNotNull(HistoryHand);

            GameObject collisionFingers = new GameObject("Shadow Joints Helper");
            collisionFingers.transform.parent = transform;
            _shadowJointsHelper = collisionFingers.AddComponent<ShadowJointsHelper>();
            _shadowJointsHelper.Iterations = _iterations;
            _fingerStatuses = new FingerStatus[5];

            for (int i = 0; i < _fingerStatuses.Length; i++)
            {
                HandJointId[] handJointIds = FINGER_JOINT_IDS[i];
                _fingerStatuses[i] = new FingerStatus()
                {
                    HandFinger = (HandFinger)i,
                    FingerTipJointId = handJointIds[handJointIds.Length - 1],
                    FingerTipIndex = handJointIds.Length - 1,
                    ShadowFrom = new ShadowJointsHelper.ShadowJoints(handJointIds),
                    ShadowTo = new ShadowJointsHelper.ShadowJoints(handJointIds),
                    JointLockPositions = new Vector3[handJointIds.Length - 1],
                    JointLockRotations = new Quaternion[handJointIds.Length - 1]
                };
            }
        }

        private Pose GetJointPose(HandJointId jointId)
        {
            Hand.GetJointPose(jointId, out Pose jointPose);
            return jointPose;
        }

        private void PushOutTouching(TouchHandGrabInteractable targetInteractable)
        {
            HistoryHand.SetHistoryOffset(1);
            for (int i = 0; i < Constants.NUM_FINGERS; i++)
            {
                FingerStatus fingerStatus = _fingerStatuses[i];
                if (fingerStatus.Locked)
                {
                    continue;
                }

                ShadowJointsHelper.ShadowJoints from = fingerStatus.ShadowFrom;
                ShadowJointsHelper.ShadowJoints to = fingerStatus.ShadowTo;

                to.SetWristJointFromHand(Hand);
                to.SetLocalFingerJointsFromHand(Hand);

                fingerStatus.Touching = false;

                if (State == InteractorState.Select)
                {
                    from.SetWristJointFromHand(HistoryHand);
                    from.SetLocalFingerJointsFromHand(HistoryHand);
                    float touchTime = _shadowJointsHelper.ExistsCollisionBetween(from, to, targetInteractable.ColliderGroup);
                    if (touchTime > 0)
                    {
                        fingerStatus.Touching = true;
                        _shadowJointsHelper.GetInterpolatedLocalRotations(from, to, touchTime, fingerStatus.JointLockRotations);
                        Quaternion[] targetRotations = fingerStatus.JointLockRotations;
                        fingerStatus.JointLockRotations = targetRotations;
                    }
                    continue;
                }

                if (_shadowJointsHelper.ExistsCollisionFor(to, targetInteractable.ColliderGroup))
                {
                    from.SetWristJointFromHand(Hand);
                    from.SetLocalFingerJointsFromHand(Hand);
                    Quaternion[] rotations = fingerStatus.JointLockRotations;
                    _shadowJointsHelper.GetInterpolatedLocalRotations(from, to, 1f, rotations);
                    if (i == 0)
                    {
                        for (int j = 0; j < THUMB_GRAB_ROTATIONS.Length; j++)
                        {
                            rotations[j] = THUMB_GRAB_ROTATIONS[j];
                        }
                    }
                    else
                    {
                        for (int j = 0; j < rotations.Length; j++)
                        {
                            Vector3 angles = rotations[j].eulerAngles;
                            angles.z = Mathf.Min(angles.z, MIN_FINGER_ROTATION_ON_Z);
                            rotations[j] = Quaternion.Euler(angles);
                        }
                    }

                    from.SetLocalFingerJointsDirectly(rotations);
                    float touchTime = _shadowJointsHelper.ExistsCollisionBetween(from, to, targetInteractable.ColliderGroup);
                    if (touchTime > 0)
                    {
                        fingerStatus.Touching = true;
                        _shadowJointsHelper.GetInterpolatedWorldPositions(from, to, touchTime, fingerStatus.JointLockPositions);
                        Vector3[] targetPositions = fingerStatus.JointLockPositions;

                        _shadowJointsHelper.GetInterpolatedLocalRotations(from, to, touchTime, fingerStatus.JointLockRotations);
                        Quaternion[] targetRotations = fingerStatus.JointLockRotations;

                        if (!HistoryHand.GetJointPosesLocal(out ReadOnlyHandJointPoses localJoints))
                        {
                            fingerStatus.Touching = false;
                            return;
                        }
                        if (i != 0)
                        {
                            HandJointId[] handJointIds = FINGER_JOINT_IDS[(int)fingerStatus.HandFinger];
                            for (int j = 1; j < targetRotations.Length; j++)
                            {
                                HandJointId jointId = handJointIds[j];
                                float historyZ = localJoints[jointId].rotation.eulerAngles.z;
                                float targetZ = targetRotations[j - 1].eulerAngles.z;
                                if (targetZ - 7f > historyZ)
                                {
                                    fingerStatus.Touching = false;
                                    break;
                                }
                                else if (!HistoryHand.GetJointPose(jointId, out Pose poseWorld))
                                {
                                    fingerStatus.Touching = false;
                                    break;
                                }
                            }
                        }

                        if (fingerStatus.Touching)
                        {
                            fingerStatus.JointLockRotations = targetRotations;
                        }
                    }
                }
            }
        }

        protected override void DoHoverUpdate()
        {
            TouchHandGrabInteractable closestInteractable = _interactable;
            if (closestInteractable == null) return;

            PushOutTouching(closestInteractable);

            if (!_fingerStatuses[0].Touching)
            {
                return;
            }
            bool fingerTouching = false;
            int firstLockedFinger = -1;
            for (int i = 1; i < Constants.NUM_FINGERS; i++)
            {
                if (_fingerStatuses[i].Touching)
                {
                    fingerTouching = true;
                    firstLockedFinger = i;
                    break;
                }
            }

            if (!fingerTouching)
            {
                return;
            }
            ShouldSelect = true;

            // Save touch locations
            for (int j = 0; j < Constants.NUM_FINGERS; j++)
            {
                FingerStatus fingerStatus = _fingerStatuses[j];
                if (!fingerStatus.Touching)
                {
                    continue;
                }

                LockFinger(fingerStatus);
            }

            for (int j = 0; j < Constants.NUM_FINGERS; j++)
            {
                FingerStatus fingerStatus = _fingerStatuses[j];
                if (!fingerStatus.Locked)
                {
                    continue;
                }

                fingerStatus.Locked = true;

                int otherFinger = j == 0 ? firstLockedFinger : 0;
                ComputeReleaseThresholdRelativeTo(fingerStatus, _fingerStatuses[otherFinger]);
            }
        }

        private void LockFinger(FingerStatus fingerStatus)
        {
            ShadowJointsHelper.ShadowJoints from = fingerStatus.ShadowFrom;
            ShadowJointsHelper.ShadowJoints to = fingerStatus.ShadowTo;
            fingerStatus.Locked = true;

            from.SetWristJointFromHand(Hand);
            from.SetLocalFingerJointsDirectly(fingerStatus.JointLockRotations);
            Vector3 worldPosition =
                _shadowJointsHelper.GetWorldPosition(fingerStatus.FingerTipIndex, from);
            fingerStatus.LocalLockPosition =
                _shadowJointsHelper.GetPositionRelativeToRoot(worldPosition, from);
        }

        private void ComputeReleaseThresholdRelativeTo(FingerStatus fingerStatus, FingerStatus otherFinger)
        {
            Vector3 halfDelta = 0.5f * (fingerStatus.LocalLockPosition - otherFinger.LocalLockPosition);
            fingerStatus.CenterAtLockTime = otherFinger.LocalLockPosition + halfDelta;
            fingerStatus.DistanceToCenterAtLockTime = halfDelta.magnitude;
            fingerStatus.CurlValueAtLockTime = FingerCurl(fingerStatus.HandFinger);
        }

        protected override void DoSelectUpdate()
        {
            TouchHandGrabInteractable interactable = _selectedInteractable;
            if (interactable == null)
            {
                // If there is no longer a selected interactable (as in the case of a transferred interactable),
                // look to move beyond the minimum threshold on the previously locked fingers to register a release
                int firstLockedFinger = -1;
                for (int i = 0; i < _fingerStatuses.Length; i++)
                {
                    FingerStatus fingerStatus = _fingerStatuses[i];
                    if (!fingerStatus.Locked)
                    {
                        continue;
                    }
                    ShadowJointsHelper.ShadowJoints from = fingerStatus.ShadowFrom;
                    from.SetWristJointFromHand(Hand);
                    Vector3 worldPosition =
                        GetJointPose(fingerStatus.FingerTipJointId).position;
                        fingerStatus.LocalLockPosition =
                            _shadowJointsHelper.GetPositionRelativeToRoot(worldPosition, from);
                    if (i > 0 && firstLockedFinger < 0)
                    {
                        firstLockedFinger = i;
                    }
                }

                bool touchingThumb = false;
                bool touchingFinger = false;
                for (int i = 0; i < _fingerStatuses.Length; i++)
                {
                    FingerStatus fingerStatus = _fingerStatuses[i];
                    if (fingerStatus.Locked)
                    {
                        fingerStatus.Locked = false;
                        fingerStatus.Touching = true;

                        int otherIndex = i == 0 ? firstLockedFinger : 0;
                        ComputeReleaseThresholdRelativeTo(fingerStatus, _fingerStatuses[otherIndex]);
                    }

                    if (fingerStatus.Touching)
                    {
                        if (!FingerWithinThreshold(fingerStatus))
                        {
                            fingerStatus.Touching = false;
                        }
                        else
                        {
                            if (i == 0)
                            {
                                touchingThumb = true;
                            }
                            else
                            {
                                touchingFinger = true;
                            }
                        }
                    }
                }

                if (!touchingThumb || !touchingFinger)
                {
                    ShouldUnselect = true;
                }

                return;
            }

            bool lockedFinger = false;
            bool lockedThumb = false;

            // Update the existing fingers to check for new lock candidates
            PushOutTouching(interactable);

            bool fingerWasLocked = false;
            for (int i = 0; i < Constants.NUM_FINGERS; i++)
            {
                FingerStatus fingerStatus = _fingerStatuses[i];

                // If previously locked, check if we can unlock this finger
                if (fingerStatus.Locked)
                {
                    if (FingerWithinThreshold(fingerStatus))
                    {
                        if (i == 0)
                        {
                            lockedThumb = true;
                        }
                        else
                        {
                            lockedFinger = true;
                        }
                    }
                    else
                    {
                        _fingerStatuses[i].Locked = false;
                    }
                }

                // If not previously locked and now touching, lock this finger too
                else if (fingerStatus.Touching)
                {
                    LockFinger(fingerStatus);

                    FingerStatus thumbStatus = _fingerStatuses[0];
                    if (thumbStatus.Locked)
                    {
                        ComputeReleaseThresholdRelativeTo(fingerStatus, thumbStatus);
                    }
                    fingerWasLocked = true;
                }
            }

            if (fingerWasLocked)
            {
                WhenFingerLocked();
            }

            if (!lockedFinger || !lockedThumb)
            {
                // If no fingers are locked, force release
                for (int j = 0; j < Constants.NUM_FINGERS; j++)
                {
                    _fingerStatuses[j].Locked = false;
                }

                ShouldUnselect = true;
            }
        }

        private float FingerCurl(HandFinger finger)
        {
            return _fingerShapes.GetCurlValue(finger, Hand) +
                   _fingerShapes.GetFlexionValue(finger, Hand);
        }

        private bool FingerWithinThreshold(FingerStatus fingerStatus)
        {
            ShadowJointsHelper.ShadowJoints from = fingerStatus.ShadowFrom;
            ShadowJointsHelper.ShadowJoints to = fingerStatus.ShadowTo;

            float curl = FingerCurl(fingerStatus.HandFinger);
            if (curl > fingerStatus.CurlValueAtLockTime)
            {
                return true;
            }

            from.SetWristJointFromHand(Hand);
            Vector3 worldPosition = GetJointPose(fingerStatus.FingerTipJointId).position;
            Vector3 currPositionRelative = _shadowJointsHelper.GetPositionRelativeToRoot(worldPosition, from);
            Vector3 delta = currPositionRelative - fingerStatus.CenterAtLockTime;

            return delta.magnitude < fingerStatus.DistanceToCenterAtLockTime + _releaseThreshold;
        }

        public override void Unselect()
        {
            if (!ShouldUnselect)
            {
                base.Unselect();
                return;
            }
            for (int i = 0; i < _fingerStatuses.Length; i++)
            {
                _fingerStatuses[i].Locked = false;
            }
            base.Unselect();
        }

        protected override TouchHandGrabInteractable ComputeCandidate()
        {
            TouchHandGrabInteractable closest = null;
            float minSqrDist = float.MaxValue;
            foreach (TouchHandGrabInteractable interactable in TouchHandGrabInteractable.Registry
                .List())
            {
                foreach (Collider collider in interactable.ColliderGroup.Colliders)
                {
                    Vector3 closestPoint = collider.ClosestPoint(_hoverLocation.position);
                    float sqrDist = (closestPoint - _hoverLocation.position).sqrMagnitude;
                    if (sqrDist < minSqrDist && sqrDist < _minHoverDistance * _minHoverDistance)
                    {
                        minSqrDist = sqrDist;
                        closest = interactable;
                    }
                }
            }

            return closest;
        }

        public Quaternion[] GetLockedFingerRotations(int i)
        {
            return _fingerStatuses[i].JointLockRotations;
        }

        protected override void OnDestroy()
        {
            if(_shadowJointsHelper != null)
            {
                Destroy(_shadowJointsHelper.gameObject);
            }
            base.OnDestroy();
        }

        protected override Pose ComputePointerPose()
        {
            return new Pose(GrabPosition, GrabRotation);
        }

        #region Inject

        public void InjectAllTouchHandGrabInteractor(
            Transform hoverLocation,
            Transform grabLocation,
            IHand hand,
            JointRotationHistoryHand historyHand)
        {
            InjectHoverLocation(hoverLocation);
            InjectGrabLocation(grabLocation);
            InjectHand(hand);
            InjectHistoryHand(historyHand);
        }

        public void InjectHoverLocation(Transform hoverLocation)
        {
            _hoverLocation = hoverLocation;
        }

        public void InjectGrabLocation(Transform grabLocation)
        {
            _grabLocation = grabLocation;
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as MonoBehaviour;
            Hand = hand;
        }

        public void InjectHistoryHand(JointRotationHistoryHand historyHand)
        {
            _historyHand = historyHand;
        }

        public void InjectOptionalMinHoverDistance(float minHoverDistance)
        {
            _minHoverDistance = minHoverDistance;
        }

        public void InjectOptionalIterations(int iterations)
        {
            _iterations = iterations;
        }

        #endregion
    }
}
