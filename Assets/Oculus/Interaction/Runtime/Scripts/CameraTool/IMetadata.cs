/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

namespace Oculus.Interaction.CameraTool
{
    public interface IMetadata
    {
        /// <summary>
        /// A unique identifier associated with the image
        /// </summary>
        string ImageID { get; }

        /// <summary>
        /// The path of the written image on disk
        /// </summary>
        string Path { get; }

        /// <summary>
        /// The width of the image, in pixels
        /// </summary>
        int Width { get; }

        /// <summary>
        /// The height of the image, in pixels
        /// </summary>
        int Height { get; }

        /// <summary>
        /// The size of the image file, in bytes
        /// </summary>
        int FileSize { get; }
    }
}
