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

namespace Oculus.Interaction.Grab
{
    public static class GrabPoseHelper
    {
        public delegate Pose PoseCalculator(in Pose desiredPose, in Pose referencePose);

        /// <summary>
        /// Finds the best pose comparing the one that requires the minimum rotation
        /// and minimum translation.
        /// </summary>
        /// <param name="desiredPose">Pose to measure from.</param>
        /// <param name="referencePose">Reference pose of the surface.</param>
        /// <param name="bestPose">Nearest pose to the desired one at the surface.</param>
        /// <param name="scoringModifier">Modifiers for the score based in rotation and distance.</param>
        /// <param name="minimalTranslationPoseCalculator">Delegate to calculate the nearest, by position, pose at a surface.</param>
        /// <param name="minimalRotationPoseCalculator">Delegate to calculate the nearest, by rotation, pose at a surface.</param>
        /// <returns>The score, normalized, of the best pose.</returns>
        public static float CalculateBestPoseAtSurface(in Pose desiredPose, in Pose referencePose, out Pose bestPose,
            in PoseMeasureParameters scoringModifier,
            PoseCalculator minimalTranslationPoseCalculator, PoseCalculator minimalRotationPoseCalculator)
        {
            float bestScore;
            Pose minimalRotationPose = minimalRotationPoseCalculator(desiredPose, referencePose);
            if (scoringModifier.MaxDistance > 0)
            {
                Pose minimalTranslationPose = minimalTranslationPoseCalculator(desiredPose, referencePose);

                bestPose = SelectBestPose(minimalRotationPose, minimalTranslationPose, desiredPose, scoringModifier, out bestScore);
            }
            else
            {
                bestPose = minimalRotationPose;
                bestScore = RotationalSimilarity(desiredPose.rotation, bestPose.rotation);
            }
            return bestScore;
        }

        /// <summary>
        /// Compares two poses to a reference and returns the most similar one
        /// </summary>
        /// <param name="a">First pose to compare with the reference.</param>
        /// <param name="b">Second pose to compare with the reference.</param>
        /// <param name="reference">Reference pose to measure from.</param>
        /// <param name="scoringModifier">Modifiers for the score based in rotation and distance.</param>
        /// <param name="maxDistance">Max distance to measure the score.</param>
        /// <param name="bestScore">Out value with the score of the best pose.</param>
        /// <returns>The most similar pose to reference out of a and b</returns>
        public static Pose SelectBestPose(in Pose a, in Pose b, in Pose reference, PoseMeasureParameters scoringModifier, out float bestScore)
        {
            float aScore = Similarity(reference, a, scoringModifier);
            float bScore = Similarity(reference, b, scoringModifier);
            if (aScore >= bScore)
            {
                bestScore = aScore;
                return a;
            }
            bestScore = bScore;
            return b;
        }


        /// <summary>
        /// Indicates how similar two poses are.
        /// </summary>
        /// <param name="from">First pose to compare.</param>
        /// <param name="to">Second pose to compare.</param>
        /// <param name="maxDistance">The max distance in which the poses can be similar.</param>
        /// <returns>0 indicates no similitude, 1 for equal poses</returns>
        public static float Similarity(in Pose from, in Pose to, PoseMeasureParameters scoringModifier)
        {
            float rotationDifference = RotationalSimilarity(from.rotation, to.rotation);
            float positionDifference = PositionalSimilarity(from.position, to.position, scoringModifier.MaxDistance);
            return positionDifference * (1f - scoringModifier.PositionRotationWeight)
                + rotationDifference * (scoringModifier.PositionRotationWeight);
        }

        /// <summary>
        /// Get how similar two positions are.
        /// It uses a maximum value to normalize the output
        /// </summary>
        /// <param name="from">The first position.</param>
        /// <param name="to">The second position.</param>
        /// <param name="maxDistance">The Maximum distance used to normalise the output</param>
        /// <returns>0 when the input positions are further than maxDistance, 1 for equal positions.</returns>
        public static float PositionalSimilarity(in Vector3 from, in Vector3 to, float maxDistance)
        {
            float distance = Vector3.Distance(from, to);
            if (distance == 0)
            {
                return 1f;
            }
            return 1f - Mathf.Clamp01(distance / maxDistance);
        }

        /// <summary>
        /// Get how similar two rotations are.
        /// Since the Quaternion.Dot is bugged in unity. We compare the
        /// dot products of the forward and up vectors of the rotations.
        /// </summary>
        /// <param name="from">The first rotation.</param>
        /// <param name="to">The second rotation.</param>
        /// <returns>0 for opposite rotations, 1 for equal rotations.</returns>
        public static float RotationalSimilarity(in Quaternion from, in Quaternion to)
        {
            float forwardDifference = Vector3.Dot(from * Vector3.forward, to * Vector3.forward) * 0.5f + 0.5f;
            float upDifference = Vector3.Dot(from * Vector3.up, to * Vector3.up) * 0.5f + 0.5f;
            return forwardDifference * upDifference;
        }

    }
}
