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
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace Oculus.Interaction.PoseDetection
{
    public class FrameRectProviderDebugVisuals : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IFrameRectProvider))]
        private MonoBehaviour _frameRectProvider;
        private IFrameRectProvider FrameRectProvider;

        [SerializeField]
        private Material _lineRendererMaterial;

        [SerializeField]
        private float _rendererLineWidth = 0.005f;

        private List<LineRenderer> _lineRenderers;
        private int _enabledRendererCount;

        protected virtual void Awake()
        {
            FrameRectProvider = _frameRectProvider as IFrameRectProvider;
            _lineRenderers = new List<LineRenderer>();
        }

        protected virtual void Start()
        {
            Assert.IsNotNull(FrameRectProvider);
        }

        protected virtual void Update()
        {
            ResetLines();

            if (FrameRectProvider.FrameRect.IsValid)
            {
                DrawRect(FrameRectProvider.FrameRect);
            }
        }

        private void DrawRect(in FrameRect frameRect)
        {
            AddLine(frameRect.BottomLeft, frameRect.BottomRight, Color.blue); // bottom
            AddLine(frameRect.BottomLeft, frameRect.TopLeft, Color.green); // left
            AddLine(frameRect.TopRight, frameRect.TopLeft, Color.red); // top
            AddLine(frameRect.TopRight, frameRect.BottomRight, Color.yellow); // right
        }

        private void ResetLines()
        {
            foreach (var lineRenderer in _lineRenderers)
            {
                lineRenderer.enabled = false;
            }
            _enabledRendererCount = 0;
        }

        private void AddLine(Vector3 indexTip, Vector3 thumbTip, Color color)
        {
            LineRenderer lineRenderer;
            if (_enabledRendererCount == _lineRenderers.Count)
            {
                lineRenderer = new GameObject().AddComponent<LineRenderer>();
                lineRenderer.startWidth = _rendererLineWidth;
                lineRenderer.endWidth = _rendererLineWidth;
                lineRenderer.positionCount = 2;
                lineRenderer.material = _lineRendererMaterial;
                _lineRenderers.Add(lineRenderer);
            }
            else
            {
                lineRenderer = _lineRenderers[_enabledRendererCount];
            }

            _enabledRendererCount++;

            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, indexTip);
            lineRenderer.SetPosition(1, thumbTip);
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }
    }
}
