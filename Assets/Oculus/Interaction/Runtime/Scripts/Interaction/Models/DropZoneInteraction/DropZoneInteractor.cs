/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction
{
    /// <summary>
    /// The DropZoneInteractor referes to an element that can snap to a DropZoneInteractable.
    /// This interactor moves itself into the Pose specified by the intereactable.
    /// Additionally, it can specify a preferred DropZoneInteractable and a TimeOut time, and it
    /// will automatically snap there if its Pointable element has not been used (hovered, selected)
    /// for a certain time.
    /// </summary>
    public class DropZoneInteractor : Interactor<DropZoneInteractor, DropZoneInteractable>,
        IRigidbodyRef
    {
        [SerializeField]
        private PointableElement _pointableElement;

        [SerializeField]
        private Rigidbody _rigidbody;
        public Rigidbody Rigidbody => _rigidbody;

        [SerializeField, Optional]
        private Transform _dropPoint;
        public Pose DropPoint => _dropPoint.GetPose();

        [Header("Time out")]
        [SerializeField, Optional]
        private DropZoneInteractable _timeOutInteractable;
        [SerializeField, Optional]
        private float _timeOut = 0f;

        private float _idleStarted = -1f;
        private IMovement _movement;

        #region Editor events
        private void Reset()
        {
            _rigidbody = this.GetComponentInParent<Rigidbody>();
            _pointableElement = this.GetComponentInParent<PointableElement>();
        }
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            _pointableElement = _pointableElement as PointableElement;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, base.Start);
            Assert.IsNotNull(_pointableElement);
            Assert.IsNotNull(Rigidbody);
            if (_dropPoint == null)
            {
                _dropPoint = this.transform;
            }

            this.EndStart(ref _started);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_started)
            {
                _pointableElement.WhenPointerEventRaised += HandlePointerEventRaised;
            }
        }

        protected override void OnDisable()
        {
            if (_started)
            {
                _pointableElement.WhenPointerEventRaised -= HandlePointerEventRaised;
            }
            base.OnDisable();
        }

        #endregion

        #region Interactor Lifecycle

        protected override void DoHoverUpdate()
        {
            base.DoHoverUpdate();

            if (Interactable == null)
            {
                return;
            }

            Interactable.InteractorHoverUpdated(this);

            if (_pointableElement.SelectingPointsCount == 0)
            {
                ShouldSelect = true;
            }
        }

        protected override void DoSelectUpdate()
        {
            base.DoSelectUpdate();

            if (Interactable != null)
            {
                if (Interactable.PoseForInteractor(this, out Pose targetPose))
                {
                    _movement.UpdateTarget(targetPose);
                    _movement.Tick();
                    GeneratePointerEvent(PointerEvent.Move);
                }
                else
                {
                    ShouldUnselect = true;
                }
            }

            if (_pointableElement.SelectingPointsCount > 1)
            {
                ShouldUnselect = true;
            }
        }

        protected override void InteractableSet(DropZoneInteractable interactable)
        {
            base.InteractableSet(interactable);
            if (interactable != null)
            {
                GeneratePointerEvent(PointerEvent.Hover);
            }
        }

        protected override void InteractableUnset(DropZoneInteractable interactable)
        {
            if (interactable != null)
            {
                GeneratePointerEvent(PointerEvent.Unhover);
            }
            base.InteractableUnset(interactable);
        }

        protected override void InteractableSelected(DropZoneInteractable interactable)
        {
            base.InteractableSelected(interactable);
            if (interactable != null)
            {
                _movement = interactable.GenerateMovement(_dropPoint.GetPose(), this);
                if (_movement != null)
                {
                    GeneratePointerEvent(PointerEvent.Select);
                }
                else
                {
                    ShouldUnselect = true;
                }
            }
        }

        protected override void InteractableUnselected(DropZoneInteractable interactable)
        {
            _movement?.StopAndSetPose(_movement.Pose);
            if (interactable != null)
            {
                GeneratePointerEvent(PointerEvent.Unselect);
            }
            base.InteractableUnselected(interactable);
            _movement = null;
        }

        #endregion

        #region Pointable

        protected virtual void HandlePointerEventRaised(PointerArgs args)
        {
            if (_pointableElement.PointsCount == 0
                && (args.PointerEvent == PointerEvent.Cancel
                    || args.PointerEvent == PointerEvent.Unhover
                    || args.PointerEvent == PointerEvent.Unselect))
            {
                _idleStarted = Time.realtimeSinceStartup;
            }
            else
            {
                _idleStarted = -1f;
            }

            if (args.Identifier == Identifier
                && args.PointerEvent == PointerEvent.Cancel
                && Interactable != null)
            {
                Interactable.RemoveInteractorById(Identifier);
                ShouldUnselect = true;
            }
        }


        public void GeneratePointerEvent(PointerEvent pointerEvent, Pose pose)
        {
            _pointableElement.ProcessPointerEvent(new PointerArgs(Identifier, pointerEvent, pose));
        }

        private void GeneratePointerEvent(PointerEvent pointerEvent)
        {
            Pose pose = ComputePointerPose();
            _pointableElement.ProcessPointerEvent(new PointerArgs(Identifier, pointerEvent, pose));
        }

        protected Pose ComputePointerPose()
        {
            if (_movement != null)
            {
                return _movement.Pose;
            }

            return DropPoint;
        }
        #endregion

        private bool TimedOut()
        {
            return _timeOut >= 0f
                && _idleStarted >= 0f
                && Time.timeSinceLevelLoad - _idleStarted > _timeOut;
        }

        protected override DropZoneInteractable ComputeCandidate()
        {
            DropZoneInteractable interactable = ComputeIntersectingCandidate();
            if (TimedOut())
            {
                return interactable != null ? interactable : _timeOutInteractable;
            }
            return interactable;
        }

        private DropZoneInteractable ComputeIntersectingCandidate()
        {
            DropZoneInteractable closestInteractable = null;
            float bestScore = float.MinValue;
            float score;

            IEnumerable<DropZoneInteractable> interactables = DropZoneInteractable.Registry.List(this);
            foreach (DropZoneInteractable interactable in interactables)
            {
                Collider[] colliders = interactable.Colliders;
                foreach (Collider collider in colliders)
                {
                    if (Collisions.IsPointWithinCollider(Rigidbody.transform.position, collider))
                    {
                        float sqrDistanceFromCenter =
                            (Rigidbody.transform.position - collider.bounds.center).magnitude;
                        score = float.MaxValue - sqrDistanceFromCenter;
                    }
                    else
                    {
                        var position = Rigidbody.transform.position;
                        Vector3 closestPointOnInteractable = collider.ClosestPoint(position);
                        score = -1f * (position - closestPointOnInteractable).magnitude;
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        closestInteractable = interactable;
                    }
                }
            }
            return closestInteractable;
        }

        #region Inject

        public void InjectAllDropZoneInteractor(PointableElement pointableElement, Rigidbody rigidbody)
        {
            InjectPointableElement(pointableElement);
            InjectRigidbody(rigidbody);
        }

        public void InjectPointableElement(PointableElement pointableElement)
        {
            _pointableElement = pointableElement;
        }

        public void InjectRigidbody(Rigidbody rigidbody)
        {
            _rigidbody = rigidbody;
        }

        public void InjectOptionalDropPoint(Transform dropPoint)
        {
            _dropPoint = dropPoint;
        }

        public void InjectOptionalTimeOutInteractable(DropZoneInteractable interactable)
        {
            _timeOutInteractable = interactable;
        }

        public void InjectOptionaTimeOut(float timeOut)
        {
            _timeOut = timeOut;
        }
        #endregion
    }
}
