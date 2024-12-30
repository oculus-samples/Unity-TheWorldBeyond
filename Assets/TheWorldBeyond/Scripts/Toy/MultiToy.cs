// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

public class MultiToy : MonoBehaviour
{
    static public MultiToy Instance = null;

    [Header("Toys")]
    public WorldBeyondToy[] _toys;
    int _toyIndexController = (int)ToyOption.None;
    int _toyIndexHand = (int)ToyOption.None;
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
        None = -1,
        Flashlight = 0,
        BallGun = 1,
        WallToy = 2,
    };

    // only used by hands
    [HideInInspector]
    public BallCollectable _grabbedBall = null;
    bool _throwBallTaught = false;
    public MeshRenderer _handGlove;

    private void Start()
    {
        WorldBeyondManager.Instance.OnHandClosedDelegate += HandClosed;
    }

    /// <summary>
    /// Only used for triggering the wall toy.
    /// </summary>
    void HandClosed()
    {
        if (WorldBeyondManager.Instance._currentChapter >= WorldBeyondManager.GameChapter.SearchForOppy)
        {
            if (_toyIndexHand == (int)ToyOption.WallToy)
            {
                _toys[_toyIndexHand].ActionUp();
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
        WorldBeyondManager.GameChapter currentChapter = WorldBeyondManager.Instance._currentChapter;

        Vector3 controllerPos = Vector3.zero;
        Quaternion controllerRot = Quaternion.identity;
        WorldBeyondManager.Instance.GetDominantHand(ref controllerPos, ref controllerRot);

        if (currentChapter != WorldBeyondManager.GameChapter.OppyBaitsYou)
        {
            transform.position = controllerPos;
            transform.rotation = controllerRot;
        }

        if (!TriggerHeld() && _canSwitchToys)
        {
            int lastToy = _toyIndexController;
            int highestToyID = (int)ToyOption.WallToy;
            if (CycleToy(false))
            {
                _toyIndexController = _toyIndexController - 1 < 0 ? highestToyID : _toyIndexController - 1;
            }
            if (CycleToy(true))
            {
                _toyIndexController = _toyIndexController + 1 > highestToyID ? 0 : _toyIndexController + 1;
            }
            if (lastToy != _toyIndexController)
            {
                _toys[lastToy].Deactivate();
                _toys[_toyIndexController].Activate();

                _switchToy_1.Play();
                WorldBeyondTutorial.Instance.HideMessage(WorldBeyondTutorial.TutorialMessage.SwitchToy);

                SetToyMesh((ToyOption)_toyIndexController);
            }
        }

        if (currentChapter == WorldBeyondManager.GameChapter.SearchForOppy)
        {
            Vector3 oppyPos = WorldBeyondManager.Instance._pet.transform.position + Vector3.up * 0.2f;
            float oppyDot = Vector3.Dot(GetFlashlightDirection(), (oppyPos - transform.position).normalized);
            bool oppyActive = WorldBeyondManager.Instance._pet.gameObject.activeSelf;
            if (!WorldBeyondManager.Instance._oppyDiscovered)
            {
                if (oppyDot >= 0.95f && oppyActive)
                {
                    WorldBeyondManager.Instance.PlayOppyDiscoveryAnim();
                    WorldBeyondManager.Instance._pet.PlaySparkles(false);
                }
            }
        }

        var currentToyIndex = _toyIndexController;
        // switch toys, using hands
        if (WorldBeyondManager.Instance._usingHands)
        {
            _ballTossCooldownFactor += Time.deltaTime;
            currentToyIndex = _toyIndexHand;
            if (currentChapter >= WorldBeyondManager.GameChapter.SearchForOppy)
            {
                Transform mainCam = WorldBeyondManager.Instance._mainCamera.transform;
                bool handOutOfView = Vector3.Dot(mainCam.forward, (controllerPos - mainCam.position).normalized) < 0.5f;
                int lastToy = _toyIndexHand;

                // palm out: flashlight
                float palmOut = Vector3.Dot(mainCam.forward, controllerRot * Vector3.forward);
                float fistStrength = Mathf.Clamp01((WorldBeyondManager.Instance._fistValue - 0.3f) * 5.0f);
                float flashL = Mathf.Clamp01(palmOut * 10) * (1 - fistStrength) * (_grabbedBall ? 0 : 1);

                currentToyIndex = (flashL > 0.5f && _flashLightUnlocked) ? (int)ToyOption.Flashlight : currentToyIndex;
                _toyFlashlight.SetLightStrength(flashL * _flickerFlashlightStrength * Mathf.Clamp01(_ballTossCooldownFactor - 1));

                // palm up: prepare room framer
                float palmUp = Vector3.Dot(mainCam.right, controllerRot * Vector3.right);
                currentToyIndex = (palmUp < 0.0f && _wallToyUnlocked && !_grabbedBall) ? (int)ToyOption.WallToy : currentToyIndex;

                if (lastToy != currentToyIndex)
                {
                    if (lastToy > (int)ToyOption.None)
                    {
                        _toys[lastToy].Deactivate();
                    }
                    if (currentToyIndex > (int)ToyOption.None)
                    {
                        if (currentToyIndex == (int)ToyOption.Flashlight)
                        {
                            WorldBeyondTutorial.Instance.HideMessage(WorldBeyondTutorial.TutorialMessage.EnableFlashlight);
                        }
                        _toys[currentToyIndex].Activate();
                    }
                }

                _toyIndexHand = currentToyIndex;
            }
        }

        if (currentChapter >= WorldBeyondManager.GameChapter.SearchForOppy)
        {
            if (currentToyIndex > (int)ToyOption.None)
            {
                if (TriggerPressed())
                {
                    if (currentToyIndex == (int)ToyOption.BallGun && WorldBeyondManager.Instance._ballCount > 0)
                    {
                        _animator.SetTrigger("Shoot");
                    }

                    _toys[currentToyIndex].ActionDown();
                }

                if (TriggerReleased())
                {
                    _toys[currentToyIndex].ActionUp();
                }

                if (TriggerHeld())
                {
                    _toys[currentToyIndex].Action();
                }
            }

            BallCollectable tbc = WorldBeyondManager.Instance.GetTargetedBall(transform.position, transform.forward);
            if (tbc)
            {
                if (tbc._ballState == BallCollectable.BallStatus.Available ||
                    tbc._ballState == BallCollectable.BallStatus.Hidden ||
                    tbc._ballState == BallCollectable.BallStatus.Released)
                {
                    if (!WorldBeyondManager.Instance._usingHands)
                    {
                        if (IsFlashlightAbsorbing())
                        {
                            tbc.Absorbing((tbc.transform.position - transform.position).normalized);
                            if (tbc.IsBallAbsorbed())
                            {
                                tbc.AbsorbBall();
                                WorldBeyondManager.Instance.DiscoveredBall(_grabbedBall);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Prepare all the Multitoy's toys (virtual flashlight, ball shooter, wall toggler).
    /// </summary>
    public void InitializeToys()
    {
        foreach (WorldBeyondToy toy in _toys)
        {
            toy.Initialize();
        }
    }

    /// <summary>
    /// Basically the power button for the flashlight effect.
    /// </summary>
    public void EnableFlashlightCone(bool activate)
    {
        if (activate)
        {
            _toys[(int)ToyOption.Flashlight].Activate();
            _flashLightUnlocked = true;
            if (!WorldBeyondManager.Instance._usingHands)
            {
                _toyIndexController = (int)ToyOption.Flashlight;
            }
        }
        else
        {
            _toys[(int)ToyOption.Flashlight].Deactivate();
        }
        _toyFlashlight.SetLightStrength(1.0f);
    }

    /// <summary>
    /// The direction the light cone is pointing; not always forward, due to the wobbly head.
    /// </summary>
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

    /// <summary>
    /// Assign the strength of the flashlight cone, based upon an evaluation of the curve set in the Inspector.
    /// </summary>
    public void SetFlickerTime(float normTime)
    {
        float flickerValue = 0.0f;
        if (_toyFlashlight) flickerValue = EvaluateFlickerCurve(normTime);
        _flickerFlashlightStrength = flickerValue;
        if (!WorldBeyondManager.Instance._usingHands)
        {
            _toyFlashlight.SetLightStrength(flickerValue);
        }
    }

    /// <summary>
    /// Sample the flicker curve at a normalized time.
    /// </summary>
    public float EvaluateFlickerCurve(float normTime)
    {
        return _toyFlashlight._flickerStrength.Evaluate(normTime);
    }

    /// <summary>
    /// Cleanly shut down the toys.
    /// </summary>
    public void DeactivateAllToys()
    {
        foreach (WorldBeyondToy toy in _toys)
        {
            toy.Deactivate();
        }
    }

    /// <summary>
    /// Show/hide the toy mesh.
    /// </summary>
    public void ShowToy(bool doShow)
    {
        _meshParent.SetActive(doShow);
        _toyVisible = doShow;
    }

    /// <summary>
    /// Prepare the proper toy, depending on the story chapter.
    /// </summary>
    public void SetToy(WorldBeyondManager.GameChapter forcedChapter)
    {
        switch (forcedChapter)
        {
            case WorldBeyondManager.GameChapter.Title:
            case WorldBeyondManager.GameChapter.Introduction:
                DeactivateAllToys();
                ShowToy(false);
                _toyIndexController = (int)ToyOption.Flashlight;
                _toyIndexHand = (int)ToyOption.None;
                _canSwitchToys = false;
                _throwBallTaught = false;
                _wallToyUnlocked = false;
                _flashLightUnlocked = false;
                _ballTossCooldownFactor = 1.0f;
                break;
            case WorldBeyondManager.GameChapter.OppyBaitsYou:
                DeactivateAllToys();
                ShowToy(false);
                _canSwitchToys = false;
                _flashLightUnlocked = false;
                _toyFlashlight.SetLightStrength(WorldBeyondManager.Instance._usingHands ? 0.0f : 1.0f);
                break;
            case WorldBeyondManager.GameChapter.SearchForOppy:
                ShowToy(true);
                _canSwitchToys = false;
                break;
            case WorldBeyondManager.GameChapter.OppyExploresReality:
                DeactivateAllToys();
                ShowToy(true);
                _toyIndexController = (int)ToyOption.None;
                _toyIndexHand = (int)ToyOption.None;
                _canSwitchToys = false;
                _wallToyUnlocked = false;
                break;
            case WorldBeyondManager.GameChapter.TheGreatBeyond:
                ShowToy(true);
                _canSwitchToys = true;
                if (!WorldBeyondManager.Instance._usingHands)
                {
                    WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.SwitchToy);
                }
                break;
            case WorldBeyondManager.GameChapter.Ending:
                _canSwitchToys = true;
                break;
        }
    }

    /// <summary>
    /// Activate the ball shooter component of the MultiToy.
    /// </summary>
    public void UnlockBallShooter()
    {
        if (!WorldBeyondManager.Instance._usingHands)
        {
            //Ballcount can be 0 when using hands, and you are not able to change to pickup balls at this point when using controllers.
            if (WorldBeyondManager.Instance._ballCount == 0)
            {
                // Give two free balls so that you don't get stuck
                WorldBeyondManager.Instance._ballCount = 2;
            }
            _unlockToy_1.Play();
            WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.ShootBall);
            _toys[(int)ToyOption.Flashlight].Deactivate();
            _toys[(int)ToyOption.BallGun].Activate();
            _toyIndexController = (int)ToyOption.BallGun;
            SetToyMesh(ToyOption.BallGun);
        }
        _flashlightLoop_1.Stop();
        _flashlightAbsorbLoop_1.Stop();
    }

    /// <summary>
    /// Activate the wall toggler component of the MultiToy.
    /// </summary>
    public void UnlockWallToy()
    {
        _flashlightLoop_1.Stop();
        _flashlightAbsorbLoop_1.Stop();
        _unlockToy_1.Play();
        if (!WorldBeyondManager.Instance._usingHands)
        {
            WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.ShootWall);
            _toys[(int)ToyOption.BallGun].Deactivate();
            _toys[(int)ToyOption.WallToy].Activate();
            _toyIndexController = (int)ToyOption.WallToy;
            SetToyMesh(ToyOption.WallToy);
        }
        else
        {
            WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.AimWall);
        }
        _wallToyUnlocked = true;
    }

    /// <summary>
    /// Handler to abstract input.
    /// </summary>
    bool TriggerPressed()
    {
        OVRInput.RawButton triggerButton = WorldBeyondManager.Instance._gameController == OVRInput.Controller.RTouch ? OVRInput.RawButton.RIndexTrigger : OVRInput.RawButton.LIndexTrigger;
        return OVRInput.GetDown(triggerButton);
    }

    /// <summary>
    /// Handler to abstract input.
    /// </summary>
    bool TriggerReleased()
    {
        OVRInput.RawButton triggerButton = WorldBeyondManager.Instance._gameController == OVRInput.Controller.RTouch ? OVRInput.RawButton.RIndexTrigger : OVRInput.RawButton.LIndexTrigger;
        return OVRInput.GetUp(triggerButton);
    }

    /// <summary>
    /// Handler to abstract input.
    /// </summary>
    bool TriggerHeld()
    {
        OVRInput.RawButton triggerButton = WorldBeyondManager.Instance._gameController == OVRInput.Controller.RTouch ? OVRInput.RawButton.RIndexTrigger : OVRInput.RawButton.LIndexTrigger;
        return OVRInput.Get(triggerButton);
    }

    /// <summary>
    /// Cycle through toy options.
    /// </summary>
    bool CycleToy(bool cycleForward)
    {
        bool usingRightController = WorldBeyondManager.Instance._gameController == OVRInput.Controller.RTouch;
        OVRInput.RawButton rightCycleButton = cycleForward ? OVRInput.RawButton.RThumbstickRight : OVRInput.RawButton.RThumbstickLeft;
        OVRInput.RawButton leftCycleButton = cycleForward ? OVRInput.RawButton.LThumbstickRight : OVRInput.RawButton.LThumbstickLeft;
        OVRInput.RawButton selectionButton = usingRightController ? rightCycleButton : leftCycleButton;
        return OVRInput.GetUp(selectionButton);
    }

    /// <summary>
    /// Effectively, "lock" toy
    /// </summary>
    public void EndGame()
    {
        StopLoopingSound();
        _canSwitchToys = false;
    }

    /// <summary>
    /// Kill all audio associated with Multitoy.
    /// </summary>
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

    /// <summary>
    /// Self-explanatory.
    /// </summary>
    public bool IsFlashlightActive()
    {
        return (_toyIndexController == (int)ToyOption.Flashlight || _toyIndexHand == (int)ToyOption.Flashlight);
    }

    /// <summary>
    /// If the wall toy is active, and it's highlighting a valid surface.
    /// </summary>
    public bool IsWalltoyActive()
    {
        if (_roomFramer)
        {
            return _roomFramer.IsHighlightingWall();
        }
        return false;
    }

    /// <summary>
    /// Is player holding down the trigger while using the flashlight.
    /// </summary>
    public bool IsFlashlightAbsorbing()
    {
        return _toyFlashlight._absorbingBall;
    }

    /// <summary>
    /// Show the correct Multitoy attachment, depending on the toy
    /// </summary>
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

    /// <summary>
    /// Toggle the passthrough mesh that occludes the Multitoy handle.
    /// </summary>
    public void ShowPassthroughGlove(bool doShow, bool isRightHand = true)
    {
        if (_handGlove)
        {
            _handGlove.gameObject.SetActive(doShow);
            _handGlove.transform.localScale = new Vector3(isRightHand ? 1.0f : -1.0f, 1.0f, 1.0f);
        }
    }

    /// <summary>
    /// The starting point for any Multitoy effects.
    /// </summary>
    public Vector3 GetMuzzlePosition()
    {
        Vector3 endPosition = transform.position + transform.forward * 0.109f;
        if (WorldBeyondManager.Instance._usingHands)
        {
            OVRSkeleton refHand = WorldBeyondManager.Instance.GetActiveHand();
            return refHand.Bones[21].Transform.position;
        }
        return endPosition;
    }

    /// <summary>
    /// Track & handle the UI prompt informing the player about the wall.
    /// </summary>
    public void PointingAtWall()
    {
        if (WorldBeyondTutorial.Instance._currentMessage == WorldBeyondTutorial.TutorialMessage.AimWall)
        {
            _pointingAtWallTimer += Time.deltaTime;
            if (_pointingAtWallTimer >= 3.0f)
            {
                _pointingAtWallTimer = 0.0f;
                WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.ShootWall);
            }
        }
    }

    /// <summary>
    /// One-time set up of lightcone, depending on using hands or not.
    /// </summary>
    public void ChildLightCone(bool usingHands)
    {
        if (usingHands)
        {
            _toyFlashlight._flashlight.transform.SetParent(this.transform);
            _toyFlashlight._flashlight.transform.localPosition = Vector3.zero;
            _toyFlashlight._flashlight.transform.localRotation = Quaternion.identity;
        }
        else
        {
            _toyFlashlight._flashlight.transform.parent = _toyFlashlight._lightBone;
            _toyFlashlight._flashlight.transform.localPosition = -0.1f * Vector3.forward;
            _toyFlashlight._flashlight.transform.localRotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// Handle when a ball is grabbed, when using hands only.
    /// </summary>
    public void GrabBall(BallCollectable bc)
    {
        if (_grabbedBall)
        {
            return;
        }
        bc.AbsorbBall();
        bc.SetState(BallCollectable.BallStatus.Grabbed);
        _grabbedBall = bc;
        WorldBeyondManager.Instance.DiscoveredBall(bc);
        WorldBeyondTutorial.Instance.HideMessage(WorldBeyondTutorial.TutorialMessage.BallSearch);

        if (!_throwBallTaught)
        {
            WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.ShootBall);
            _throwBallTaught = true;
        }
    }

    /// <summary>
    /// Handle when a ball is thrown, when using hands only.
    /// </summary>
    public void ThrewBall()
    {
        _ballTossCooldownFactor = 0.0f;
        _grabbedBall = null;
    }

    /// <summary>
    /// When using hands, disable the toy's collision.
    /// </summary>
    public void EnableCollision(bool doEnable)
    {
        if (GetComponent<BoxCollider>())
        {
            GetComponent<BoxCollider>().enabled = doEnable;
        }
    }

    /// <summary>
    /// When pressing the Oculus button, hide the toy or else it snaps to the camera position.
    /// </summary>
    private void OnApplicationFocus(bool pause)
    {
        if (WorldBeyondManager.Instance._currentChapter >= WorldBeyondManager.GameChapter.SearchForOppy)
        {
            if (_toyIndexController < 0 || _toyIndexController >= _toys.Length || _toys[_toyIndexController] == null)
            {
                return;
            }
            ShowToy(pause);
            if (pause)
            {
                _toys[_toyIndexController].Activate();
            }
            else
            {

                _toys[_toyIndexController].Deactivate();
            }
        }
    }

    public void UseHands(bool usingHands, bool rightHand = false)
    {
        if (usingHands)
        {
            if (_toyVisible && WorldBeyondManager.Instance._currentChapter >= WorldBeyondManager.GameChapter.SearchForOppy)
            {
                SetToyMesh(ToyOption.None);
            }
            ShowPassthroughGlove(false);
        }
        else
        {
            SetToyMesh((ToyOption)_toyIndexController);
            if (_toyVisible && WorldBeyondManager.Instance._currentChapter >= WorldBeyondManager.GameChapter.SearchForOppy)
            {
                ShowPassthroughGlove(true, rightHand);
            }
        }
        _toyFlashlight.SetLightStrength(usingHands ? 0.0f : 1.0f);
        ChildLightCone(usingHands);
    }

    public ToyOption GetCurrentToy()
    {
        if (WorldBeyondManager.Instance._usingHands)
        {
            return (ToyOption)_toyIndexHand;
        }
        else
        {
            return (ToyOption)_toyIndexController;
        }
    }
}
