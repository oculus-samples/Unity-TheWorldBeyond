// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.AI;

public class SamplePet : MonoBehaviour
{
    NavMeshAgent _agent;
    public Animator _animator;
    public Transform _petRoot;
    Vector3 _lastPetPosition;
    Vector3 _faceDirection = Vector3.forward;

    // head is Z-left, X-down, and Y-forward
    public Transform _headBone;
    Quaternion _targetRotation = Quaternion.identity;
    Quaternion _originalHeadRot = Quaternion.identity;
    // eyes are Z-forward
    public Transform _leftEye;
    public Transform _rightEye;
    Quaternion _originalLeftEyeRot = Quaternion.identity;
    Quaternion _originalRightEyeRot = Quaternion.identity;
    const float _maxHeadAngle = 45.0f;
    const float _maxEyeAngle = 30.0f;

    // pet looks at user by default, unless this transform is close
    public Transform _nearbyTarget;

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _originalLeftEyeRot = _leftEye.localRotation;
        _originalRightEyeRot = _rightEye.localRotation;
        _originalHeadRot = _headBone.localRotation;
    }

    // because the look behavior overrides bone rotations, use LateUpdate() instead of Update()
    void LateUpdate()
    {
        _animator.SetBool("Running", _agent.velocity.magnitude > Mathf.Epsilon);
        _animator.SetFloat("Speed", Mathf.Clamp01(_agent.velocity.magnitude * 0.25f + 0.5f));

        // face direction of travel
        Vector3 targetFaceDirection = _petRoot.forward;
        Vector3 currentVelocity = transform.position - _lastPetPosition;
        bool petNavigating = currentVelocity.magnitude > Mathf.Epsilon;
        if (petNavigating)
        {
            targetFaceDirection = currentVelocity.normalized;

            // update "look" rotation even when unused, for proper bone blending
            _targetRotation = _headBone.parent.rotation * _originalHeadRot;
        }

        _lastPetPosition = transform.position;
        _faceDirection = Vector3.Slerp(_faceDirection, targetFaceDirection, 0.1f);
        FacePosition(transform.position + _faceDirection);

        // by default, look at the user/camera
        // otherwise, look at the target if it's close or being chased
        Vector3 lookPos = Camera.main.transform.position;
        bool graphicNearby = Vector3.Distance(_nearbyTarget.position, _petRoot.position) <= 1.0f;
        graphicNearby &= Vector3.Dot(_faceDirection, (_nearbyTarget.position - _petRoot.position).normalized) >= 0.0f;
        if (petNavigating || (!petNavigating && graphicNearby))
        {
            lookPos = _nearbyTarget.position;
        }
        DoLookAtBehavior(lookPos);
    }

    /// <summary>
    /// Point pet's Z+ towards a world position.
    /// </summary>
    public void FacePosition(Vector3 worldPosition)
    {
        worldPosition = new Vector3(worldPosition.x, transform.position.y, worldPosition.z);
        _petRoot.rotation = Quaternion.LookRotation(worldPosition - transform.position);
    }

    /// <summary>
    /// Have the pet try and look at a position in the world. Bone angles clamp to prevent Exorcist-neck.
    /// </summary>
    void DoLookAtBehavior(Vector3 lookPosition)
    {
        Vector3 headLook = _headBone.transform.rotation * Vector3.up;
        Vector3 headUp = _headBone.transform.rotation * Vector3.right;

        // the head bone is at a neck position
        // so the illusion requires it to look at a point slightly below the camera
        Vector3 petHeadY = (lookPosition - Vector3.up * 0.2f) - _headBone.position;

        // clamp angle so pet doesn't break its neck
        float currentAngle = Vector3.Angle(petHeadY, headLook);
        petHeadY = Vector3.Slerp(headLook, petHeadY, Mathf.Clamp01(_maxHeadAngle / currentAngle)).normalized;

        Vector3 petHeadX = headUp;
        Vector3.OrthoNormalize(ref petHeadY, ref petHeadX);
        Vector3 petHeadZ = Vector3.Cross(petHeadX, petHeadY);
        Quaternion lookRot = Quaternion.LookRotation(petHeadZ, petHeadY);
        _targetRotation = Quaternion.Lerp(_targetRotation, lookRot, 0.05f);

        _headBone.rotation = _targetRotation;

        // make eyes look
        Vector3 leftLook = lookPosition - _leftEye.position;
        Vector3 rightLook = lookPosition - _rightEye.position;
        Vector3 leftEyeFwd = (_leftEye.parent.rotation * _originalLeftEyeRot) * Vector3.forward;
        Vector3 rightEyeFwd = (_rightEye.parent.rotation * _originalRightEyeRot) * Vector3.forward;
        float leftAngle = Vector3.Angle(leftEyeFwd, leftLook);
        float rightAngle = Vector3.Angle(rightEyeFwd, rightLook);
        leftLook = Vector3.Slerp(leftEyeFwd, leftLook, Mathf.Clamp01(_maxEyeAngle / leftAngle)).normalized;
        rightLook = Vector3.Slerp(rightEyeFwd, rightLook, Mathf.Clamp01(_maxEyeAngle / rightAngle)).normalized;

        _leftEye.rotation = Quaternion.LookRotation(leftLook);
        _rightEye.rotation = Quaternion.LookRotation(rightLook);
    }
}
