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

namespace Oculus.Interaction
{
    public class PolylineRenderer
    {
        private Vector4[] _positions = null;
        private bool _positionsNeedUpdate = false;

        private Color[] _colors = null;
        private bool _colorsNeedUpdate = false;

        private Bounds _bounds;

        private Mesh _baseMesh;
        private Material _material;

        private ComputeBuffer _positionBuffer;
        private ComputeBuffer _colorBuffer;
        private ComputeBuffer _argsBuffer;
        private uint[] _argsData;

        private int _positionBufferShaderID = Shader.PropertyToID("_PositionBuffer");
        private int _colorBufferShaderID = Shader.PropertyToID("_ColorBuffer");
        private int _localToWorldShaderID = Shader.PropertyToID("_LocalToWorld");
        private int _scaleShaderID = Shader.PropertyToID("_Scale");

        private int _maxLineCount = 1;
        private Matrix4x4 _matrix = Matrix4x4.identity;
        private float _lineScaleFactor = 1.0f;

        public float LineScaleFactor
        {
            get
            {
                return _lineScaleFactor;
            }
            set
            {
                _lineScaleFactor = value;
            }
        }

        public PolylineRenderer(Material material = null)
        {
            if (material == null)
            {
                material = new Material(Shader.Find("Custom/PolylineUnlit"));
            }

            _material = new Material(material);

            GameObject baseCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _baseMesh = baseCube.GetComponent<MeshFilter>().sharedMesh;
            GameObject.DestroyImmediate(baseCube);

            // Start with one of these is one Polyline
            _positions = new Vector4[_maxLineCount * 2];
            _colors = new Color[_maxLineCount * 2];

            // _maxLineCount * 2 as we use two points per segment
            // 16 for Vector4: 4*sizeof(float) = 4*4
            _positionBuffer = new ComputeBuffer(_maxLineCount * 2, 16);
            _positionBuffer.SetData(_positions);
            _colorBuffer = new ComputeBuffer(_maxLineCount * 2, 16);
            _colorBuffer.SetData(_colors);

            _material.SetBuffer(_positionBufferShaderID, _positionBuffer);
            _material.SetBuffer(_colorBufferShaderID, _colorBuffer);

            _argsData = new uint[5] {0, 0, 0, 0, 0};
            _argsData[0] = (uint)_baseMesh.GetIndexCount(0);
            _argsData[1] = (uint)_maxLineCount;

            _argsBuffer = new ComputeBuffer(1, _argsData.Length * sizeof(uint),
                ComputeBufferType.IndirectArguments);
            _argsBuffer.SetData(_argsData);

            _positionsNeedUpdate = true;
            _colorsNeedUpdate = true;
        }

        public void Cleanup()
        {
            _positionBuffer.Release();
            _colorBuffer.Release();
            _argsBuffer.Release();
            if (Application.isPlaying)
            {
                GameObject.Destroy(_material);
            }
            else
            {
                GameObject.DestroyImmediate(_material);
            }
        }

        public void SetLines(List<Vector4> positions, Color color)
        {
            List<Color> colors = new List<Color>();
            for (int i = 0; i < positions.Count; i++)
            {
                colors.Add(color);
            }

            SetLines(positions, colors);
        }

        public void SetLines(List<Vector4> positions, List<Color> colors, int maxCount = -1)
        {
            int count = maxCount < 0 ? positions.Count : maxCount;
            if (count > _positions.Length)
            {
                _maxLineCount = count / 2;
                _positions = new Vector4[_maxLineCount * 2];
                _positionBuffer.Release();
                _positionBuffer = new ComputeBuffer(_maxLineCount * 2, 16);
                _positionBuffer.SetData(_positions);
            }

            _bounds = new Bounds();
            Vector3 min = Vector3.zero;
            Vector3 max = Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                Vector4 position = positions[i];
                _positions[i] = position;

                Vector3 width = position.w * Vector3.one;
                Vector3 p = (Vector3)position;
                Vector3 pmin = p - width / 2f;
                Vector3 pmax = p + width / 2f;
                if (i == 0)
                {
                    min = pmin;
                    max = pmax;
                }
                else
                {
                    min.x = Mathf.Min(pmin.x, min.x);
                    min.y = Mathf.Min(pmin.y, min.y);
                    min.z = Mathf.Min(pmin.z, min.z);
                    max.x = Mathf.Max(pmax.x, max.x);
                    max.y = Mathf.Max(pmax.y, max.y);
                    max.z = Mathf.Max(pmax.z, max.z);
                }
            }
            _bounds.SetMinMax(min, max);

            _positionsNeedUpdate = true;

            if (count > _colors.Length)
            {
                _maxLineCount = count / 2;
                _colors = new Color[_maxLineCount * 2];
                _colorBuffer.Release();
                _colorBuffer = new ComputeBuffer(_maxLineCount * 2, 16);
                _colorBuffer.SetData(_colors);
            }

            for (int i = 0; i < count; i++)
            {
                _colors[i] = colors[i];
            }

            _colorsNeedUpdate = true;

            SetDrawCount(count / 2);
        }

        private void SetDrawCount(int c)
        {
            int drawCount = c;
            _argsData[1] = (uint)drawCount;
            _argsBuffer.SetData(_argsData);
        }

        public void RenderLines()
        {
            if (_positionsNeedUpdate)
            {
                _positionBuffer.SetData(_positions);
                _material.SetBuffer(_positionBufferShaderID, _positionBuffer);
                _positionsNeedUpdate = false;
            }

            if (_colorsNeedUpdate)
            {
                _colorBuffer.SetData(_colors);
                _material.SetBuffer(_colorBufferShaderID, _colorBuffer);
                _colorsNeedUpdate = false;
            }

            _material.SetFloat(_scaleShaderID, _lineScaleFactor);
            _material.SetMatrix(_localToWorldShaderID, _matrix);
            Graphics.DrawMeshInstancedIndirect(_baseMesh, 0, _material, _bounds, _argsBuffer);
        }

        public void SetTransform(Transform transform)
        {
            _matrix = transform.localToWorldMatrix;
        }
    }
}
