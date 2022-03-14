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

namespace Oculus.Interaction.Input
{
    /// <summary>
    /// Property block for OneEuroFilter parameters
    /// </summary>
    [Serializable]
    public struct OneEuroFilterPropertyBlock
    {
        [SerializeField]
        [Tooltip("Decrease min cutoff until jitter is eliminated")]
        public float _minCutoff;

        [SerializeField]
        [Tooltip("Increase beta from zero to reduce lag")]
        public float _beta;

        /// <summary>
        /// The minimum cutoff frequency of the filter, in Hertz
        /// </summary>
        public float MinCutoff => _minCutoff;

        /// <summary>
        /// Filter cutoff slope
        /// </summary>
        public float Beta => _beta;

        /// <summary>
        /// Cutoff frequency for derivative, in Hertz
        /// </summary>
        public float DCutoff => 1f;

        public OneEuroFilterPropertyBlock(float minCutoff, float beta)
        {
            _minCutoff = minCutoff;
            _beta = beta;
        }

        public static OneEuroFilterPropertyBlock Default =>
             new OneEuroFilterPropertyBlock() { _minCutoff = 1, _beta = 0 };
    }
}
