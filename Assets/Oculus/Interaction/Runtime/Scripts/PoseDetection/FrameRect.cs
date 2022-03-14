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

namespace Oculus.Interaction.PoseDetection
{
    public interface IFrameRectProvider
    {
        /// <summary>
        /// Data struture representing the rect.
        /// </summary>
        ref readonly FrameRect FrameRect { get; }

        /// <summary>
        /// Is the provider currently active and updating <see cref="FrameRect"/>
        /// </summary>
        bool IsActive { get; }
    }

    public readonly struct FrameRect
    {
        public readonly bool IsValid;

        public readonly float AspectRatio;
        public readonly float Width;
        public readonly float Height;

        public readonly Vector3 BottomLeft;
        public readonly Vector3 TopLeft;
        public readonly Vector3 TopRight;
        public readonly Vector3 BottomRight;
        public readonly Vector3 Center;

        /// <summary>
        /// Build a FrameRect using provided corner points
        /// </summary>
        /// <param name="aspectRatio">Used to set aspect ratio explicitly.
        /// If not provided, aspect will be calculated from dimensions</param>
        public FrameRect(Vector3 bottomLeft,
                         Vector3 topLeft,
                         Vector3 topRight,
                         Vector3 bottomRight,
                         float aspectRatio = -1)
        {
            IsValid = true;

            BottomLeft = bottomLeft;
            TopLeft = topLeft;
            TopRight = topRight;
            BottomRight = bottomRight;

            Center = Vector3.Lerp(bottomLeft, topRight, 0.5f);

            Width = Vector3.Magnitude(bottomRight - bottomLeft);
            Height = Vector3.Magnitude(topLeft - bottomLeft);

            AspectRatio = aspectRatio > 0 ? aspectRatio : Width / Height;
        }

        public Vector3 GetWorldNormal()
        {
            return Vector3.Cross(TopLeft - BottomLeft, BottomRight - BottomLeft).normalized;
        }

        public Vector3 GetWorldUp()
        {
            return Vector3.Normalize(TopLeft - BottomLeft);
        }
    }
}
