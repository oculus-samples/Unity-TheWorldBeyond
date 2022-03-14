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

namespace Oculus.Interaction.CameraTool
{
    public interface IThumbnail
    {
        /// <summary>
        /// A unique identifier associated with the image
        /// </summary>
        string ImageID { get; }

        /// <summary>
        /// The texture object
        /// </summary>
        Texture2D Texture { get; }

        /// <summary>
        /// Disposes the texture object
        /// </summary>
        void Destroy();
    }
}
