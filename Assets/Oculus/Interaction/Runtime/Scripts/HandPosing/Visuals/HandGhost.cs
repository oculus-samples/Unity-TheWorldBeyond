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

namespace Oculus.Interaction.HandPosing.Visuals
{
    /// <summary>
    /// A static (non-user controlled) representation of a hand. This script is used
    /// to be able to manually visualize hand grab poses.
    /// </summary>
    [RequireComponent(typeof(HandPuppet))]
    public class HandGhost : MonoBehaviour
    {
        /// <summary>
        /// The puppet is used to actually move the representation of the hand.
        /// </summary>
        [SerializeField]
        private HandPuppet _puppet;

        /// <summary>
        /// The GripPoint of the hand. Needs to be
        /// at the same position/rotation as the gripPoint used
        /// by the visual HandPuppet controlled by the user.
        /// </summary>
        [SerializeField]
        private Transform _gripPoint;

        /// <summary>
        /// The HandGrab point can be set so the ghost automatically
        /// adopts the desired pose of said point.
        /// </summary>
        [SerializeField, Optional]
        private HandGrabPoint _handGrabPoint;

        #region editor events
        protected virtual void Reset()
        {
            _puppet = this.GetComponent<HandPuppet>();
            _handGrabPoint = this.GetComponentInParent<HandGrabPoint>();
        }

        protected virtual void OnValidate()
        {
            if (_puppet == null
                || _gripPoint == null)
            {
                return;
            }

            if (_handGrabPoint == null)
            {
                HandGrabPoint point = this.GetComponentInParent<HandGrabPoint>();
                if (point != null)
                {
                    SetPose(point);
                }
            }
            else if (_handGrabPoint != null)
            {
                SetPose(_handGrabPoint);
            }
        }
        #endregion

        protected virtual void Start()
        {
            Assert.IsNotNull(_puppet);
            Assert.IsNotNull(_gripPoint);
        }

        /// <summary>
        /// Relay to the Puppet to set the ghost hand to the desired static pose
        /// </summary>
        /// <param name="handGrabPoint">The point to read the HandPose from</param>
        public void SetPose(HandGrabPoint handGrabPoint)
        {
            HandPose userPose = handGrabPoint.HandPose;
            if (userPose == null)
            {
                return;
            }

            Transform relativeTo = handGrabPoint.RelativeTo;
            _puppet.SetJointRotations(userPose.JointRotations);
            SetGripPose(handGrabPoint.RelativeGrip, relativeTo);
        }

        /// <summary>
        /// Moves the underlying puppet so the grip point aligns with the given parameters
        /// </summary>
        /// <param name="gripPose">The relative grip pose to align the hand to</param>
        /// <param name="relativeTo">The object to use as anchor</param>
        public void SetGripPose(Pose gripPose, Transform relativeTo)
        {
            Pose inverseGrip = _gripPoint.RelativeOffset(this.transform);
            gripPose.Premultiply(inverseGrip);
            gripPose.Postmultiply(relativeTo.GetPose());
            _puppet.SetRootPose(gripPose);
        }

        #region Inject
        public void InjectHandPuppet(HandPuppet puppet)
        {
            _puppet = puppet;
        }
        public void InjectGripPoint(Transform gripPoint)
        {
            _gripPoint = gripPoint;
        }
        public void InjectOptionalHandGrabPoint(HandGrabPoint handGrabPoint)
        {
            _handGrabPoint = handGrabPoint;
        }

        public void InjectAllHandGhost(HandPuppet puppet, Transform gripPoint)
        {
            InjectHandPuppet(puppet);
            InjectGripPoint(gripPoint);
        }
        #endregion
    }
}
