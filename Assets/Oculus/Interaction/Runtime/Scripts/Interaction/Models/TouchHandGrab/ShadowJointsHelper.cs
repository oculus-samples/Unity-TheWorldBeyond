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
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// Used in conjunction with TouchGrabHandInteractor for detecting collisions with approximated
    /// sphere sweeps between a set of start joint and end joint poses
    /// </summary>
    public class ShadowJointsHelper : MonoBehaviour
    {
        public class ShadowJoints
        {
            private HandJointId[] _jointIds;
            public Pose[] Poses { get; }
            public Vector3 Scale { get; private set; }
            public int NumJoints => _jointIds.Length;

            public ShadowJoints(HandJointId[] jointIds)
            {
                _jointIds = jointIds;
                Poses = new Pose[_jointIds.Length];
            }

            public void SetLocalFingerJointsFromHand(IHand hand)
            {
                if (!hand.GetJointPosesLocal(out ReadOnlyHandJointPoses localJoints))
                {
                    return;
                }
                for (int i = 1; i < Poses.Length; i++)
                {
                    Poses[i].CopyFrom(localJoints[_jointIds[i]]);
                }
            }

            public void SetLocalFingerJointsDirectly(Quaternion[] jointLockRotations)
            {
                for (int i = 1; i < Poses.Length; i++)
                {
                    Poses[i].rotation = jointLockRotations[i - 1];
                }
            }

            public void SetWristJointFromHand(IHand hand)
            {
                if (!hand.GetJointPose(_jointIds[0], out Pose jointPose)) return;
                Poses[0] = jointPose;
                Scale = hand.TrackingToWorldSpace.localScale * hand.Scale;
            }
        }

        [SerializeField]
        private int _startJointIndexForCollisions = 1;

        [SerializeField]
        private float _fingerRadius = 0.005f;

        [SerializeField]
        private int _iterations = 10;

        public int Iterations
        {
            get
            {
                return _iterations;
            }
            set
            {
                _iterations = Math.Max(1, value);
            }
        }

        private List<Transform> _resultTransforms;

        protected virtual void Start()
        {
            _resultTransforms = new List<Transform>();
        }

        private void UpdateResultTransforms(ShadowJoints from, ShadowJoints to, float t)
        {
            while (_resultTransforms.Count < from.NumJoints)
            {
                Transform parent = _resultTransforms.Count == 0
                ? transform
                : _resultTransforms[_resultTransforms.Count - 1];

                GameObject go = new GameObject("ShadowJoint");
                _resultTransforms.Add(go.transform);
                go.transform.parent = parent;
            }

            _resultTransforms[0].localScale = from.Scale;
            _resultTransforms[0].position =
                Vector3.Lerp(from.Poses[0].position, to.Poses[0].position, t);

            _resultTransforms[0].rotation =
                Quaternion.Slerp(from.Poses[0].rotation, to.Poses[0].rotation, t);

            for (int i = 1; i < from.NumJoints; i++)
            {
                Vector3 position = Vector3.Lerp(from.Poses[i].position, to.Poses[i].position, t);
                Quaternion rotation =
                    Quaternion.Slerp(from.Poses[i].rotation, to.Poses[i].rotation, t);

                if (i == 0)
                {
                    _resultTransforms[0].position = position;
                    _resultTransforms[0].rotation = rotation;
                }
                else
                {
                    _resultTransforms[i].localPosition = position;
                    _resultTransforms[i].localRotation = rotation;
                }
            }
        }

        public void GetInterpolatedWorldPositions(ShadowJoints from, ShadowJoints to, float t, Vector3[] positions)
        {
            UpdateResultTransforms(from, to, t);
            for (int i = 1; i < from.NumJoints; i++)
            {
                positions[i - 1] = _resultTransforms[i].position;
            }
        }

        public void GetInterpolatedLocalRotations(ShadowJoints from, ShadowJoints to, float t, Quaternion[] rotations)
        {
            UpdateResultTransforms(from, to, t);
            for (int i = 1; i < from.NumJoints; i++)
            {
                rotations[i - 1] = _resultTransforms[i].localRotation;
            }
        }

        private bool ExistsCollision(ColliderGroup colliderGroup, Vector3[] positions, int lastJointIndex)
        {
            // Test collisions segment-wise using from joints starting with the start joint
            // The last from joint we check is the second-last joint.
            for (int i = _startJointIndexForCollisions - 1; i < lastJointIndex - 1; i++)
            {
                Vector3 halfDelta = (positions[i + 1] - positions[i]) * 0.5f;
                Vector3 midPoint = positions[i] + halfDelta;
                float radius = halfDelta.magnitude + _fingerRadius;

                // Broad test
                if (!Collisions.IsSphereWithinCollider(midPoint, radius, colliderGroup.Bounds))
                {
                    continue;
                }

                if (!Collisions.IsCapsuleWithinColliderApprox(positions[i], positions[i + 1],
                    _fingerRadius, colliderGroup.Bounds))
                {
                    continue;
                }

                List<Collider> colliders = colliderGroup.Colliders;
                for (int j = 0; j < colliders.Count; j++)
                {
                    // Broader test
                    if (!Collisions.IsSphereWithinCollider(midPoint, radius, colliders[j]))
                    {
                        continue;
                    }

                    if (Collisions.IsCapsuleWithinColliderApprox(positions[i], positions[i + 1],
                        _fingerRadius, colliders[j]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private Vector3[] ResizeCachedArray(ref Vector3[] positionCache, int targetLength)
        {
            if (positionCache == null || positionCache.Length < targetLength)
            {
                positionCache = new Vector3[targetLength];
            }

            return positionCache;
        }

        private Vector3[] _existsCollisionForCache;
        public bool ExistsCollisionFor(ShadowJoints joints, ColliderGroup colliderGroup)
        {
            Vector3[] positions = ResizeCachedArray(ref _existsCollisionForCache, joints.NumJoints - 1);
            GetInterpolatedWorldPositions(joints, joints, 0, positions);
            return ExistsCollision(colliderGroup, positions, joints.NumJoints - 1);
        }

        private Vector3[] _existsCollisionBetweenAtTCache;
        public bool ExistsCollisionBetweenAtT(ShadowJoints from, ShadowJoints to, ColliderGroup colliderGroup, float t)
        {
            Vector3[] positions = ResizeCachedArray(ref _existsCollisionBetweenAtTCache, from.NumJoints - 1);
            GetInterpolatedWorldPositions(from, to, t, positions);
            return ExistsCollision(colliderGroup, positions, from.NumJoints - 1);
        }

        private Vector3[] _existsCollisionBetweenCache0;
        private Vector3[] _existsCollisionBetweenCache1;
        public float ExistsCollisionBetween(ShadowJoints from, ShadowJoints to,
            ColliderGroup colliderGroup, float radiusMultiplier = 0.25f)
        {
            Vector3[] positions0 =
                ResizeCachedArray(ref _existsCollisionBetweenCache0, from.NumJoints - 1);
            Vector3[] positions1 =
                ResizeCachedArray(ref _existsCollisionBetweenCache1, from.NumJoints - 1);

            float t = 0;
            float tinc = 1.0f / _iterations;
            float deltaSq = _fingerRadius * _fingerRadius * radiusMultiplier * radiusMultiplier;

            GetInterpolatedWorldPositions(from, to, t, positions0);

            bool checkCollision = true;

            while (t <= 1)
            {
                if (checkCollision)
                {
                    if (ExistsCollision(colliderGroup, positions0, from.NumJoints - 1))
                    {
                        return t;
                    }
                }

                if (t >= 1)
                {
                    break;
                }

                t += tinc;
                if (t >= 1)
                {
                    checkCollision = true;
                }
                else
                {
                    checkCollision = false;
                    GetInterpolatedWorldPositions(from, to, t, positions1);

                    for (int i = 0; i < positions1.Length; i++)
                    {
                        if (Vector3.SqrMagnitude(positions0[i] - positions1[i]) > deltaSq)
                        {
                            Array.Copy(positions1, positions0, positions0.Length);
                            checkCollision = true;
                            break;
                        }
                    }
                }
            }

            return -1;
        }

        private Vector3[] _getWorldPositionCache;
        public Vector3 GetWorldPosition(int jointIndex, ShadowJoints shadowJoints)
        {
            Vector3[] positions =
                ResizeCachedArray(ref _getWorldPositionCache, shadowJoints.NumJoints - 1);
            GetInterpolatedWorldPositions(shadowJoints, shadowJoints, 0f, positions);
            return positions[jointIndex - 1];
        }

        public Vector3 GetPositionRelativeToRoot(Vector3 worldPosition, ShadowJoints shadowJoints)
        {
            UpdateResultTransforms(shadowJoints, shadowJoints, 0f);
            return _resultTransforms[0].InverseTransformPoint(worldPosition);
        }
    }
}
