/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace Oculus.Interaction.Input
{
    /// <summary>
    /// Data Modifier used to apply the One Euro Filter to a hand pose.
    /// Filtering can be applied to the wrist and any number of finger joints.
    /// </summary>
    public class OneEuroFilterRotationHand : Hand
    {
        [Header("Wrist")]
        [SerializeField]
        private bool _wristFilterEnabled = true;

        [SerializeField]
        private OneEuroFilterPropertyBlock _wristFilterProperties =
                                           new OneEuroFilterPropertyBlock(2f, 3f);

        [Header("Fingers")]
        [SerializeField]
        private bool _fingerFiltersEnabled = true;

        [SerializeField]
        private HandFingerJointFlags _fingerJoints = HandFingerJointFlags.None;

        [SerializeField]
        private OneEuroFilterPropertyBlock _fingerFilterProperties =
                                           new OneEuroFilterPropertyBlock(1f, 2f);

        private IOneEuroFilter<Quaternion> _wristFilter;
        private List<JointFilter> _fingerJointFilters;
        private HandDataAsset _lastFiltered;
        private int _lastFrameUpdated;

        protected override void Start()
        {
            base.Start();

            InitFingerJointFilters();

            _lastFrameUpdated = 0;
            _lastFiltered = new HandDataAsset();
            _wristFilter = OneEuroFilter.CreateQuaternion();
        }

        private void InitFingerJointFilters()
        {
            _fingerJointFilters = new List<JointFilter>();
            if (_fingerJoints == HandFingerJointFlags.None)
            {
                return;
            }

            foreach (var jointId in HandJointUtils.JointIds)
            {
                HandFingerJointFlags jointFlag = (HandFingerJointFlags)(1 << (int)jointId);
                if (!Enum.IsDefined(typeof(HandFingerJointFlags), jointFlag))
                {
                    continue;
                }
                if (_fingerJoints.HasFlag(jointFlag))
                {
                    _fingerJointFilters.Add(new JointFilter(jointId));
                }
            }
        }

        #region IHandInputDataModifier Implementation
        protected override void Apply(HandDataAsset handDataAsset)
        {
            if (!handDataAsset.IsTracked)
            {
                return;
            }

            Profiler.BeginSample($"{nameof(OneEuroFilterRotationHand)}." +
                                 $"{nameof(OneEuroFilterRotationHand.Apply)}");

            if (Time.frameCount > _lastFrameUpdated)
            {
                _lastFrameUpdated = Time.frameCount;
                ApplyFilters(handDataAsset);
                _lastFiltered.CopyFrom(handDataAsset);
            }
            else
            {
                handDataAsset.CopyFrom(_lastFiltered);
            }

            handDataAsset.RootPoseOrigin = PoseOrigin.FilteredTrackedPose;

            Profiler.EndSample();
        }
        #endregion

        private void ApplyFilters(HandDataAsset handDataAsset)
        {
            if (_wristFilterEnabled)
            {
                Pose rootPose = handDataAsset.Root;
                _wristFilter.SetProperties(_wristFilterProperties);
                rootPose.rotation = _wristFilter.Step(rootPose.rotation, Time.fixedDeltaTime);
                handDataAsset.Root = rootPose;
            }

            if (_fingerFiltersEnabled)
            {
                foreach (var joint in _fingerJointFilters)
                {
                    joint.Filter.SetProperties(_fingerFilterProperties);
                    handDataAsset.Joints[(int)joint.JointId] =
                        joint.Filter.Step(handDataAsset.Joints[(int)joint.JointId], Time.fixedDeltaTime);
                }
            }
        }

        private class JointFilter
        {
            public HandJointId JointId => _jointId;
            public IOneEuroFilter<Quaternion> Filter => _filter;

            private readonly HandJointId _jointId;
            private readonly IOneEuroFilter<Quaternion> _filter;

            public JointFilter(HandJointId jointId)
            {
                _jointId = jointId;
                _filter = OneEuroFilter.CreateQuaternion();
            }
        }

        #region Inject
        public void InjectAllOneEuroFilterRotationDataModifier(UpdateModeFlags updateMode, IDataSource updateAfter,
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
