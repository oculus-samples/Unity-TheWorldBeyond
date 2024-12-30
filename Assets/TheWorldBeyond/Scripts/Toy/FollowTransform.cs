// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using System.Collections;

public class FollowTransform : MonoBehaviour
{
    public bool lateUpdate = false;
    public bool unscaledTime = false;
    public bool followRotate = true;
    public bool followWithOffset = true;
    public bool followWithSpring = true;
    public bool offsetsOnEnable = true;

    public bool animated = false;

    // if no transform specified, current is used
    public Transform theTransform;

    // if no follow transform specified, parent is used
    public Transform followTransform;
    Transform followTrans;
    private Vector3 prevFollowPos;

    [Range(0f, 1f)]
    public float weight = 1f;

    // Dynamics settings
    public float stiffness = 0.04f;
    public float mass = 0.5f;
    public float damping = 1f;
    public float gravity = 0.0001f;

    float currentDelta = 0f;

    // noise
    //public float sNoiseAmp = 0;
    //public float sNoiseFreq = 0.5f;

    // Target and dynamic positions
    Vector3 targetPos;
    Quaternion targetRot;
    Vector3 dynamicPos;
    Quaternion dynamicRot;

    Vector3 force;
    Vector3 acc;
    Vector3 vel;

    Vector3 followTransPosOffset = Vector3.zero;
    Quaternion followTransRotOffset = Quaternion.identity;

    void Start()
    {
        if (!offsetsOnEnable)
        {
            Init();
        }
    }

    void OnEnable()
    {
        if (offsetsOnEnable)
        {
            Init();
        }
    }

    void Init()
    {
        // assign this transform if not assigned
        if (theTransform == null)
            theTransform = transform;

        // assign parent transform if not assigned
        if (!followTransform && theTransform.parent != null)
        {
            followTransform = theTransform.parent;
        }

        followTrans = followTransform;

        if (followWithOffset)
        {
            followTransPosOffset = followTrans.InverseTransformPoint(theTransform.position);
            followTransRotOffset = Quaternion.FromToRotation(followTrans.forward, theTransform.forward);
        }

        targetPos = followTrans.TransformPoint(followTransPosOffset);
        targetRot = followTrans.rotation * followTransRotOffset;

        dynamicPos = targetPos;
        dynamicRot = targetRot;

        vel = Vector3.zero;

        theTransform.position = targetPos;
        theTransform.rotation = targetRot;
    }

    void Update()
    {
        if (!lateUpdate)
        {
            Tick();
        }
    }

    void LateUpdate()
    {
        if (lateUpdate)
        {
            Tick();
        }
    }

    void Tick()
    {
        if (followTrans == null)
            return;

        if (animated)
        {
            followTransPosOffset = followTrans.InverseTransformPoint(theTransform.position);
            followTransRotOffset = Quaternion.FromToRotation(followTrans.forward, theTransform.forward);
        }

        currentDelta = Time.deltaTime * 90f;

        // clamp delta to avoid craziness
        currentDelta = Mathf.Clamp(currentDelta, 0.8f, 1.2f);

        targetPos = followTrans.TransformPoint(followTransPosOffset);

        if (followRotate)
            targetRot = followTrans.rotation * followTransRotOffset;

        // Calculate force, acceleration, and velocity per X, Y and Z
        force.x = (targetPos.x - dynamicPos.x) * stiffness;
        acc.x = force.x / mass;
        vel.x += acc.x * (1f - damping);

        force.y = (targetPos.y - dynamicPos.y) * stiffness;
        force.y -= gravity / 10f; // Add some gravity
        acc.y = force.y / mass;
        vel.y += acc.y * (1f - damping);

        force.z = (targetPos.z - dynamicPos.z) * stiffness;
        acc.z = force.z / mass;
        vel.z += acc.z * (1f - damping);

        // Update dynamic postion
        dynamicPos += (vel + force) * currentDelta;

        if (followRotate)
            dynamicRot = Quaternion.Lerp(theTransform.rotation, targetRot, stiffness * 3f * currentDelta);

        // set transform
        if (followWithSpring)
        {
            theTransform.position = Vector3.Lerp(theTransform.position, dynamicPos, weight);

            if (followRotate)
                theTransform.rotation = Quaternion.Lerp(theTransform.rotation, dynamicRot, weight);
        }
        else
        {
            theTransform.position = Vector3.Lerp(theTransform.position, targetPos, weight);

            if (followRotate)
                theTransform.rotation = Quaternion.Lerp(theTransform.rotation, targetRot, weight);
        }
    }

}
