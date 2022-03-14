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
using UnityEngine.UI;
using UnityEngine.Assertions;
using Oculus.Interaction.PoseDetection;

namespace Oculus.Interaction.CameraTool
{
    public class AspectLayoutElement : MonoBehaviour, ILayoutElement
    {
        public enum TargetAxis
        {
            Vertical,
            Horizontal,
        }

        [SerializeField, Interface(typeof(IAspectRatioProvider))]
        private MonoBehaviour _aspectProvider;

        [SerializeField]
        private TargetAxis _targetAxis;

        [SerializeField]
        private int _layoutPriority = 1;

        public float preferredWidth { get; private set; }
        public float preferredHeight { get; private set; }
        public float minWidth => -1;
        public float minHeight => -1;
        public float flexibleWidth => -1;
        public float flexibleHeight => -1;
        public int layoutPriority => _isCalculatingLayout ? int.MinValue : _layoutPriority;

        private bool _isCalculatingLayout = false;

        private IAspectRatioProvider AspectProvider;

        private RectTransform RectTransform => (RectTransform)transform;

        private void SetTargetAxis()
        {
            float aspect = AspectProvider == null ? 1 : AspectProvider.AspectRatio;

            switch (_targetAxis)
            {
                case TargetAxis.Vertical:
                    preferredWidth = -1;
                    preferredHeight = RectTransform.rect.width / aspect;
                    break;
                case TargetAxis.Horizontal:
                    preferredWidth = RectTransform.rect.height * aspect;
                    preferredHeight = -1;
                    break;
                default:
                    preferredWidth = -1;
                    preferredHeight = -1;
                    break;
            }
        }

        public void CalculateLayoutInputHorizontal()
        {
            _isCalculatingLayout = true;
            preferredWidth = LayoutUtility.GetPreferredWidth(RectTransform);
            _isCalculatingLayout = false;
        }

        public void CalculateLayoutInputVertical()
        {
            _isCalculatingLayout = true;
            preferredHeight = LayoutUtility.GetPreferredHeight(RectTransform);
            _isCalculatingLayout = false;

            SetTargetAxis();
        }

        protected virtual void Awake()
        {
            AspectProvider = _aspectProvider as IAspectRatioProvider;
        }

        protected virtual void Start()
        {
            Assert.IsNotNull(AspectProvider);
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            LayoutRebuilder.MarkLayoutForRebuild(RectTransform);
        }
#endif
    }
}
