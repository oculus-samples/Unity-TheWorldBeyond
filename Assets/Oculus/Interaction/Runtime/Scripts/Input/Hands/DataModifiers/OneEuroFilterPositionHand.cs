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
using UnityEngine.Profiling;

namespace Oculus.Interaction.Input
{
    public class OneEuroFilterPositionHand : Hand
    {
        [Header("Wrist")]
        [SerializeField]
        private OneEuroFilterPropertyBlock _wristFilterProperties =
                                           new OneEuroFilterPropertyBlock(2f, 10f);

        private IOneEuroFilter<Vector3> _wristFilter;
        private int _lastFrameUpdated;
        private Pose _lastSmoothedPose;

        protected override void Start()
        {
            base.Start();

            _lastFrameUpdated = 0;
            _wristFilter = OneEuroFilter.CreateVector3();
        }

        protected override void Apply(HandDataAsset handDataAsset)
        {
            if (!handDataAsset.IsTracked)
            {
                return;
            }

            Profiler.BeginSample($"{nameof(OneEuroFilterPositionHand)}." +
                                 $"{nameof(OneEuroFilterPositionHand.Apply)}");

            if (Time.frameCount > _lastFrameUpdated)
            {
                _lastFrameUpdated = Time.frameCount;
                _lastSmoothedPose = ApplyFilter(handDataAsset.Root);
            }

            handDataAsset.Root = _lastSmoothedPose;
            handDataAsset.RootPoseOrigin = PoseOrigin.FilteredTrackedPose;

            Profiler.EndSample();
        }

        private Pose ApplyFilter(Pose pose)
        {
            _wristFilter.SetProperties(_wristFilterProperties);
            pose.position = _wristFilter.Step(pose.position, Time.fixedDeltaTime);
            return pose;
        }

        #region Inject
        public void InjectAllOneEuroFilterPositionDataModifier(UpdateModeFlags updateMode, IDataSource updateAfter,
            DataModifier<HandDataAsset> modifyDataFromSource, bool applyModifier,
            Component[] aspects, OneEuroFilterPropertyBlock wristFilterProperties)
        {
            base.InjectAllHand(updateMode, updateAfter, modifyDataFromSource, applyModifier, aspects);
            InjectWristFilterProperties(wristFilterProperties);
        }

        public void InjectWristFilterProperties(OneEuroFilterPropertyBlock wristFilterProperties)
        {
            _wristFilterProperties = wristFilterProperties;
        }
        #endregion
    }
}
