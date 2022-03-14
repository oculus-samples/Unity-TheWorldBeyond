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

namespace Oculus.Interaction.HandPosing
{
    /// <summary>
    /// All the relevant data needed for a snapping position.
    /// This includes the HandGrabInteractor to (which can have a volume)
    /// and the best Pose within that volume if any available
    /// </summary>
    public class SnapAddress<TSnappable> : ISnapData
        where TSnappable : class, ISnappable
    {
        public TSnappable Interactable { get; set; }
        public ref Pose SnapPoint => ref _snapPoint;
        public bool IsValidAddress => Interactable != null;

        public HandPose HandPose => _isHandPoseValid ? _handPose : null;

        public Pose WorldSnapPose => Interactable.RelativeTo.GlobalPose(SnapPoint);
        public HandAlignType HandAlignment => Interactable != null ?
            Interactable.HandAlignment : HandAlignType.None;

        public bool SnappedToPinch { get; private set; }

        private bool _isHandPoseValid = false;
        private HandPose _handPose = new HandPose();
        private Pose _snapPoint;

        public void Set(SnapAddress<TSnappable> other)
        {
            Set(other.Interactable, other.HandPose, other._snapPoint, other.SnappedToPinch);
        }

        public void Set(TSnappable snapInteractable, HandPose pose, in Pose snapPoint, bool usedPinchPoint)
        {
            Interactable = snapInteractable;
            SnappedToPinch = usedPinchPoint;

            _snapPoint.CopyFrom(snapPoint);
            _isHandPoseValid = pose != null;
            if (_isHandPoseValid)
            {
                _handPose.CopyFrom(pose);
            }
        }

        public void Clear()
        {
            Interactable = null;
            SnappedToPinch = false;
            _isHandPoseValid = false;
        }

        public static bool IsNullOrInvalid(SnapAddress<TSnappable> snapAddress)
        {
            return snapAddress == null || !snapAddress.IsValidAddress;
        }
    }
}
