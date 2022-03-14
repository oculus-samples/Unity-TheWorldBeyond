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

namespace Oculus.Interaction.PoseDetection
{
    /// <summary>
    /// Matches a <see cref="Transform"/> to a <see cref="FrameRect"/>
    /// </summary>
    public class FrameRectTransformer : MonoBehaviour
    {
        public enum ScaleMode
        {
            /// <summary>
            /// Do not scale
            /// </summary>
            None,
            /// <summary>
            /// Scale follows <see cref="FrameRect.Width"/>
            /// </summary>
            Width,
            /// <summary>
            /// Scale follows <see cref="FrameRect.Height"/>
            /// </summary>
            Height,
        }

        [SerializeField, Interface(typeof(IFrameRectProvider))]
        private MonoBehaviour _frameRectProvider;

        [SerializeField]
        private Transform _target;

        [Header("Position")]
        [SerializeField]
        private bool _followPosition;

        [Header("Rotation")]
        [SerializeField]
        private bool _followRotation;

        [Header("Scaling")]
        [SerializeField]
        private ScaleMode _scaleX = ScaleMode.Width;

        [SerializeField]
        private ScaleMode _scaleY = ScaleMode.Height;

        [SerializeField]
        private ScaleMode _scaleZ = ScaleMode.None;

        private IFrameRectProvider FrameRectProvider;

        private void UpdateTarget()
        {
            FrameRect frameRect = FrameRectProvider.FrameRect;

            if (!frameRect.IsValid)
            {
                return;
            }

            if (_followPosition)
            {
                _target.position = frameRect.Center;
            }
            if (_followRotation)
            {
                _target.rotation = Quaternion.LookRotation(frameRect.GetWorldNormal());
            }

            void ScaleAxis(ScaleMode dimension, ref float value)
            {
                switch (dimension)
                {
                    default:
                    case ScaleMode.None:
                        break;
                    case ScaleMode.Width:
                        value = frameRect.Width;
                        break;
                    case ScaleMode.Height:
                        value = frameRect.Height;
                        break;
                }
            }

            Vector3 localScale = _target.localScale;
            ScaleAxis(_scaleX, ref localScale.x);
            ScaleAxis(_scaleY, ref localScale.y);
            ScaleAxis(_scaleZ, ref localScale.z);
            _target.localScale = localScale;
        }

        protected virtual void Awake()
        {
            FrameRectProvider = _frameRectProvider as IFrameRectProvider;
        }

        protected virtual void Start()
        {
            Assert.IsNotNull(_target);
            Assert.IsNotNull(FrameRectProvider);
        }

        protected virtual void Update()
        {
            UpdateTarget();
        }

        #region Inject

        public void InjectAllFrameRectTransformer(IFrameRectProvider provider,
                                                  Transform target)
        {
            InjectFrameRectProvider(provider);
            InjectTarget(target);
        }

        public void InjectFrameRectProvider(IFrameRectProvider provider)
        {
            _frameRectProvider = provider as MonoBehaviour;
            FrameRectProvider = provider;
        }

        public void InjectTarget(Transform target)
        {
            _target = target;
        }

        #endregion
    }
}
