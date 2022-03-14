// Copyright(c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

public class MultiToy : MonoBehaviour
{
    static public MultiToy Instance = null;

    [Header("Toys")]
    public SanctuaryToy[] _toys;
    int _toyIndex = 0;
    const int _flashlightToyID = 0;
    const int _ballShooterToyID = 1;
    const int _wallToyID = 2;
    bool _canSwitchToys = false;
    VirtualFlashlight _toyFlashlight;
    float _flickerFlashlightStrength = 1.0f;
    float _ballTossCooldownFactor = 1.0f;
    RoomFramer _roomFramer;

    [Header("Meshes")]
    public GameObject _accessoryBase;
    public GameObject _accessoryFlashlight;
    public GameObject _accessoryBallGun;
    public GameObject _accessoryWallToy;

    public GameObject _meshParent;
    public bool _toyVisible { get; private set; }

    public Animator _animator;

    [Header("Sounds")]
    public AudioClip _wallToyLoop;
    public AudioClip _wallToyWallOpen;
    public AudioClip _wallToyWallClose;

    public SoundEntry _unlockToy_1;
    public SoundEntry _switchToy_1;
    public SoundEntry _grabToy_1;
    public SoundEntry _malfunction_1;
    public SoundEntry _ballShoot_1;
    public SoundEntry _flashlightLoop_1;
    public SoundEntry _flashlightFlicker_1;
    public SoundEntry _flashlightAbsorb_1;
    public SoundEntry _flashlightAbsorbLoop_1;
    public SoundEntry _wallToyLoop_1;
    public SoundEntry _wallToyWallOpen_1;
    public SoundEntry _wallToyWallClose_1;

    AudioSource _audioSource;

    [HideInInspector]
    public Transform wallToyTarget;
    bool _flashLightUnlocked = false;
    bool _wallToyUnlocked = false;
    float _pointingAtWallTimer = 0.0f; // ensure player reads the first instructions

    public enum ToyOption
    {
        Flashlight,
        BallGun,
        WallToy,
        None
    };

    // only used by hands
    [HideInInspector]
    public BallCollectable _grabbedBall = null;
    bool _throwBallTaught = false;
    public MeshRenderer _handGlove;

    private void Start()
    {
        SanctuaryExperience.Instance.OnHandOpenDelegate += HandOpened;
        SanctuaryExperience.Instance.OnHandClosedDelegate += HandClosed;
    }

    void HandOpened()
    {

    }

    void HandClosed()
    {
        if (SanctuaryExperience.Instance._currentChapter >= SanctuaryExperience.SanctuaryChapter.SearchForOz)
        {
            if (_toyIndex == _wallToyID)
            {
                _toys[_toyIndex].ActionUp();
            }
        } 
    }

    private void Awake()
    {
        if (!Instance)
        {
            Instance = this;
        }
        _audioSource = GetComponent<AudioSource>();
        _toyFlashlight = this.GetComponent<VirtualFlashlight>();
        _roomFramer = this.GetComponent<RoomFramer>();
        ShowToy(false);
        ShowPassthroughGlove(false);
        // render after passthrough.
        SkinnedMeshRenderer[] parts = _meshParent.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (SkinnedMeshRenderer part in parts)
        {
            foreach (Material mat in part.materials)
            {
                mat.renderQueue = 3000;
            }
        }
    }

    void Update()
    {
        SanctuaryExperience.SanctuaryChapter currentChapter = SanctuaryExperience.Instance._currentChapter;

        Vector3 controllerPos = Vector3.zero;
        Quaternion controllerRot = Quaternion.identity;
        SanctuaryExperience.Instance.GetDominantHand(ref controllerPos, ref controllerRot);

        if (currentChapter != SanctuaryExperience.SanctuaryChapter.OzBaitsYou)
        {
            transform.position = controllerPos;
            transform.rotation = controllerRot;
        }

        if (!TriggerHeld() && _canSwitchToys)
        {
            int lastToy = _toyIndex;
            int highestToyID = _wallToyID;
            if (SelectPreviousToy())
            {
                _toyIndex = _toyIndex - 1 < 0 ? highestToyID : _toyIndex - 1;
            }
            if (SelectNextToy())
            {
                _toyIndex = _toyIndex + 1 > highestToyID ? 0 : _toyIndex + 1;
            }
            if (lastToy != _toyIndex)
            {
                _toys[lastToy].Deactivate();
                _toys[_toyIndex].Activate();

                _switchToy_1.Play();
                SanctuaryTutorial.Instance.HideMessage(SanctuaryTutorial.TutorialMessage.SwitchToy);

                SetToyMesh((ToyOption)_toyIndex);
            }
        }

        if (currentChapter == SanctuaryExperience.SanctuaryChapter.SearchForOz)
        {
            Vector3 ozPos = SanctuaryExperience.Instance._pet.transform.position + Vector3.up * 0.2f;
            float ozDot = Vector3.Dot(GetFlashlightDirection(), (ozPos - transform.position).normalized);
            bool ozActive = SanctuaryExperience.Instance._pet.gameObject.activeSelf;
            if (!SanctuaryExperience.Instance._ozDiscovered)
            {
                if (ozDot >= 0.95f && ozActive)
                {
                    SanctuaryExperience.Instance.PlayOzDiscoveryAnim();
                    SanctuaryExperience.Instance._pet.PlaySparkles(false);
                }
            }
        }

        // switch toys, using hands
        if (SanctuaryExperience.Instance._usingHands)
        {
            _ballTossCooldownFactor += Time.deltaTime;

            if (currentChapter >= SanctuaryExperience.SanctuaryChapter.SearchForOz)
            {
                Transform mainCam = SanctuaryExperience.Instance._mainCamera.transform;
                bool handOutOfView = Vector3.Dot(mainCam.forward, (SanctuaryExperience.Instance.GetPalmBallPosition(controllerRot) - mainCam.position).normalized) < 0.6f;
                int lastToy = _toyIndex;

                // palm out: flashlight
                float palmOut = Vector3.Dot(mainCam.right, controllerRot * Vector3.right);
                float fistStrength = Mathf.Clamp01((SanctuaryExperience.Instance._fistValue - 0.3f)*5.0f);
                float flashL = Mathf.Clamp01(palmOut * 10) * (1-fistStrength) * (_grabbedBall ? 0 : 1);

                _toyIndex = (flashL > 0.5f && _flashLightUnlocked)? _flashlightToyID : _toyIndex;
                _toyFlashlight.SetLightStrength(flashL * _flickerFlashlightStrength * Mathf.Clamp01(_ballTossCooldownFactor-1));

                // palm up: prepare room framer
                float palmUp = Vector3.Dot(mainCam.up, controllerRot * Vector3.forward);
                bool wallToyActive = palmUp > 0.5f && palmOut < 0.0f;
                _toyIndex = (palmOut < 0.0f && _wallToyUnlocked && !_grabbedBall) ? _wallToyID : _toyIndex;
               
                if (handOutOfView || SanctuaryExperience.Instance._pet.IsGameOver())
                {
                    _toyIndex = -1;
                }
                
                if (lastToy != _toyIndex)
                {
                    if (lastToy >= 0)
                    {
                        _toys[lastToy].Deactivate();
                    }
                    if (_toyIndex >= 0)
                    {
                        if (_toyIndex == _flashlightToyID)
                        {
                            SanctuaryTutorial.Instance.HideMessage(SanctuaryTutorial.TutorialMessage.EnableFlashlight);
                        }
                        _toys[_toyIndex].Activate();
                    }
                }
            }
        }

        if (currentChapter >= SanctuaryExperience.SanctuaryChapter.SearchForOz)
        {
            if (_toyIndex >= 0)
            {
                if (TriggerPressed())
                {
                    if (_toyIndex == _ballShooterToyID && SanctuaryExperience.Instance._ballCount > 0)
                    {
                        _animator.SetTrigger("Shoot");
                    }

                    _toys[_toyIndex].ActionDown();
                }

                if (TriggerReleased())
                {
                    _toys[_toyIndex].ActionUp();
                }

                if (TriggerHeld())
                {
                    _toys[_toyIndex].Action();
                }

                if (SecondaryButtonPressed())
                {
                    _toys[_toyIndex].SecondAction();
                }
            }

            BallCollectable tbc = SanctuaryExperience.Instance.GetTargetedBall(transform.position, transform.forward);
            if (tbc)
            {
                if (tbc._ballState == BallCollectable.BallStatus.Available || tbc._ballState == BallCollectable.BallStatus.Released)
                {
                    if (!SanctuaryExperience.Instance._usingHands)
                    {
                        if (IsFlashlightAbsorbing())
                        {
                            tbc.Absorbing((tbc.transform.position - transform.position).normalized);
                            if (tbc.IsBallAbsorbed())
                            {
                                tbc.AbsorbBall();
                                SanctuaryExperience.Instance.DiscoveredBall(_grabbedBall);
                            }
                        }
                    }
                }

                if (SanctuaryExperience.Instance._usingHands && _grabbedBall == null)
                {
                    //bool usingLeft = SanctuaryExperience.Instance.gameController == OVRInput.Controller.LTouch || SanctuaryExperience.Instance.gameController == OVRInput.Controller.LHand;
                    //if (usingLeft)
                    //{
                    //    Vector3 localLeftPos = leftHandGrabSphere.transform.InverseTransformPoint(tbc.transform.position);
                    //    leftHandGrabSphere.center = localLeftPos;
                    //    leftHandPinchSphere.center = localLeftPos;
                    //}
                    //else
                    //{
                    //    Vector3 localRightPos = rightHandGrabSphere.transform.InverseTransformPoint(tbc.transform.position);
                    //    rightHandGrabSphere.center = localRightPos;
                    //    rightHandPinchSphere.center = localRightPos;
                    //}
                }
            }
        }
    }

    // called once there's a roombox
    public void InitializeToys()
    {
        foreach (SanctuaryToy toy in _toys)
        {
            toy.Initialize();
        }
    }

    public void EnableFlashlightCone(bool activate)
    {
        if (activate)
        {
            _toys[_flashlightToyID].Activate();
            _flashLightUnlocked = true;
            if (!SanctuaryExperience.Instance._usingHands)
            {
                _toyIndex = _flashlightToyID;
            }
        }
        else
        {
            _toys[_flashlightToyID].Deactivate();
        }
        _toyFlashlight.SetLightStrength(1.0f);
    }

    public Vector3 GetFlashlightDirection()
    {
        if (_toyFlashlight)
        {
            return _toyFlashlight._lightVolume.transform.up;
        }
        else
        {
            return Vector3.forward;
        }
    }

    public void SetFlickerTime(float normTime)
    {
        float flickerValue = 0.0f;
        if (_toyFlashlight) flickerValue = _toyFlashlight._flickerStrength.Evaluate(normTime);
        _flickerFlashlightStrength = flickerValue;
        if (!SanctuaryExperience.Instance._usingHands)
        {
            _toyFlashlight.SetLightStrength(flickerValue);
        }
    }

    public float EvaluateFlickerCurve(float normTime)
    {
        return _toyFlashlight._flickerStrength.Evaluate(normTime);
    }

    public void DeactivateAllToys()
    {
        foreach (SanctuaryToy toy in _toys)
        {
            toy.Deactivate();
        }
    }

    public void ShowToy(bool doShow)
    {
        _meshParent.SetActive(doShow);
        _toyVisible = doShow;
    }

    public void SetToy(SanctuaryExperience.SanctuaryChapter forcedChapter)
    {
        switch (forcedChapter)
        {
            case SanctuaryExperience.SanctuaryChapter.Title:
            case SanctuaryExperience.SanctuaryChapter.Introduction:
                DeactivateAllToys();
                ShowToy(false);
                ShowPassthroughGlove(false);
                _toyIndex = -1;
                _canSwitchToys = false;
                _throwBallTaught = false;
                _wallToyUnlocked = false;
                _flashLightUnlocked = false;
                _ballTossCooldownFactor = 1.0f;
                break;
            case SanctuaryExperience.SanctuaryChapter.OzBaitsYou:
                DeactivateAllToys();
                ShowToy(false);
                _canSwitchToys = false;
                _flashLightUnlocked = false;
                _toyFlashlight.SetLightStrength(SanctuaryExperience.Instance._usingHands ? 0.0f : 1.0f);
                break;
            case SanctuaryExperience.SanctuaryChapter.SearchForOz:
                ShowToy(true);
                _canSwitchToys = false;
                break;
            case SanctuaryExperience.SanctuaryChapter.OzExploresReality:
                DeactivateAllToys();
                ShowToy(true);
                _canSwitchToys = false;
                _toyIndex = -1;
                _wallToyUnlocked = false;
                break;
            case SanctuaryExperience.SanctuaryChapter.TheGreatBeyond:
                ShowToy(true);
                _canSwitchToys = true;
                if (!SanctuaryExperience.Instance._usingHands)
                {
                    SanctuaryTutorial.Instance.DisplayMessage(SanctuaryTutorial.TutorialMessage.SwitchToy);
                }
                break;
            case SanctuaryExperience.SanctuaryChapter.Ending:
                _canSwitchToys = true;
                break;
        }
    }

    public void UnlockBallShooter()
    {
        if (!SanctuaryExperience.Instance._usingHands)
        {
            _unlockToy_1.Play();
            SanctuaryTutorial.Instance.DisplayMessage(SanctuaryTutorial.TutorialMessage.ShootBall);
            _toys[_flashlightToyID].Deactivate();
            _toys[_ballShooterToyID].Activate();
            _toyIndex = _ballShooterToyID;
            SetToyMesh(ToyOption.BallGun);
        }
        _flashlightLoop_1.Stop();
        _flashlightAbsorbLoop_1.Stop();
    }

    public void UnlockWallToy()
    {
        _flashlightLoop_1.Stop();
        _flashlightAbsorbLoop_1.Stop();
        _unlockToy_1.Play();
        if (!SanctuaryExperience.Instance._usingHands)
        {
            SanctuaryTutorial.Instance.DisplayMessage(SanctuaryTutorial.TutorialMessage.ShootWall);
            _toys[_ballShooterToyID].Deactivate();
            _toys[_wallToyID].Activate();
            _toyIndex = _wallToyID;
            SetToyMesh(ToyOption.WallToy);
        }
        else
        {
            SanctuaryTutorial.Instance.DisplayMessage(SanctuaryTutorial.TutorialMessage.AimWall);
        }
        _wallToyUnlocked = true;
    }

    bool TriggerPressed()
    {
        OVRInput.RawButton triggerButton = SanctuaryExperience.Instance._gameController == OVRInput.Controller.RTouch ? OVRInput.RawButton.RIndexTrigger : OVRInput.RawButton.LIndexTrigger;
        return OVRInput.GetDown(triggerButton);
    }

    bool TriggerReleased()
    {
        OVRInput.RawButton triggerButton = SanctuaryExperience.Instance._gameController == OVRInput.Controller.RTouch ? OVRInput.RawButton.RIndexTrigger : OVRInput.RawButton.LIndexTrigger;
        return OVRInput.GetUp(triggerButton);
    }

    bool TriggerHeld()
    {
        OVRInput.RawButton triggerButton = SanctuaryExperience.Instance._gameController == OVRInput.Controller.RTouch ? OVRInput.RawButton.RIndexTrigger : OVRInput.RawButton.LIndexTrigger;
        return OVRInput.Get(triggerButton);
    }

    bool SecondaryButtonPressed()
    {
        OVRInput.RawButton pressedButton = SanctuaryExperience.Instance._gameController == OVRInput.Controller.RTouch ? OVRInput.RawButton.A : OVRInput.RawButton.X;
        return OVRInput.GetUp(pressedButton);
    }

    bool SelectPreviousToy()
    {
        OVRInput.RawButton selectionButton = SanctuaryExperience.Instance._gameController == OVRInput.Controller.RTouch ? OVRInput.RawButton.RThumbstickLeft : OVRInput.RawButton.LThumbstickLeft;
        return OVRInput.GetUp(selectionButton);
    }

    public void EndGame()
    {
        StopLoopingSound();
        /*
        wallToyLoop_1.Stop();
        flashlightLoop_1.Stop();
        flashlightAbsorbLoop_1.Stop();
        */
        _canSwitchToys = false;
    }

    bool SelectNextToy()
    {
        OVRInput.RawButton selectionButton = SanctuaryExperience.Instance._gameController == OVRInput.Controller.RTouch ? OVRInput.RawButton.RThumbstickRight : OVRInput.RawButton.LThumbstickRight;
        return OVRInput.GetUp(selectionButton);
    }

    public void StopLoopingSound()
    {
        _wallToyLoop_1.Stop();
        _flashlightLoop_1.Stop();
        _flashlightAbsorbLoop_1.Stop();
        if (_audioSource)
        {
            _audioSource.Stop();
        }
    }

    public bool IsFlashlightActive()
    {
        return (_toyIndex == _flashlightToyID);
    }

    public bool IsWalltoyActive()
    {
        if (_roomFramer)
        {
            return _roomFramer.IsHighlightingWall();
        }
        return false;
    }

    public bool IsFlashlightAbsorbing()
    {
        return _toyFlashlight._absorbingBall;
    }

    public void SetToyMesh(ToyOption newToy)
    {
        _accessoryBase.SetActive(newToy != ToyOption.None);
        _accessoryFlashlight.SetActive(newToy == ToyOption.Flashlight);
        _accessoryBallGun.SetActive(newToy == ToyOption.BallGun);
        _accessoryWallToy.SetActive(newToy == ToyOption.WallToy);

        switch (newToy)
        {
            case ToyOption.Flashlight:
                _animator.SetTrigger("SetFlashlight");
                break;
            case ToyOption.BallGun:
                _animator.SetTrigger("SetShooter");
                break;
            case ToyOption.WallToy:
                _animator.SetTrigger("SetWallBeam");
                break;
        }
    }

    public void ShowPassthroughGlove(bool doShow, bool isRightHand = true)
    {
        if (_handGlove)
        {
            _handGlove.gameObject.SetActive(doShow);
            _handGlove.transform.localScale = new Vector3(isRightHand ? 1.0f : -1.0f, 1.0f, 1.0f);
        }
    }
 
    public Vector3 GetMuzzlePosition()
    {
        Vector3 endPosition = transform.position + transform.forward * 0.109f;
        if (SanctuaryExperience.Instance._usingHands)
        {
            OVRSkeleton refHand = SanctuaryExperience.Instance.GetActiveHand();
            return refHand.Bones[21].Transform.position;
        }
        return endPosition;
    }

    public void PointingAtWall()
    {
        if (SanctuaryTutorial.Instance._currentMessage == SanctuaryTutorial.TutorialMessage.AimWall)
        {
            _pointingAtWallTimer += Time.deltaTime;
            if (_pointingAtWallTimer >= 3.0f)
            {
                _pointingAtWallTimer = 0.0f;
                SanctuaryTutorial.Instance.DisplayMessage(SanctuaryTutorial.TutorialMessage.ShootWall);
            }
        }
    }

    public void ChildLightCone(bool usingHands)
    {
        if (usingHands)
        {
            _toyFlashlight._flashlight.transform.SetParent(this.transform);
        }
        else
        {
            _toyFlashlight._flashlight.transform.parent = _toyFlashlight._lightBone;
            _toyFlashlight._flashlight.transform.localPosition = -0.1f * Vector3.forward;
            _toyFlashlight._flashlight.transform.localRotation = Quaternion.identity;
        }
    }

    public void GrabBall(BallCollectable bc)
    {
        bc.AbsorbBall();
        bc.SetState(BallCollectable.BallStatus.Grabbed);
        _grabbedBall = bc;
        SanctuaryExperience.Instance.DiscoveredBall(bc);
        SanctuaryTutorial.Instance.HideMessage(SanctuaryTutorial.TutorialMessage.BallSearch);

        if (!_throwBallTaught)
        {
            SanctuaryTutorial.Instance.DisplayMessage(SanctuaryTutorial.TutorialMessage.ShootBall);
            _throwBallTaught = true;
        }
    }

    public void ThrewBall()
    {
        _ballTossCooldownFactor = 0.0f;
        _grabbedBall = null;
    }

    // when using hands, disable the toy's collision
    public void EnableCollision(bool doEnable)
    {
        if (GetComponent<BoxCollider>())
        {
            GetComponent<BoxCollider>().enabled = doEnable;
        }
    }

    // when pressing the Oculus button, hide the toy or else it snaps to the camera position
    private void OnApplicationFocus(bool pause)
    {
        if (SanctuaryExperience.Instance._currentChapter >= SanctuaryExperience.SanctuaryChapter.SearchForOz)
        {
            ShowToy(pause);
            if (pause)
            {
                _toys[_toyIndex].Activate();
            }
            else
            {
            
                _toys[_toyIndex].Deactivate();
            }
        }
    }
}
