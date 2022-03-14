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

namespace Oculus.Interaction
{
    /// <summary>
    /// Scales and positions a transform to match a RectTransform.
    /// An example use case is to fit a BoxCollider to a Canvas.
    /// </summary>
    [ExecuteInEditMode]
    public class FitTransformToRect : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _source;

        [SerializeField]
        private Transform _target;

        [SerializeField]
        private bool _followPosition = true;

        [SerializeField]
        private bool _followRotation = true;

        [SerializeField]
        private bool _followScale = true;

        [SerializeField]
        private float _depth = 0.005f;

        [SerializeField]
        private Vector3 _localPosOffset = Vector3.zero;

        [SerializeField]
        private Vector3 _localScaleOffset = Vector3.zero;

        protected virtual void Start()
        {
            Assert.IsNotNull(_source);
            Assert.IsNotNull(_target);
        }

        private void MatchTargetToSource()
        {
            if (_source == null || _target == null)
            {
                return;
            }

            if (_followRotation)
            {
                _target.rotation = _source.rotation;
            }

            if (_followPosition)
            {
                _target.position = _source.position +
                    _source.TransformDirection(_localPosOffset);
            }

            if (_followScale)
            {
                Vector3 targetLossyScale =
                    new Vector3(_source.lossyScale.x * _source.rect.width,
                                _source.lossyScale.y * _source.rect.height,
                                _depth);

                if (_target.parent != null)
                {
                    _target.localScale =
                        new Vector3(targetLossyScale.x / _target.parent.lossyScale.x,
                                    targetLossyScale.y / _target.parent.lossyScale.y,
                                    targetLossyScale.z / _target.parent.lossyScale.z);
                }
                else
                {
                    _target.localScale = targetLossyScale;
                }

                _target.localScale += _localScaleOffset;
            }
        }

        protected virtual void Update()
        {
            MatchTargetToSource();
        }

#if UNITY_EDITOR
        protected virtual void OnDrawGizmosSelected()
        {
            if (_target == null)
            {
                return;
            }

            var prevMatrix = Gizmos.matrix;
            Gizmos.matrix = _target.localToWorldMatrix;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            Gizmos.matrix = prevMatrix;
        }
#endif
    }
}
