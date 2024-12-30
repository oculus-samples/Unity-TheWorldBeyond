// Copyright (c) Meta Platforms, Inc. and affiliates.

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
