// Copyright(c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class SanctuaryPet : MonoBehaviour
{
    BallCollectable _ballEatTarget = null;
    // we need a cooldown before eating the next ball
    float _eatCooldown = 0.0f;
    Vector3 _moveTargetPos = Vector3.zero;
    Vector3 _moveTargetDir = Vector3.forward;
    int _collectedBalls = 0;
    float _ozGlowOvershoot = 0.0f;
    NavMeshAgent _agent;
    const float _runSpeed = 2.0f;
    const float _walkSpeed = 1.0f;
    Animator _animator;
    public Transform _petRoot;
    public BoneSimManager _boneSim;
    Vector3 _lastPetPosition;
    Vector3 _faceDirection = Vector3.forward;

    float _lookAtPlayerDistance = 7.0f;
    const float _maxHeadAngle = 45.0f;
    const float _maxEyeAngle = 30.0f;
    // head is Z-left, X-down, and Y-forward
    public Transform _headBone;
    Quaternion _targetRotation = Quaternion.identity;
    bool _lookingAtTarget = false;
    BallCollectable _ballLookTarget = null;
    // eyes are Z-forward
    public Transform _leftEye;
    public Transform _rightEye;
    Quaternion _originalLeftEyeRot = Quaternion.identity;
    Quaternion _originalRightEyeRot = Quaternion.identity;
    public Transform _ballAttachmentBoneRight = null;
    public Transform _ballAttachmentBoneLeft = null;
    bool _ballAttached = false;

    public SkinnedMeshRenderer[] _ozMeshes;

    public GameObject _listeningIndicator;
    public GameObject _fullyChargedPrefab;

    // this object is what "hides" Oz during SearchForOz
    public GameObject _passthroughShell;

    public enum PetState
    {
        Idle,
        Chasing, // Oz's default state
        Angry,
        Petting,
        Eating,
        Listening,
        Ending // running to ship to take off, pet is on auto-pilot
    };
    public PetState _ozState { private set; get; } = PetState.Idle;

    public GameObject _ozTeleportPrefab;
    public AudioClip _sparkleSound;
    AudioSource OzAudioSource;
    OzAudio OzAudioManager;

    public Transform _ozShadowMesh;
    public Transform _ozShadowRoot;

    bool _rampBaseHit = false;

    public ThoughtBubble _thoughtBubble;

    Transform _mainCam;

    private void Awake()
    {
        transform.position = Vector3.zero;
        OzAudioSource = GetComponent<AudioSource>();
        OzAudioManager = GetComponent<OzAudio>();
        _animator = GetComponent<Animator>();

        if (Application.platform == RuntimePlatform.Android)
        {
            if (transform.GetComponent<OzDevInput>())
            {
                transform.GetComponent<OzDevInput>().enabled = false;
            }
        }

        _listeningIndicator.SetActive(false);
    }

    public void Initialize()
    {
        _originalLeftEyeRot = _leftEye.localRotation;
        _originalRightEyeRot = _rightEye.localRotation;
        _agent = GetComponent<NavMeshAgent>();
        _agent.SetDestination(Vector3.zero);
        // Body rotation is controlled in FacePosition()
        // Look direction is controlled in DoLookAtBehavior()
        _agent.updateRotation = false;
        ResetAnimFlags();

        _thoughtBubble.gameObject.SetActive(false);

        // turn off ball/oz collision
        // instead of dealing with players intentionally shooting Oz, and the added complexity of her reacting, just disable it
        Physics.IgnoreLayerCollision(13, 14, true);
    }

    public void LateUpdate()
    {
        if (OVRInput.GetUp(OVRInput.RawButton.A))
        {
            PrintDebugOutput();
        }

        if (!_mainCam)
        {
            _mainCam = SanctuaryExperience.Instance._mainCamera.transform;
        }
        
        _animator.SetBool("Running", _agent.velocity.magnitude > Mathf.Epsilon);
        // agent.velocity.magnitude is animator.speed (between 1 and 2, set in DoChaseBehavior)
        _animator.SetFloat("Speed", Mathf.Clamp01(_agent.velocity.magnitude*0.25f+0.5f));

        if (_ozGlowOvershoot > 0.0f)
        {
            _ozGlowOvershoot -= Time.deltaTime * 2.0f;
            _ozGlowOvershoot = Mathf.Clamp01(_ozGlowOvershoot);
            foreach (SkinnedMeshRenderer smr in _ozMeshes)
            {
                smr.sharedMaterial.SetFloat("_GlowStrength", _ozGlowOvershoot);
            }
        }

        switch (_ozState)
        {
            case PetState.Idle:
                break;
            case PetState.Chasing:
                DoChaseBehavior();
                CheckForPetting();
                _eatCooldown += Time.deltaTime;
                break;
            case PetState.Eating:
                if (_ballEatTarget && _ballAttached)
                {
                    Vector3 targetGrabPos = (_ballAttachmentBoneRight.position + _ballAttachmentBoneLeft.position) * 0.5f;
                    _ballEatTarget.gameObject.transform.position = Vector3.Lerp(_ballEatTarget.gameObject.transform.position, targetGrabPos, 0.5f);
                }
                break;
            case PetState.Listening:
                CheckForPetting();
                break;
            case PetState.Ending:
                // trigger the UFO animation upon approaching
                Vector3 targetPos = _rampBaseHit ? SanctuaryExperience.Instance._finalOzTarget.position + Vector3.up * 3.0f : SanctuaryExperience.Instance._finalOzRamp.position;
                Vector3 targetDistance = transform.position - targetPos;
                float dist = _rampBaseHit ? 1.0f : 0.5f;
                bool endGame = false;
                if (targetDistance.magnitude <= dist)
                {
                    if (!_rampBaseHit)
                    {
                        _rampBaseHit = true;
                        _agent.SetDestination(SanctuaryExperience.Instance._finalOzTarget.position + Vector3.up * 3.0f);
                        AudioManager.SetSnapshot_Ending();
                        MusicManager.Instance.PlayMusic(MusicManager.Instance.OutroMusic);
                    }
                    else
                    {
                        endGame = true;
                    }
                }

                if (endGame)
                {
                    SanctuaryExperience.Instance.FlyAwayUFO();
                    _ozState = PetState.Idle;
                    gameObject.transform.position = _mainCam.position;
                    gameObject.SetActive(false);
                }
                break;
        }

        Vector3 targetLookDirection = _moveTargetDir;

        // rotate Oz to look in the correct direction
        Vector3 currentVelocity = transform.position - _lastPetPosition;
        if (currentVelocity.magnitude > Mathf.Epsilon)
        {
            targetLookDirection = currentVelocity.normalized;
        }
        _lastPetPosition = transform.position;
        _faceDirection = Vector3.Slerp(_faceDirection, targetLookDirection, 0.1f);
        FacePosition(transform.position + _faceDirection);

        if (_lookingAtTarget)
        {
            DoLookAtBehavior(false, true);
        }

        // position shadow
        if (_ozShadowMesh && _ozShadowRoot)
        {
            _ozShadowMesh.position = _ozShadowRoot.position;
            _ozShadowMesh.localPosition = new Vector3(_ozShadowMesh.transform.localPosition.x, 0.005f, _ozShadowMesh.transform.localPosition.z);
            if (_passthroughShell && _passthroughShell.activeSelf)
            {
                _passthroughShell.transform.position = _ozShadowMesh.position + Vector3.up * 0.3f;
            }
        }
    }

    void DoLookAtBehavior(bool instantLook, bool eyesAlso)
    {
        bool inDistance = Vector3.Distance(transform.position, _mainCam.position) <= _lookAtPlayerDistance;
        Vector3 headLook = _headBone.transform.rotation * Vector3.up;
        Vector3 headUp = _headBone.transform.rotation * Vector3.right;
        Vector3 lookPosition = _headBone.position + headLook;

        if (SanctuaryExperience.Instance._currentChapter == SanctuaryExperience.SanctuaryChapter.OzExploresReality
            && MultiToy.Instance.IsWalltoyActive())
        {
            lookPosition = MultiToy.Instance.wallToyTarget.position;
        }
        else if (_ballLookTarget && _ozState != PetState.Listening)
        {
            lookPosition = new Vector3(_ballLookTarget.gameObject.transform.position.x, Mathf.Clamp(_ballLookTarget.gameObject.transform.position.y, 0.0f, 2.0f), _ballLookTarget.gameObject.transform.position.z);
        }
        else
        {
            if (_headBone && inDistance)
            {
                lookPosition = _mainCam.position;
            }
        }

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
        _targetRotation = instantLook ? lookRot : Quaternion.Lerp(_targetRotation, lookRot, 0.05f);

        _headBone.rotation = _targetRotation;

        if (eyesAlso)
        {
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

    public void StartLookTarget()
    {
        _lookingAtTarget = true;
        DoLookAtBehavior(true, true);
    }

    public void EndLookTarget()
    {
        _lookingAtTarget = false;
    }

    public void SetLookDirection(Vector3 lookDirection)
    {
        _moveTargetDir = lookDirection;
    }

    void DoChaseBehavior()
    {
        if (_animator.GetBool("Eating") || _animator.GetBool("Petting"))
        {
            return;
        }

        // get the closest one
        BallCollectable ballBC = SanctuaryExperience.Instance.GetClosestEdibleBall(transform.position);
        if (SanctuaryExperience.Instance._currentChapter == SanctuaryExperience.SanctuaryChapter.OzExploresReality
            && MultiToy.Instance.IsWalltoyActive())
        {
            SetMoveTarget(MultiToy.Instance.wallToyTarget.position - MultiToy.Instance.wallToyTarget.forward * 0.5f, MultiToy.Instance.wallToyTarget.forward);
            _agent.speed = _walkSpeed;
            return;
        }

        _ballLookTarget = ballBC;

        if (ballBC)
        {
            // put Oz target "behind" ball so we get a good view of her eating it
            Vector3 ballDirectionToOz = Vector3.Scale(transform.position - ballBC.gameObject.transform.position, new Vector3(1,0,1));
            Vector3 viewDirectionToBall = Vector3.Scale(ballBC.gameObject.transform.position - _mainCam.position, new Vector3(1,0,1));
            float currentAngle = Vector3.Angle(ballDirectionToOz, viewDirectionToBall);
            if (ballDirectionToOz.magnitude > 0.5f)
            {
                if (currentAngle > 45.0f)
                {
                    ballDirectionToOz = Vector3.Slerp(viewDirectionToBall, ballDirectionToOz, Mathf.Clamp01(45.0f / currentAngle)).normalized;
                    // make sure there's space for Oz there
                    Vector3 idealOzPosition = GetIdealOzPosition(ballBC.gameObject.transform.position, ballDirectionToOz);
                    SetMoveTarget(idealOzPosition, -ballDirectionToOz.normalized, true);
                }
                else
                {
                    SetMoveTarget(new Vector3(ballBC.gameObject.transform.position.x, SanctuaryExperience.Instance.GetFloorHeight(), ballBC.gameObject.transform.position.z), -ballDirectionToOz.normalized, true);
                }
            }
            // once close enough, start the eat animation & lock the ball in
            else if (ballDirectionToOz.magnitude <= 0.5f
                && !ballBC.rigidBody.isKinematic
                && ballBC.rigidBody.velocity.magnitude <= 0.5f
                && _eatCooldown >= 1.0f)
            {
                _animator.SetBool("Eating", true);
                _ozState = PetState.Eating;

                Vector3 idealBallPosition = _moveTargetPos + _moveTargetDir * 0.1f;
                ballBC.PrepareForEating(idealBallPosition);

                _ballEatTarget = ballBC;
                _ballLookTarget = ballBC;
            }

            // slow down when approaching target
            float locomotionBlend = Mathf.Clamp01((transform.position - ballBC.gameObject.transform.position).magnitude - 1.0f);
            _agent.speed = Mathf.Lerp(_walkSpeed, _runSpeed, locomotionBlend);
        }
    }

    // make sure there's space for Oz to eat the ball
    Vector3 GetIdealOzPosition(Vector3 ballPos, Vector3 ballDirectionToOz)
    {
        Vector3 idealPos = ballPos + ballDirectionToOz.normalized * 0.2f;

        Vector3 raycastDir = ballDirectionToOz;
        LayerMask roomLayer = LayerMask.GetMask("RoomBox", "Furniture");
        RaycastHit[] roomboxHit = Physics.RaycastAll(ballPos, ballDirectionToOz, 0.2f, roomLayer);
        foreach (RaycastHit hit in roomboxHit)
        {
            idealPos = hit.point + hit.normal * 0.3f;
        }

        return idealPos;
    }

    void CheckForPetting()
    {
        bool petting = false;
        // get the center eye position, close enough to head top
        Vector3 foreheadPos = (_leftEye.position + _rightEye.position) * 0.5f;
        // only do calculation if the pet is close
        if (_mainCam && Vector3.Distance(_mainCam.transform.position, foreheadPos) < 1.5f)
        {
            Transform Lhand = SanctuaryExperience.Instance._leftHandAnchor;
            Transform Rhand = SanctuaryExperience.Instance._rightHandAnchor;
            if (SanctuaryExperience.Instance._usingHands)
            {
                if (SanctuaryExperience.Instance._leftHand && SanctuaryExperience.Instance._rightHand)
                {
                    Lhand = SanctuaryExperience.Instance._leftHand.Bones[9].Transform;
                    Rhand = SanctuaryExperience.Instance._rightHand.Bones[9].Transform;
                }
            }
            const float minDist = 0.3f;
            petting = (Vector3.Distance(Lhand.position, foreheadPos) < minDist || Vector3.Distance(Rhand.position, foreheadPos) < minDist);
        }

        // check eat hand for distance to head
        _animator.SetBool("Petting", petting);
        if (petting)
        {
            WitConnector.Instance.WitSwitcher(false);
        }
    }

    public void SetOzChapter(SanctuaryExperience.SanctuaryChapter newChapter)
    {
        StopAllCoroutines();
        ResetAnimFlags();
        switch (newChapter)
        {
            case SanctuaryExperience.SanctuaryChapter.Title:
                SanctuaryExperience.Instance._spaceShipAnimator.StopIdleSound();
                break;
            case SanctuaryExperience.SanctuaryChapter.Introduction:
                _collectedBalls = 0;
                _ozState = PetState.Idle;
                transform.position = _mainCam.position;
                break;
            case SanctuaryExperience.SanctuaryChapter.OzBaitsYou:
                _collectedBalls = 0;
                _ozState = PetState.Idle;
                break;
            case SanctuaryExperience.SanctuaryChapter.SearchForOz:
                _collectedBalls = 0;
                _ozState = PetState.Idle;
                SetMaterialSaturation(0);
                EnablePassthroughShell(true);
                break;
            case SanctuaryExperience.SanctuaryChapter.OzExploresReality:
                SanctuaryExperience.Instance._spaceShipAnimator.StopIdleSound();
                _collectedBalls = 0;
                _animator.SetBool("Wonder", true);
                SetMaterialSaturation(1.0f);
                EnablePassthroughShell(false);
                SetLookDirection((_mainCam.position - transform.position).normalized);
                break;
            case SanctuaryExperience.SanctuaryChapter.TheGreatBeyond:
                _ozState = PetState.Idle;
                _ballEatTarget = null;
                _animator.SetBool("Wonder", true);
                EnablePassthroughShell(false);
                break;
            case SanctuaryExperience.SanctuaryChapter.Ending:
                EndLookTarget();
                _agent.SetDestination(SanctuaryExperience.Instance._finalOzRamp.position);
                _rampBaseHit = false;
                _ozState = PetState.Ending;
                _agent.speed = _runSpeed;
                break;
        }
    }

    #region AnimEvents
    // these are called from animations
    public void AttachBallToBone()
    {
        if (_ballEatTarget)
        {
            _ballAttached = true;
        }
    }

    public void ChompBall()
    {
        if (_ballEatTarget)
        {
            _collectedBalls++;
            _ballEatTarget.ForceKill(_headBone.up);
            _ballEatTarget = null;
            _ballAttached = false;
            _ozGlowOvershoot = 1.0f;
        }
    }

    public void FinishEating()
    {
        StartLookTarget();

        _animator.SetBool("Eating", false);

        _eatCooldown = 0.0f;

        if (_collectedBalls >= SanctuaryExperience.Instance._ballTargetCount)
        {
            _animator.SetTrigger("PowerUp");
            _ozState = PetState.Idle;
        }
        else
        {
            ResumeChasing();
        }

        //NOTE: after eating, OZ will try to listen again, if it doesn't have user's focus or can't enable voice somehow, OZ moves away.
        if (CanListen() && WitConnector.Instance.currentFocus) {
            bool reactivateWit = WitConnector.Instance.WitSwitcher(true);
            if (!reactivateWit) MoveAway();
        }
    }

    public void PlayPowerUp()
    {
        Instantiate(_fullyChargedPrefab, transform.position, transform.rotation);
        AudioManager.SetSnapshot_TheGreatBeyond_AfterPowerup();
    }

    public void GoToUFO()
    {
        SanctuaryExperience.Instance.ForceChapter(SanctuaryExperience.SanctuaryChapter.Ending);
    }
    #endregion AnimEvents
    public bool IsGameOver()
    {
        bool gameOver = SanctuaryExperience.Instance._currentChapter == SanctuaryExperience.SanctuaryChapter.Ending;
        gameOver &= _rampBaseHit;
        return gameOver;
    }

    public void ResetAnimFlags()
    {
        _animator.SetBool("Running", false);
        _animator.SetBool("Discovered", false);
        _animator.SetBool("Listening", false);
        _animator.SetBool("Petting", false);
        _animator.ResetTrigger("PowerUp");
        _animator.SetBool("Dislike", false);
        _animator.SetBool("Eating", false);
        _animator.ResetTrigger("Jumping");
        _animator.SetBool("Wonder", false);
        _animator.ResetTrigger("Like");
        _animator.ResetTrigger("Wave");
        _animator.SetBool("ListenFail", false);
    }

    public bool CanListen()
    {
        bool canListen = _collectedBalls < SanctuaryExperience.Instance._ballTargetCount;
        canListen &= SanctuaryExperience.Instance._currentChapter == SanctuaryExperience.SanctuaryChapter.TheGreatBeyond;
        canListen &= (_ozState == PetState.Chasing);
        canListen &= (_ballEatTarget == null);
        canListen &= !_animator.GetBool("Petting");
        canListen &= !_animator.GetBool("Running");
        return canListen;
    }

    public void PlayInitialDiscoveryAnim()
    {
        _animator.speed = 1.0f;
    }

    public void PrepareInitialDiscoveryAnim()
    {
        _animator.SetBool("Discovered", true);
        _animator.speed = 0.0f;
    }

    public void PlayRandomOzDiscoveryAnim()
    {
        _lookingAtTarget = true;
        _boneSim.gameObject.SetActive(false);
        float randomChance = Random.Range(0.0f, 1.0f);
        if (randomChance > 0.5f)
        {
            _animator.SetTrigger("Like");
        }
        else
        {
            _animator.SetTrigger("Wave");
        }
    }

    public void PlaySparkles(bool doPlay)
    {
        OzAudioSource.clip = _sparkleSound;
        if (doPlay)
        {
            OzAudioSource.time = 0.0f;
            OzAudioSource.Play();
        }
        else
        {
            OzAudioSource.Stop();
        }
    }

    public void PlayTeleport()
    {
        Instantiate(_ozTeleportPrefab, _petRoot.position, _petRoot.rotation);
    }

    public void SetMaterialSaturation(float matSat)
    {
        foreach (SkinnedMeshRenderer smr in _ozMeshes)
        {
            smr.sharedMaterial.SetFloat("_SaturationAmount", matSat);
        }
    }

    public void FacePosition(Vector3 worldPosition)
    {
        worldPosition = new Vector3(worldPosition.x, transform.position.y, worldPosition.z);
        _petRoot.rotation = Quaternion.LookRotation(worldPosition - transform.position);
    }

    public void ShotOz()
    {
        _animator.SetBool("Dislike", true);
        _ballEatTarget = null;
        FacePosition(_mainCam.position);
        _ozState = PetState.Angry;
    }

    public void EnablePassthroughShell(bool doEnable)
    {
        _passthroughShell.SetActive(doEnable);
    }

    public void ResumeChasing()
    {
        _ozState = GetResumedState();
        _lookingAtTarget = true;
        ResetAnimFlags();
    }

    PetState GetResumedState()
    {
        if (_ozState == PetState.Ending)
        {
            return PetState.Ending;
        }
        else
        {
            return PetState.Chasing;
        }
    }

    // by default, only move to a new target if it's 1m away from its current one, or forced
    public void SetMoveTarget(Vector3 worldPosition, Vector3 faceDirection, bool force = false)
    {
        Vector3 currTargetPos = new Vector3(worldPosition.x, SanctuaryExperience.Instance.GetFloorHeight(), worldPosition.z);

        if (Vector3.Distance(_moveTargetPos, currTargetPos) > 1.0f || force)
        {
            _moveTargetPos = currTargetPos;
            _moveTargetDir = new Vector3(faceDirection.x, SanctuaryExperience.Instance.GetFloorHeight(), faceDirection.z).normalized;
            _agent.SetDestination(_moveTargetPos);
        }
    }

    void ApproachPlayer()
    {
        Vector3 targetPos = _mainCam.position + _mainCam.forward;
        _agent.speed = _runSpeed;
        _agent.SetDestination(new Vector3(targetPos.x, SanctuaryExperience.Instance.GetFloorHeight(), targetPos.z));
    }

    void PrintDebugOutput()
    {
        Debug.Log("Sanctuary: pet state: " + _ozState);
        Debug.Log("Sanctuary: valid ball: " + (_ballEatTarget == null));
        Debug.Log("Sanctuary: bool Running: " + _animator.GetBool("Running"));
        Debug.Log("Sanctuary: bool Discovered: " + _animator.GetBool("Discovered"));
        Debug.Log("Sanctuary: bool Listening: " + _animator.GetBool("Listening"));
        Debug.Log("Sanctuary: bool Petting: " + _animator.GetBool("Petting"));
        Debug.Log("Sanctuary: bool Dislike: " + _animator.GetBool("Dislike"));
        Debug.Log("Sanctuary: bool Eating: " + _animator.GetBool("Eating"));
        Debug.Log("Sanctuary: bool ListenFail: " + _animator.GetBool("ListenFail"));
        Debug.Log("Sanctuary: bool Wonder: " + _animator.GetBool("Wonder"));
    }

    public void DisplayThought(string thought = "")
    {
        _thoughtBubble.gameObject.SetActive(true);
        _thoughtBubble.ForceSizeUpdate();
        if (thought == "")
        {
            _thoughtBubble.ShowHint();
        }
        else {
            _thoughtBubble.UpdateText(thought);
        }
    }

    public void HideThought() {
        _thoughtBubble.gameObject.SetActive(false);
    }

    //NOTE: Called from ListenFailAni which is attached on ListenFail Animation
    public void MoveAway()
    {
        _animator.SetBool("Listening", false);
        _animator.SetBool("ListenFail", false);
        _listeningIndicator.SetActive(false);
        Vector2 point = Random.insideUnitCircle.normalized * Random.Range(2f,6f);
        Vector3 targetPoint = _mainCam.position + new Vector3(point.x, SanctuaryExperience.Instance.GetFloorHeight(), point.y);
        targetPoint.y = 0;
        _agent.SetDestination(targetPoint);
    }

    //NOTE: Called from CheckListenAvailable script which is attached on Run and Pet_End Animations
    public void CheckListenAvailable() {
        StartCoroutine(TryToReactivateWit());
    }
    
    IEnumerator TryToReactivateWit(float waitTime = 1) {
        yield return new WaitForSeconds(waitTime);
        if (CanListen() && WitConnector.Instance.currentFocus && ArrivedDestination())
        {
            bool reactivateWit = WitConnector.Instance.WitSwitcher(true);
        }
    }
    
    bool ArrivedDestination() {
        if (!_agent.pathPending)
        {
            if (_agent.remainingDistance <= _agent.stoppingDistance)
            {
                if (!_agent.hasPath || _agent.velocity.sqrMagnitude == 0f)
                {
                    return true;
                }
            }
        }
        return false;
    }

    #region VoiceCommand
    public void Listening(bool value)
    {
        if(value) _animator.SetBool("ListenFail", false);
        _listeningIndicator.SetActive(value);
        // make sure to properly exit the animator state
        // for example when it's interrupted by shooting a ball
        if (!value && _animator.GetBool("Listening"))
        {
            _animator.SetTrigger("ForceChase");
        }
        _animator.SetBool("Listening", value);
        _ozState = value ? PetState.Listening : GetResumedState();
        if (value)
        {
            SetLookDirection((_mainCam.position - transform.position).normalized);
        }
    }
    public void ListenFail()
    {
        HideThought();
        _animator.SetBool("ListenFail", true);
        _listeningIndicator.SetActive(false);
    }

    public void VoiceCommandHandler(string actionString)
    {
        _animator.SetBool("Listening", false);
        switch (actionString)
        {
            case "come":
                ApproachPlayer();
                break;
            case "jump":
                _animator.SetTrigger("Jumping");
                break;
            case "hi":
                _animator.SetTrigger("Wave");
                break;
        }
        Listening(false);
        DisplayThought(actionString + "?");
    }
    #endregion VoiceCommand
}
