/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using UnityEngine;

public class GravityAligner : MonoBehaviour
{
    public static Quaternion GetAlignedOrientation(Quaternion rotation, float alignmentAngleThreshold)
    {
        var alignmentThreshold = Mathf.Cos(alignmentAngleThreshold * Mathf.Deg2Rad);
        var normal = rotation * Vector3.forward;

        var dot = Vector3.Dot(Vector3.up, normal);
        if (dot > alignmentThreshold)
        {
            return Quaternion.FromToRotation(normal, Vector3.up) * rotation;
        }

        if (dot < -alignmentThreshold)
        {
            return Quaternion.FromToRotation(normal, -Vector3.up) * rotation;
        }

        var up = rotation * Vector3.up;
        dot = Vector3.Dot(Vector3.up, up);
        if (dot > alignmentThreshold)
        {
            return Quaternion.FromToRotation(up, Vector3.up) * rotation;
        }

        return rotation;
    }
}
