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

namespace Oculus.Interaction.CameraTool
{
    public interface ICaptureCamera
    {
        /// <summary>
        /// Raised when image is captured
        /// </summary>
        event Action WhenCaptured;

        /// <summary>
        /// Field of View of Camera
        /// </summary>
        float FieldOfView { get; set; }

        /// <summary>
        /// The camera's target texture
        /// </summary>
        RenderTexture Texture { get; }

        /// <summary>
        /// Capture an image
        /// </summary>
        void Capture();
    }
}
