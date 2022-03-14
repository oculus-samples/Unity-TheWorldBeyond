/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction
{
    public class PointableDebugPolylineGizmos : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IPointable))]
        private MonoBehaviour _pointable;

        [SerializeField]
        private Color _hoverColor = Color.blue;

        [SerializeField]
        private Color _selectColor = Color.green;

        class PointData
        {
            public Pose Pose { get; set; }
            public bool Selecting { get; set; }
        }

        private Dictionary<int, PointData> _points;

        public Color HoverColor
        {
            get
            {
                return _hoverColor;
            }
            set
            {
                _hoverColor = value;
            }
        }

        public Color SelectColor
        {
            get
            {
                return _selectColor;
            }
            set
            {
                _selectColor = value;
            }
        }

        private IPointable Pointable;

        protected bool _started = false;

        protected virtual void Awake()
        {
            Pointable = _pointable as IPointable;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            Assert.IsNotNull(Pointable);
            _points = new Dictionary<int, PointData>();
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Pointable.WhenPointerEventRaised += HandlePointerEventRaised;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Pointable.WhenPointerEventRaised -= HandlePointerEventRaised;
            }
        }

        private void HandlePointerEventRaised(PointerArgs args)
        {
            switch (args.PointerEvent)
            {
                case PointerEvent.Hover:
                    _points.Add(args.Identifier,
                        new PointData() {Pose = args.Pose, Selecting = false});
                    break;
                case PointerEvent.Select:
                    _points[args.Identifier].Selecting = true;
                    break;
                case PointerEvent.Move:
                    _points[args.Identifier].Pose = args.Pose;
                    break;
                case PointerEvent.Unselect:
                    _points[args.Identifier].Selecting = false;
                    break;
                case PointerEvent.Unhover:
                case PointerEvent.Cancel:
                    _points.Remove(args.Identifier);
                    break;
            }
        }

        protected virtual void LateUpdate()
        {
            PolylineGizmos.LineWidth = 0.03f;
            foreach (PointData pointData in _points.Values)
            {
                PolylineGizmos.Color = pointData.Selecting ? _selectColor : _hoverColor;
                PolylineGizmos.DrawPoint(pointData.Pose.position);
            }
        }

        #region Inject

        public void InjectAllPointableDebugPolylineGizmos(IPointable pointable)
        {
            InjectPointable(pointable);
        }

        public void InjectPointable(IPointable pointable)
        {
            _pointable = pointable as MonoBehaviour;
            Pointable = pointable;
        }

        #endregion
    }
}
