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
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction
{
    /// <summary>
    /// The SnapZoneInteractable , or simply SnapZone, specifies a volume in space in which
    /// SnapZoneInteractors can snap to. How the slots are organised is configured via a custom
    /// ISnapZonePoseProvider. If none is provided the SnapZone has one single slot at its own transform.
    /// </summary>
    public class DropZoneInteractable : Interactable<DropZoneInteractor, DropZoneInteractable>,
        IRigidbodyRef
    {
        [SerializeField, Optional, Interface(typeof(IMovement))]
        private MonoBehaviour _movementProvider;
        private IMovementProvider MovementProvider { get; set; }

        [SerializeField, Optional, Interface(typeof(IDropZoneSlotsProvider))]
        private MonoBehaviour _slotsProvider;
        private IDropZoneSlotsProvider SlotsProvider { get; set; }

        [SerializeField]
        private Rigidbody _rigidbody;
        public Rigidbody Rigidbody => _rigidbody;

        private Collider[] _colliders;
        public Collider[] Colliders => _colliders;

        private bool _started;

        private static CollisionInteractionRegistry<DropZoneInteractor, DropZoneInteractable> _registry = null;

        #region Editor events
        private void Reset()
        {
            _rigidbody = this.GetComponentInParent<Rigidbody>();
        }
        #endregion

        protected virtual void Awake()
        {
            MovementProvider = _movementProvider as IMovementProvider;
            SlotsProvider = _slotsProvider as IDropZoneSlotsProvider;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(Rigidbody);
            _colliders = Rigidbody.GetComponentsInChildren<Collider>();
            if (_registry == null)
            {
                _registry = new CollisionInteractionRegistry<DropZoneInteractor, DropZoneInteractable>();
                SetRegistry(_registry);
            }
            if (MovementProvider == null)
            {
                FollowTargetProvider movementProvider = this.gameObject.AddComponent<FollowTargetProvider>();
                InjectOptionalMovementProvider(movementProvider);
            }
            this.EndStart(ref _started);
        }

        protected override void InteractorAdded(DropZoneInteractor interactor)
        {
            base.InteractorAdded(interactor);
            if (SlotsProvider != null)
            {
                SlotsProvider.TrackInteractor(interactor);
            }
        }

        protected override void InteractorRemoved(DropZoneInteractor interactor)
        {
            base.InteractorRemoved(interactor);
            if (SlotsProvider != null)
            {
                SlotsProvider.UntrackInteractor(interactor);
            }
        }

        public void InteractorHoverUpdated(DropZoneInteractor interactor)
        {
            if (SlotsProvider != null)
            {
                SlotsProvider.UpdateTrackedInteractor(interactor);
            }
        }

        public bool PoseForInteractor(DropZoneInteractor interactor, out Pose slot)
        {
            if (SlotsProvider != null)
            {
                return SlotsProvider.PoseForInteractor(interactor, out slot);
            }

            slot = this.transform.GetPose();
            return true;
        }

        public IMovement GenerateMovement(in Pose from, DropZoneInteractor interactor)
        {
            if (PoseForInteractor(interactor, out Pose to))
            {
                IMovement movement = MovementProvider.CreateMovement();
                movement.StopAndSetPose(from);
                movement.MoveTo(to);
                return movement;
            }
            return null;
        }

        #region Inject
        public void InjectAllSnapZoneInteractable(Rigidbody rigidbody)
        {
            InjectRigidbody(rigidbody);
        }

        public void InjectRigidbody(Rigidbody rigidbody)
        {
            _rigidbody = rigidbody;
        }

        public void InjectOptionalMovementProvider(IMovementProvider provider)
        {
            _movementProvider = provider as MonoBehaviour;
            MovementProvider = provider;
        }

        public void InjectOptionalPoseProvider(IDropZoneSlotsProvider slotsProvider)
        {
            _slotsProvider = slotsProvider as MonoBehaviour;
            SlotsProvider = slotsProvider;
        }

        #endregion
    }
}
