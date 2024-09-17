// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Oculus.Interaction;
using Oculus.Interaction.DistanceReticles;
using UnityEngine;
using Random = UnityEngine.Random;

public class WorldBeyondManager : MonoBehaviour
{
    static public WorldBeyondManager Instance = null;

    [Header("Scene Preview")]
    [SerializeField] private OVRSceneManager _sceneManager;
    [SerializeField] private OVRPassthroughLayer _passthroughLayer;
    bool _sceneModelLoaded = false;
    float _floorHeight = 0.0f;
    // after the Scene has been loaded successfuly, we still wait a frame before the data has "settled"
    // e.g. VolumeAndPlaneSwitcher needs to happen first, and script execution order also isn't fixed by default
    int _frameWait = 0;

    [HideInInspector]
    public OVRSceneAnchor[] _sceneAnchors;

    [Header("Game Pieces")]
    public VirtualPet _pet;
    int _oppyDiscoveryCount = 0;
    [HideInInspector]
    public bool _oppyDiscovered = false;
    public VirtualRoom _vrRoom;
    public LightBeam _lightBeam;
    Vector3 _toyBasePosition = Vector3.zero;
    public Transform _finalUfoTarget;
    public Transform _finalUfoRamp;
    [HideInInspector]
    public SpaceshipTrigger _spaceShipAnimator;

    // Energy balls
    Transform _ballContainer;
    public GameObject _ballPrefab;
    BallCollectable _hiddenBallCollectable = null;
    Vector3 _hiddenBallPosition = Vector3.zero;

    // the little gems spawned when a ball collides
    List<BallDebris> _ballDebrisObjects;
    const int _maxBallDebris = 100;

    // How many balls the player currently has
    [HideInInspector]
    public int _ballCount = 0;
    const int _startingBallCount = 0;
    // How many balls Oppy should eat before heading to the UFO
    // only starts incrementing during TheGreatBeyond chapter
    public int _oppyTargetBallCount { private set; get; } = 2;
    float _ballSpawnTimer = 0.0f;
    const float _spawnTimeMin = 3.0f;
    const float _spawnTimeMax = 6.0f;
    bool _shouldSpawnBall = false;
    public GameObject _worldShockwave;
    public Material[] _environmentMaterials;

    [Header("Overlays")]
    public Camera _mainCamera;
    public MeshRenderer _fadeSphere;
    GameObject _backgroundFadeSphere;
    public GameObject _titleScreenPrefab;
    public GameObject _endScreenPrefab;
    GameObject _titleScreen;
    GameObject _endScreen;
    float _vrRoomEffectTimer = 0.0f;
    float _vrRoomEffectMaskTimer = 0.0f;
    float _titleFadeTimer = 0.0f;
    PassthroughStylist _passthroughStylist;
    Color _cameraDark = new Color(0, 0, 0, 0.75f);

    public enum GameChapter
    {
        Void,               // waiting to find the Scene objects
        Title,              // the title screen
        Introduction,       // Passthrough fades in from black
        OppyBaitsYou,       // light beam is visible
        SearchForOppy,      // flashlight-hunting for Oppy & balls
        OppyExploresReality,// Oppy walks around your room
        TheGreatBeyond,     // room walls come down
        Ending              // Oppy has collected all balls, flies away in ship
    };
    public GameChapter _currentChapter { get; private set; }

    [Header("Hands")]
    public OVRSkeleton _leftHand;
    public OVRSkeleton _rightHand;
    OVRHand _leftOVR;
    OVRHand _rightOVR;
    public Transform _leftHandAnchor;
    public Transform _rightHandAnchor;
    public OVRInput.Controller _gameController { get; private set; }
    // hand input for grabbing is handled by the Interaction SDK
    // otherwise, we track some basic custom poses (palm up/out, hand closed)
    public bool _usingHands { get; private set; }
    bool _handClosed = false;
    public delegate void OnHand();
    public OnHand OnHandOpenDelegate;
    public OnHand OnHandClosedDelegate;
    public OnHand OnHandDelegate;
    [HideInInspector]
    public float _fistValue = 0.0f;
    public HandVisual _leftHandVisual;
    public HandVisual _rightHandVisual;
    public HandWristOffset _leftPointerOffset;
    public HandWristOffset _rightPointerOffset;

    public DistantInteractionLineVisual _interactionLineLeft;
    public DistantInteractionLineVisual _interactionLineRight;

    private float _leftHandGrabbedBallLastDistance = Mathf.Infinity;
    private float _rightHandGrabbedBallLastDistance = Mathf.Infinity;
    private void Awake()
    {
        if (!Instance)
        {
            Instance = this;
        }

        _currentChapter = GameChapter.Void;
        _gameController = OVRInput.Controller.RTouch;
        _fadeSphere.gameObject.SetActive(true);
        _fadeSphere.sharedMaterial.SetColor("_Color", Color.black);

        // copy the black fade sphere to be behind the intro title
        // this shouldn't be necessary once color controls can be added to color PT
        _backgroundFadeSphere = Instantiate(_fadeSphere.gameObject, _fadeSphere.transform.parent);

        _usingHands = false;
        _leftOVR = _leftHand.GetComponent<OVRHand>();
        _rightOVR = _rightHand.GetComponent<OVRHand>();

        _passthroughLayer.colorMapEditorType = OVRPassthroughLayer.ColorMapEditorType.None;

        GameObject spawnedBalls = new GameObject("SpawnedBalls");
        _ballContainer = spawnedBalls.transform;

        _ballDebrisObjects = new List<BallDebris>();

        _passthroughLayer.textureOpacity = 0;
        _passthroughStylist = this.gameObject.AddComponent<PassthroughStylist>();
        _passthroughStylist.Init(_passthroughLayer);
        PassthroughStylist.PassthroughStyle darkPassthroughStyle = new PassthroughStylist.PassthroughStyle(
            new Color(0, 0, 0, 0),
            1.0f,
            0.0f,
            0.0f,
            0.0f,
            true,
            Color.black,
            Color.black,
            Color.black);
        _passthroughStylist.ForcePassthroughStyle(darkPassthroughStyle);

        _spaceShipAnimator = _finalUfoTarget.GetComponent<SpaceshipTrigger>();

        _titleScreen = Instantiate(_titleScreenPrefab);
        _titleScreen.SetActive(false);
        _endScreen = Instantiate(_endScreenPrefab);
        // end screen needs to render above the black fade sphere, which is 4999
        _endScreen.GetComponent<MeshRenderer>().sharedMaterial.renderQueue = 5000;
        _endScreen.SetActive(false);
    }

    public void Start()
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_ANDROID
        OVRManager.eyeFovPremultipliedAlphaModeEnabled = false;
#endif

        if (MultiToy.Instance) MultiToy.Instance.InitializeToys();
        _pet.Initialize();

        _sceneManager.SceneModelLoadedSuccessfully += SceneModelLoaded;
    }

    public void Update()
    {
        CalculateFistStrength();
        if (_handClosed)
        {
            if (_fistValue < 0.2f)
            {
                _handClosed = false;
                OnHandOpenDelegate?.Invoke();
            }
            else
            {
                OnHandDelegate?.Invoke();
            }
        }
        else
        {
            if (_fistValue > 0.3f)
            {
                _handClosed = true;
                OnHandClosedDelegate?.Invoke();
            }
        }

        var usingHands = (
                OVRInput.GetActiveController() == OVRInput.Controller.Hands ||
                OVRInput.GetActiveController() == OVRInput.Controller.LHand ||
                OVRInput.GetActiveController() == OVRInput.Controller.RHand ||
                OVRInput.GetActiveController() == OVRInput.Controller.None);
        if (usingHands != _usingHands)
        {
            _usingHands = usingHands;
            if (usingHands)
            {
                if (_gameController == OVRInput.Controller.LTouch)
                {
                    _gameController = OVRInput.Controller.LHand;
                }
                if (_gameController == OVRInput.Controller.RTouch)
                {
                    _gameController = OVRInput.Controller.RHand;
                }
            }
            else
            {
                if (_gameController == OVRInput.Controller.RHand)
                {
                    _gameController = OVRInput.Controller.RTouch;
                }
                if (_gameController == OVRInput.Controller.LHand)
                {
                    _gameController = OVRInput.Controller.LTouch;
                }
            }

            MultiToy.Instance.UseHands(_usingHands, _gameController == OVRInput.Controller.RTouch);
            MultiToy.Instance.EnableCollision(!_usingHands);

            // update tutorial text when switching input, if onscreen
            WorldBeyondTutorial.Instance.UpdateMessageTextForInput();
        }

        // constantly check if the player is within the polygonal floorplan of the room
        if (_currentChapter >= GameChapter.Title)
        {
            if (!_vrRoom.IsPlayerInRoom())
            {
                WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.ERROR_USER_WALKED_OUTSIDE_OF_ROOM);
            }
            else if (WorldBeyondTutorial.Instance._currentMessage == WorldBeyondTutorial.TutorialMessage.ERROR_USER_WALKED_OUTSIDE_OF_ROOM)
            {
                WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.None);
            }
        }

        // disable a hand if it's not tracked (avoiding ghost hands)
        if (_leftOVR && _rightOVR)
        {
            _leftHandVisual.ForceOffVisibility = !_leftOVR.IsTracked;
            _rightHandVisual.ForceOffVisibility = !_rightOVR.IsTracked;
        }

        switch (_currentChapter)
        {
            case GameChapter.Void:
                if (_sceneModelLoaded) GetRoomFromScene();
                break;
            case GameChapter.Title:
                PositionTitleScreens(false);
                break;
            case GameChapter.Introduction:
                // Passthrough fading is done in the PlayIntroPassthrough coroutine
                break;
            case GameChapter.OppyBaitsYou:
                // if either hand is getting close to the toy, grab it and start the experience
                float handRange = 0.2f;
                float leftRange = Vector3.Distance(OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch), MultiToy.Instance.transform.position);
                float rightRange = Vector3.Distance(OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch), MultiToy.Instance.transform.position);
                bool leftHandApproaching = leftRange <= handRange;
                bool rightHandApproaching = rightRange <= handRange;
                if (MultiToy.Instance._toyVisible && (leftHandApproaching || rightHandApproaching))
                {
                    if (usingHands)
                    {
                        _gameController = leftRange < rightRange ? OVRInput.Controller.LHand : OVRInput.Controller.RHand;
                        MultiToy.Instance.SetToyMesh(MultiToy.ToyOption.None);
                    }
                    else
                    {
                        _gameController = leftRange < rightRange ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
                        MultiToy.Instance.ShowPassthroughGlove(true, _gameController == OVRInput.Controller.RTouch);
                    }
                    _usingHands = usingHands;

                    _lightBeam.CloseBeam();
                    MultiToy.Instance._grabToy_1.Play();
                    ForceChapter(GameChapter.SearchForOppy);
                }
                break;
            case GameChapter.SearchForOppy:
                break;
            case GameChapter.OppyExploresReality:
                break;
            case GameChapter.TheGreatBeyond:
                break;
            case GameChapter.Ending:
                if (_endScreen.activeSelf)
                {
                    PositionTitleScreens(false);
                }
                break;
        }

        // make sure there's never a situation with no balls to grab
        bool noHiddenBall = (_hiddenBallCollectable == null);
        bool flashlightActive = MultiToy.Instance.IsFlashlightActive();
        bool validMode = (_currentChapter > GameChapter.SearchForOppy && _currentChapter < GameChapter.Ending);

        // note: this logic only executes after Oppy enters reality
        // before that, the experience is scripted, so balls shouldn't spawn so randomly
        if (flashlightActive && noHiddenBall && _oppyDiscoveryCount >= 2 && validMode)
        {
            _ballSpawnTimer -= Time.deltaTime;
            if (_ballSpawnTimer <= 0.0f)
            {
                _shouldSpawnBall = true;
                _ballSpawnTimer = Random.Range(_spawnTimeMin, _spawnTimeMax);
            }
        }

        if (_shouldSpawnBall)
        {
            SpawnHiddenBall();
            _shouldSpawnBall = false;
        }


        bool roomSparkleRingVisible = (_currentChapter >= GameChapter.OppyExploresReality && _hiddenBallCollectable);
        roomSparkleRingVisible |= (_currentChapter == GameChapter.SearchForOppy && (_pet.gameObject.activeSelf || (_hiddenBallCollectable && !_hiddenBallCollectable._wasShot)));

        Vector3 ripplePosition = _hiddenBallCollectable ? _hiddenBallPosition : Vector3.one * -1000.0f;
        if (_currentChapter == GameChapter.SearchForOppy)
        {
            ripplePosition = _pet.gameObject.activeSelf ? _pet.transform.position : ripplePosition;
        }
        float effectSpeed = Time.deltaTime * 2.0f;
        _vrRoomEffectTimer += roomSparkleRingVisible ? effectSpeed : -effectSpeed;

        // to make balls easier to find, display a ripple effect on Passthrough
        bool showRippleMask = _hiddenBallCollectable != null && flashlightActive && _hiddenBallCollectable._ballState == BallCollectable.BallStatus.Hidden;
        _vrRoomEffectMaskTimer += showRippleMask ? effectSpeed : -effectSpeed;
        if (_vrRoomEffectTimer >= 0.0f || _vrRoomEffectMaskTimer >= 0.0f)
        {
            VirtualRoom.Instance.SetWallEffectParams(ripplePosition, Mathf.Clamp01(_vrRoomEffectTimer), _vrRoomEffectMaskTimer);
        }
        _vrRoomEffectTimer = Mathf.Clamp01(_vrRoomEffectTimer);
        _vrRoomEffectMaskTimer = Mathf.Clamp01(_vrRoomEffectMaskTimer);
        if (_usingHands)
        {
            HideInvisibleHandAccessories();
        }
    }

    /// <summary>
    /// Calculates whether each hand should be visible or not
    /// </summary>
    private void HideInvisibleHandAccessories()
    {
        bool leftHandHidden = !_leftHand.IsDataValid || _leftHandVisual.ForceOffVisibility;
        bool rightHandHidden = !_rightHand.IsDataValid || _rightHandVisual.ForceOffVisibility;
        var grabbedBall = MultiToy.Instance._grabbedBall;

        // Called before updating distance so that the hidden property is set while the ball is close to the hand
        UpdateHandVisibility(leftHandHidden, _interactionLineLeft, _leftHandGrabbedBallLastDistance, _gameController == OVRInput.Controller.LHand, grabbedBall);
        UpdateHandVisibility(rightHandHidden, _interactionLineRight, _rightHandGrabbedBallLastDistance, _gameController == OVRInput.Controller.RHand, grabbedBall);

        // Hidden hands have a position of 0, only update if the hand is visible.
        if (!leftHandHidden) _leftHandGrabbedBallLastDistance = grabbedBall ? Vector3.Distance(_leftHandAnchor.position, grabbedBall.transform.position) : Mathf.Infinity;
        if (!rightHandHidden) _rightHandGrabbedBallLastDistance = grabbedBall ? Vector3.Distance(_rightHandAnchor.position, grabbedBall.transform.position) : Mathf.Infinity;

        if (!_usingHands)
        {
            _interactionLineLeft.enabled = false;
            _interactionLineRight.enabled = false;
        }
    }

    /// <summary>
    /// Hides the ball, reticule and tutorial if the hand is not tracked anymore.
    /// Using previousHandToBallDistance to determine whether the current hand is holding the ball
    /// </summary>
    private void UpdateHandVisibility(bool handHidden, DistantInteractionLineVisual interactionLine, float previousHandToBallDistance, bool primary, [CanBeNull] BallCollectable grabbedBall)
    {
        bool holdingBall = grabbedBall != null && previousHandToBallDistance < 0.2f;
        if (handHidden)
        {
            interactionLine.gameObject.SetActive(false);
            if (primary) WorldBeyondTutorial.Instance.ForceInvisible();
            if (holdingBall) grabbedBall.ForceInvisible();
        }
        else
        {
            interactionLine.gameObject.SetActive(_usingHands && MultiToy.Instance.GetCurrentToy() == MultiToy.ToyOption.Flashlight);
            if (primary) WorldBeyondTutorial.Instance.ForceVisible();
            if (holdingBall) grabbedBall.ForceVisible();
        }
    }

    /// <summary>
    /// Advance the story line of The World Beyond.
    /// </summary>
    public void ForceChapter(GameChapter forcedChapter)
    {
        StopAllCoroutines();
        KillControllerVibration();
        MultiToy.Instance.SetToy(forcedChapter);
        _currentChapter = forcedChapter;
        WorldBeyondEnvironment.Instance.ShowEnvironment((int)_currentChapter > (int)GameChapter.SearchForOppy);

        if ((int)_currentChapter < (int)GameChapter.SearchForOppy) _mainCamera.backgroundColor = _cameraDark;

        _pet.gameObject.SetActive((int)_currentChapter >= (int)GameChapter.OppyExploresReality);
        _pet.SetOppyChapter(_currentChapter);
        _pet.PlaySparkles(false);

        if (_lightBeam) { _lightBeam.gameObject.SetActive(false); }
        if (_titleScreen) _titleScreen.SetActive(false);
        if (_endScreen) _endScreen.SetActive(false);

        switch (_currentChapter)
        {
            case GameChapter.Title:
                AudioManager.SetSnapshot_Title();
                MusicManager.Instance.PlayMusic(MusicManager.Instance.IntroMusic);
                StartCoroutine(ShowTitleScreen());
                VirtualRoom.Instance.ShowAllWalls(false);
                VirtualRoom.Instance.HideEffectMesh();
                WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.None);
                WorldBeyondEnvironment.Instance._sun.enabled = false;
                break;
            case GameChapter.Introduction:
                AudioManager.SetSnapshot_Introduction();
                VirtualRoom.Instance.ShowDarkRoom(true);
                VirtualRoom.Instance.AnimateEffectMesh();
                StartCoroutine(PlayIntroPassthrough());
                break;
            case GameChapter.OppyBaitsYou:
                _passthroughStylist.ResetPassthrough(0.1f);
                StartCoroutine(PlaceToyRandomly(2.0f));
                break;
            case GameChapter.SearchForOppy:
                VirtualRoom.Instance.HideEffectMesh();
                _oppyDiscovered = false;
                _oppyDiscoveryCount = 0;
                _ballCount = _startingBallCount;
                _passthroughStylist.ResetPassthrough(0.1f);
                WorldBeyondEnvironment.Instance._sun.enabled = true;
                StartCoroutine(CountdownToFlashlight(5.0f));
                StartCoroutine(FlickerCameraToClearColor());
                break;
            case GameChapter.OppyExploresReality:
                AudioManager.SetSnapshot_OppyExploresReality();
                _passthroughStylist.ResetPassthrough(0.1f);
                VirtualRoom.Instance.ShowAllWalls(true);
                VirtualRoom.Instance.SetRoomSaturation(1.0f);
                StartCoroutine(UnlockBallShooter(_usingHands ? 0f : 5.0f));
                StartCoroutine(UnlockWallToy(_usingHands ? 5f : 20.0f));
                _spaceShipAnimator.StartIdleSound(); // Start idle sound here - mix will mute it.
                break;
            case GameChapter.TheGreatBeyond:
                AudioManager.SetSnapshot_TheGreatBeyond();
                _passthroughStylist.ResetPassthrough(0.1f);
                SetEnvironmentSaturation(IsGreyPassthrough() ? 0.0f : 1.0f);
                if (IsGreyPassthrough()) StartCoroutine(SaturateEnvironmentColor());
                MusicManager.Instance.PlayMusic(MusicManager.Instance.PortalOpen);
                MusicManager.Instance.PlayMusic(MusicManager.Instance.TheGreatBeyondMusic);
                break;
            default:
                break;
        }
        Debug.Log("TheWorldBeyond: started chapter " + _currentChapter);
    }

    /// <summary>
    /// After the title screen fades to black, start the transition from black to darkened-Passthrough.
    /// After that, trigger the next chapter that shows the light beam.
    /// </summary>
    IEnumerator PlayIntroPassthrough()
    {
        _backgroundFadeSphere.SetActive(false);
        // first, make everything black
        PassthroughStylist.PassthroughStyle darkPassthroughStyle = new PassthroughStylist.PassthroughStyle(
            new Color(0, 0, 0, 0),
            1.0f,
            0.0f,
            0.0f,
            0.0f,
            true,
            Color.black,
            Color.black,
            Color.black);
        _passthroughStylist.ForcePassthroughStyle(darkPassthroughStyle);

        // fade in edges
        float timer = 0.0f;
        float lerpTime = 4.0f;
        while (timer <= lerpTime)
        {
            timer += Time.deltaTime;

            Color edgeColor = Color.white;
            edgeColor.a = Mathf.Clamp01(timer / 3.0f); // fade from transparent
            _passthroughLayer.edgeColor = edgeColor;

            float normTime = Mathf.Clamp01(timer / lerpTime);
            _fadeSphere.sharedMaterial.SetColor("_Color", Color.Lerp(Color.black, Color.clear, normTime));

            VirtualRoom.Instance.SetEdgeEffectIntensity(normTime);

            // once lerpTime is over, fade in normal passthrough
            if (timer >= lerpTime)
            {
                PassthroughStylist.PassthroughStyle normalPassthrough = new PassthroughStylist.PassthroughStyle(
                    new Color(0, 0, 0, 0),
                    1.0f,
                    0.0f,
                    0.0f,
                    0.0f,
                    false,
                    Color.white,
                    Color.black,
                    Color.white);
                _passthroughStylist.ShowStylizedPassthrough(normalPassthrough, 5.0f);
                _fadeSphere.gameObject.SetActive(false);
            }
            yield return null;
        }

        yield return new WaitForSeconds(3.0f);
        ForceChapter(GameChapter.OppyBaitsYou);
    }

    /// <summary>
    /// When you first grab the MultiToy, the world flashes for a split second.
    /// </summary>
    IEnumerator FlickerCameraToClearColor()
    {
        float timer = 0.0f;
        float flickerTimer = 0.5f;
        while (timer <= flickerTimer)
        {
            timer += Time.deltaTime;
            float normTimer = Mathf.Clamp01(0.5f * timer / flickerTimer);
            _mainCamera.backgroundColor = Color.Lerp(Color.black, _cameraDark, MultiToy.Instance.EvaluateFlickerCurve(normTimer));
            if (timer >= flickerTimer)
            {
                VirtualRoom.Instance.ShowAllWalls(true);
                VirtualRoom.Instance.ShowDarkRoom(false);
                VirtualRoom.Instance.SetRoomSaturation(IsGreyPassthrough() ? 0 : 1);
                WorldBeyondEnvironment.Instance.ShowEnvironment(true);
            }
            yield return null;
        }
    }

    /// <summary>
    /// Handle black fading, Passthrough blending, and the intro title screen animation.
    /// </summary>
    IEnumerator ShowTitleScreen()
    {
        _fadeSphere.gameObject.SetActive(true);
        _backgroundFadeSphere.gameObject.SetActive(true);

        PassthroughStylist.PassthroughStyle darkPassthroughStyle = new PassthroughStylist.PassthroughStyle(
               new Color(0, 0, 0, 0),
               1.0f,
               0.0f,
               0.0f,
               0.0f,
               true,
               Color.black,
               Color.black,
               Color.black);
        _passthroughStylist.ForcePassthroughStyle(darkPassthroughStyle);

        _fadeSphere.sharedMaterial.SetColor("_Color", Color.black);
        _fadeSphere.sharedMaterial.renderQueue = 4999;

        _backgroundFadeSphere.GetComponent<MeshRenderer>().material.renderQueue = 1997;
        _backgroundFadeSphere.GetComponent<MeshRenderer>().material.SetColor("_Color", Color.black);

        _titleScreen.SetActive(true);
        PositionTitleScreens(true);

        // fade/animate title text
        float timer = 0.0f;
        float lerpTime = 8.0f;
        while (timer <= lerpTime)
        {
            timer += Time.deltaTime;

            float normTimer = Mathf.Clamp01(timer / lerpTime);

            // fade black above everything
            float blackFade = Mathf.Clamp01(normTimer * 5) * Mathf.Clamp01((1 - normTimer) * 5);
            _fadeSphere.sharedMaterial.SetColor("_Color", Color.Lerp(Color.black, Color.clear, blackFade));

            // once lerpTime is over, fade in normal passthrough
            if (timer >= lerpTime)
            {
                _titleScreen.SetActive(false);
            }
            yield return null;
        }
        ForceChapter(GameChapter.Introduction);
    }

    /// <summary>
    /// When Oppy first enters reality, prepare to unlock the ball shooter.
    /// </summary>
    IEnumerator UnlockBallShooter(float countdown)
    {
        yield return new WaitForSeconds(countdown);
        // ensures the flashlight works again, once it's switched back to
        MultiToy.Instance.SetFlickerTime(0.0f);

        MultiToy.Instance.UnlockBallShooter();
        OVRInput.SetControllerVibration(1, 1, _gameController);
        yield return new WaitForSeconds(1.0f);
        KillControllerVibration();
    }

    /// <summary>
    /// After a few seconds of playing with Oppy, unlock the wall toggler toy.
    /// </summary>
    IEnumerator UnlockWallToy(float countdown)
    {
        yield return new WaitForSeconds(countdown);
        MultiToy.Instance.UnlockWallToy();
        OVRInput.SetControllerVibration(1, 1, _gameController);
        yield return new WaitForSeconds(1.0f);
        KillControllerVibration();
    }

    /// <summary>
    /// Prepare the toy and light beam for their initial appearance.
    /// </summary>
    IEnumerator PlaceToyRandomly(float spawnTime)
    {
        yield return new WaitForSeconds(spawnTime);
        MultiToy.Instance.ShowToy(true);
        MultiToy.Instance.SetToyMesh(MultiToy.ToyOption.Flashlight);
        _toyBasePosition = GetRandomToyPosition();
        _lightBeam.gameObject.SetActive(true);
        _lightBeam.transform.localScale = new Vector3(1, VirtualRoom.Instance.GetCeilingHeight(), 1);
        _lightBeam.Prepare(_toyBasePosition);
    }

    /// <summary>
    /// Right after player grabs Multitoy, wait a few seconds before turning on the flashlight.
    /// </summary>
    IEnumerator CountdownToFlashlight(float spawnTime)
    {
        yield return new WaitForSeconds(spawnTime - 0.5f);
        OVRInput.SetControllerVibration(1, 1, _gameController);
        MultiToy.Instance.EnableFlashlightCone(true);
        if (_usingHands)
        {
            WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.EnableFlashlight);
        }
        MultiToy.Instance._flashlightFlicker_1.Play();
        float timer = 0.0f;
        float lerpTime = 0.5f;
        while (timer <= lerpTime)
        {
            timer += Time.deltaTime;
            MultiToy.Instance.SetFlickerTime((0.5f * timer / lerpTime) + 0.5f);
            if (timer >= lerpTime)
            {
                MultiToy.Instance.SetFlickerTime(1.0f);
            }
            yield return null;
        }
        KillControllerVibration();
        StartCoroutine(SpawnPetRandomly(true, Random.Range(_spawnTimeMin, _spawnTimeMax)));
    }

    /// <summary>
    /// Called from OVRSceneManager.SceneModelLoadedSuccessfully().
    /// This only sets a flag, and the game behavior begins in Update().
    /// This is because OVRSceneManager does all the heavy lifting, and this experience requires it to be complete.
    /// </summary>
    void SceneModelLoaded()
    {
        _sceneModelLoaded = true;
    }

    /// <summary>
    /// When the Scene has loaded, instantiate all the wall and furniture items.
    /// OVRSceneManager creates proxy anchors, that we use as parent tranforms for these instantiated items.
    /// </summary>
    void GetRoomFromScene()
    {
        if (_frameWait < 1)
        {
            _frameWait++;
            return;
        }

        try
        {
            // OVRSceneAnchors have already been instantiated from OVRSceneManager
            // to avoid script execution conflicts, we do this once in the Update loop instead of directly when the SceneModelLoaded event is fired
            _sceneAnchors = FindObjectsOfType<OVRSceneAnchor>();

            // WARNING: right now, a Scene is guaranteed to have closed walls
            // if this ever changes, this logic needs to be revisited because the whole game fails (e.g. furniture with no walls)
            _vrRoom.Initialize(_sceneAnchors);

            // even though loading has succeeded to this point, do some sanity checks
            if (!_vrRoom.IsPlayerInRoom())
            {
                WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.ERROR_USER_STARTED_OUTSIDE_OF_ROOM);
            }
            WorldBeyondEnvironment.Instance.Initialize();
            ForceChapter(GameChapter.Title);
        }
        catch
        {
            // if initialization messes up for some reason, quit the app
            WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.ERROR_NO_SCENE_DATA);
        }
    }

    /// <summary>
    /// When the flashlight shines on an energy ball, advance the story and handle the UI message.
    /// </summary>
    public void DiscoveredBall(BallCollectable collected)
    {
        if (!_usingHands) // Balls are not absorbed, just picked up when using hands
        {
            _ballCount++;
        }
        WorldBeyondTutorial.Instance.HideMessage(WorldBeyondTutorial.TutorialMessage.BallSearch);
        WorldBeyondTutorial.Instance.HideMessage(WorldBeyondTutorial.TutorialMessage.NoBalls);

        // when using hands, make sure the discovered ball was actually hidden
        // otherwise, grabbing any ball will advance the script in undesirable ways
        if (_usingHands && collected._wasShot)
        {
            return;
        }
        if (_currentChapter == GameChapter.SearchForOppy)
        {
            // in case we already picked up a ball and triggered the coroutine, cancel the old one
            // this is only a problem when using hands, since the balls stay around
            if (_usingHands)
            {
                StopAllCoroutines();
            }
            StartCoroutine(SpawnPetRandomly(false, Random.Range(_spawnTimeMin, _spawnTimeMax)));
        }
    }

    /// <summary>
    /// Get the closest ball to Oppy that's available to be eaten. (some are intentionally unavailable, like hidden ones)
    /// </summary>
    public BallCollectable GetClosestEdibleBall(Vector3 petPosition)
    {
        float closestDist = 20.0f;
        BallCollectable closestBall = null;
        foreach (Transform bcXform in _ballContainer)
        {
            BallCollectable bc = bcXform.GetComponent<BallCollectable>();
            if (!bc)
            {
                continue;
            }
            float thisDist = Vector3.Distance(petPosition, bc.gameObject.transform.position);
            if (thisDist < closestDist
                && bc._ballState == BallCollectable.BallStatus.Released
                && bc._shotTimer >= 1.0f)
            {
                closestDist = thisDist;
                closestBall = bc;
            }
        }
        return closestBall;
    }

    /// <summary>
    /// Simple cone to find the best candidate ball within view of the flashlight
    /// </summary>
    public BallCollectable GetTargetedBall(Vector3 toyPos, Vector3 toyFwd)
    {
        float closestAngle = 0.9f;

        BallCollectable closestBall = null;
        for (int i = 0; i < _ballContainer.childCount; i++)
        {
            BallCollectable bc = _ballContainer.GetChild(i).GetComponent<BallCollectable>();
            if (!bc)
            {
                continue;
            }
            Vector3 rayFromHand = (bc.gameObject.transform.position - toyPos).normalized;
            float thisViewAngle = Vector3.Dot(rayFromHand, toyFwd);
            if (thisViewAngle > closestAngle)
            {
                if (bc._ballState == BallCollectable.BallStatus.Available ||
                    bc._ballState == BallCollectable.BallStatus.Hidden ||
                    bc._ballState == BallCollectable.BallStatus.Released)
                {
                    closestAngle = thisViewAngle;
                    closestBall = bc;
                }
            }
        }
        return closestBall;
    }

    /// <summary>
    /// When discovering Oppy for the last time, the flashlight dies temporarily as she "pops" into reality.
    /// </summary>
    IEnumerator MalfunctionFlashlight()
    {
        yield return new WaitForSeconds(2.0f);

        GameObject effectRing = Instantiate(_worldShockwave);
        effectRing.transform.position = _pet.transform.position;
        effectRing.GetComponent<ParticleSystem>().Play();

        _pet.EnablePassthroughShell(true);

        PassthroughStylist.PassthroughStyle weirdPassthrough = new PassthroughStylist.PassthroughStyle(
                    new Color(0, 0, 0, 0),
                    1.0f,
                    0.0f,
                    0.0f,
                    0.8f,
                    true,
                    new Color(0, 0.5f, 1, 0.5f),
                    Color.black,
                    Color.white);
        _passthroughStylist.ShowStylizedPassthrough(weirdPassthrough, 0.2f);

        WorldBeyondEnvironment.Instance.FlickerSun();

        // flicker out
        MultiToy.Instance._flashlightFlicker_1.Play();
        float timer = 0.0f;
        float lerpTime = 0.3f;
        while (timer <= lerpTime)
        {
            timer += Time.deltaTime;
            MultiToy.Instance.SetFlickerTime(0.5f * timer / lerpTime);
            if (timer >= lerpTime)
            {
                MultiToy.Instance.SetFlickerTime(0.5f);
                MultiToy.Instance.StopLoopingSound();
                MultiToy.Instance._malfunction_1.Play();
                _passthroughStylist.ResetPassthrough(0.15f);
            }
            yield return null;
        }

        ForceChapter(GameChapter.OppyExploresReality);
        _pet.EndLookTarget();
    }

    /// <summary>
    /// After shining the flashlight on Oppy the first two times, it flickers so she can disappear.
    /// </summary>
    IEnumerator FlickerFlashlight(float delayTime = 0.0f)
    {
        yield return new WaitForSeconds(delayTime);
        // flicker out
        MultiToy.Instance._flashlightFlicker_1.Play();
        MultiToy.Instance._flashlightLoop_1.Pause();
        float timer = 0.0f;
        float lerpTime = 0.2f;
        while (timer <= lerpTime)
        {
            timer += Time.deltaTime;
            MultiToy.Instance.SetFlickerTime(0.5f * timer / lerpTime);
            if (timer >= lerpTime)
            {
                MultiToy.Instance.SetFlickerTime(0.5f);
            }
            yield return null;
        }

        // play Oppy teleport particles, only on the middle discovery
        if (_oppyDiscoveryCount == 1)
        {
            _pet.PlayTeleport();
        }

        // hide Oppy
        _oppyDiscovered = false;
        _oppyDiscoveryCount++;
        _pet.ResetAnimFlags();
        float colorSaturation = IsGreyPassthrough() ? Mathf.Clamp01(_oppyDiscoveryCount / 3.0f) : 1.0f;
        _pet.SetMaterialSaturation(colorSaturation);
        _pet.StartLookTarget();
        _pet.gameObject.SetActive(false);

        // increase room saturation while flashlight is off
        VirtualRoom.Instance.SetRoomSaturation(colorSaturation);

        // flicker in
        timer = 0.0f;
        while (timer <= lerpTime)
        {
            timer += Time.deltaTime;
            MultiToy.Instance.SetFlickerTime((0.5f * timer / lerpTime) + 0.5f);
            if (timer >= lerpTime)
            {
                MultiToy.Instance.SetFlickerTime(1.0f);
            }
            yield return null;
        }
        MultiToy.Instance._flashlightLoop_1.Resume();

        if (_oppyDiscoveryCount == 1)
        {
            WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.BallSearch);
            _hiddenBallCollectable.SetState(BallCollectable.BallStatus.Hidden);
        }
        else
        {
            // spawn ball only after the first discovery (first ball already exists)
            yield return new WaitForSeconds(Random.Range(_spawnTimeMin, _spawnTimeMax));
            SpawnHiddenBall();
        }
    }

    /// <summary>
    /// During the discovery chapter, Oppy is place in hidden locations in the space.
    /// </summary>
    IEnumerator SpawnPetRandomly(bool firstTimeSpawning, float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        // because the pet has a Navigation component, we must turn off the object before manually moving
        // this is to avoid the pet getting "stuck" on the walls
        _pet.gameObject.SetActive(false);
        Vector3 spawnPos = GetRandomPetPosition();
        Vector3 fwd = _mainCamera.transform.position - spawnPos;
        Quaternion oppyRotation = Quaternion.LookRotation(new Vector3(fwd.x, 0, fwd.z));
        _pet.transform.rotation = oppyRotation;
        _pet.transform.position = spawnPos - _pet.transform.right * 0.1f;
        _pet.gameObject.SetActive(true);
        _pet.PlaySparkles(true);
        _pet.SetLookDirection(fwd.normalized);
        if (firstTimeSpawning)
        {
            _pet.PrepareInitialDiscoveryAnim();
            _pet.SetMaterialSaturation(IsGreyPassthrough() ? 0.0f : 1.0f);
            GameObject hiddenBall = Instantiate(_ballPrefab);
            _hiddenBallCollectable = hiddenBall.GetComponent<BallCollectable>();
            _hiddenBallPosition = spawnPos + _pet.transform.right * 0.05f + Vector3.up * 0.06f;
            _hiddenBallCollectable.PlaceHiddenBall(_hiddenBallPosition, 0);
            _hiddenBallCollectable.SetState(BallCollectable.BallStatus.Unavailable);
        }
    }

    /// <summary>
    /// When the player has the flashlight active, there should always be a hidden ball to discover.
    /// </summary>
    void SpawnHiddenBall()
    {
        // if spawning on a wall, track the id:
        // if the wall is toggled off, we need to destroy the ball
        int wallID = -1;
        GameObject hiddenBall = Instantiate(_ballPrefab);
        _hiddenBallCollectable = hiddenBall.GetComponent<BallCollectable>();
        _hiddenBallPosition = GetRandomBallPosition(ref wallID);
        _hiddenBallCollectable.PlaceHiddenBall(_hiddenBallPosition, wallID);
    }

    /// <summary>
    /// Any time the player "opens" a wall with the wall toggler, some special behavior needs to happen:
    /// 1. Any "hidden" ball on that wall needs to be destroyed, otherwise there'd be a weird float passthrough ball.
    /// 2. If it's the first time, advance the story.
    /// </summary>
    public void OpenedWall(int wallID)
    {
        foreach (Transform child in _ballContainer)
        {
            if (child.GetComponent<BallCollectable>())
            {
                if (child.GetComponent<BallCollectable>()._wallID == wallID)
                {
                    RemoveBallFromWorld(child.GetComponent<BallCollectable>());
                }
            }
        }

        if (_currentChapter == WorldBeyondManager.GameChapter.OppyExploresReality)
        {
            WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.None);
            ForceChapter(GameChapter.TheGreatBeyond);
        }
    }

    /// <summary>
    /// Self-explanatory.
    /// </summary>
    void KillControllerVibration()
    {
        OVRInput.SetControllerVibration(1, 0, _gameController);
    }

    /// <summary>
    /// Find a position in the room to place Oppy.
    /// </summary>
    public Vector3 GetRandomPetPosition()
    {
        Vector3 floorPos = new Vector3(_mainCamera.transform.position.x, GetFloorHeight(), _mainCamera.transform.position.z);
        Vector3 randomPos = _mainCamera.transform.position - _mainCamera.transform.forward;

        // shoot rays out from player, a few cm above ground
        Vector3 curbHeight = floorPos + Vector3.up * 0.2f;
        // startingVec isn't based on -camera.forward because we "sweep" 180 degrees in the loop below
        Vector3 startingVec = new Vector3(-_mainCamera.transform.right.x, curbHeight.y, -_mainCamera.transform.right.z).normalized;

        // return the farthest position, behind player
        // however, for each individual raycast, use the closest hit
        // this avoids a bug where Oppy can spawn outside (from a ray aiming through a wall to another wall or wall edge)
        float farthestOverallHit = 0.0f;
        const float castDistance = 100.0f;
        int sliceCount = 4;
        for (int i = 0; i <= sliceCount; i++)
        {
            RaycastHit hitInfo;
            LayerMask oppySpawnLayer = LayerMask.GetMask("RoomBox", "Furniture");
            float closestRaycastHit = castDistance;
            Vector3 candidatePosition = randomPos;
            RaycastHit[] roomboxHit = Physics.RaycastAll(curbHeight, startingVec, castDistance, oppySpawnLayer);
            foreach (RaycastHit hit in roomboxHit)
            {
                float thisHit = Vector3.Distance(curbHeight, hit.point);
                if (thisHit < closestRaycastHit)
                {
                    closestRaycastHit = thisHit;
                    candidatePosition = hit.point - startingVec * 0.5f; // back off from the impact point to give Oppy space
                }
            }

            float distanceToHit = Vector3.Distance(curbHeight, candidatePosition);
            if (distanceToHit > farthestOverallHit)
            {
                farthestOverallHit = distanceToHit;
                randomPos = candidatePosition;
            }

            startingVec = Quaternion.Euler(0, -180.0f / sliceCount, 0) * startingVec;
        }

        randomPos = new Vector3(randomPos.x, GetFloorHeight(), randomPos.z);
        return randomPos;
    }

    /// <summary>
    /// Find a room surface upon which to spawn a hidden ball.
    /// </summary>
    public Vector3 GetRandomBallPosition(ref int wallID)
    {
        // default case; spawn it randomly on the floor, which has to exist
        List<Vector3> randomPositions = new List<Vector3>();
        List<int> matchingWallID = new List<int>();

        const int numSamples = 8;
        for (int i = 0; i < numSamples; i++)
        {
            // try a random position in front of player
            float localX = Random.Range(-1.0f, 1.0f);
            float localY = Random.Range(-1.0f, 1.0f);
            float localZ = Random.Range(0.0f, 1.0f);
            Vector3 randomVector = _mainCamera.transform.rotation * (new Vector3(localX, localY, localZ).normalized);
            LayerMask ballSpawnLayer = LayerMask.GetMask("RoomBox", "Furniture");
            RaycastHit[] roomboxHit = Physics.RaycastAll(MultiToy.Instance.transform.position, randomVector, 100, ballSpawnLayer);
            float closestSurface = 100.0f;
            bool foundSurface = false;
            Vector3 bestPos = Vector3.zero;
            int bestID = -1;
            foreach (RaycastHit hit in roomboxHit)
            {
                GameObject hitObj = hit.collider.gameObject;
                if (hitObj.GetComponent<WorldBeyondRoomObject>() && !hitObj.GetComponent<WorldBeyondRoomObject>()._passthroughWallActive)
                {
                    // don't spawn hidden balls on open walls
                    continue;
                }
                // need to find the closest impact, because hit order isn't guaranteed
                float thisHit = Vector3.Distance(MultiToy.Instance.transform.position, hit.point);
                if (thisHit < closestSurface)
                {
                    foundSurface = true;
                    closestSurface = thisHit;
                    int surfId = -1;
                    if (hitObj.GetComponent<WorldBeyondRoomObject>())
                    {
                        surfId = hitObj.GetComponent<WorldBeyondRoomObject>()._surfaceID;
                    }
                    bestID = surfId;
                    bestPos = hit.point + hit.normal * 0.06f;
                }
            }

            if (foundSurface)
            {
                randomPositions.Add(bestPos);
                matchingWallID.Add(bestID);
            }
            Debug.Log("TWB found ball position: " + foundSurface);
        }

        // default position, on the floor
        Vector3 randomPos = VirtualRoom.Instance.GetSimpleFloorPosition() + Vector3.up * 0.05f;
        if (randomPositions.Count > 0)
        {
            int randomSelection = Random.Range(0, randomPositions.Count);
            randomPos = randomPositions[randomSelection];
            wallID = matchingWallID[randomSelection];
        }

        return randomPos;
    }

    /// <summary>
    /// Find a clear space on the floor to place the light beam/Multitoy.
    /// </summary>
    public Vector3 GetRandomToyPosition()
    {
        // define guardian bounds or set bounds to 0 if guardian is disabled
        var bounds = new Bounds(Vector3.zero, OVRManager.boundary?.GetConfigured() ?? false ? OVRManager.boundary.GetDimensions(OVRBoundary.BoundaryType.PlayArea) : Vector3.zero);
        // default position is where camera is. Shouldn't happen, but it's a fallback
        Vector3 finalPos = new Vector3(_mainCamera.transform.position.x, GetFloorHeight(), _mainCamera.transform.position.z);

        // shoot rays out from player, a few cm above ground
        Vector3 curbHeight = finalPos + Vector3.up * 0.1f;
        Vector3 direction = _mainCamera.transform.right;

        // select the farthest candidate position
        float farthestPosition = 0.0f;
        int sliceCount = 4;
        for (int i = 0; i <= sliceCount; i++)
        {
            LayerMask ballSpawnLayer = LayerMask.GetMask("RoomBox", "Furniture");
            RaycastHit hit;
            Ray ray = new Ray(curbHeight, direction);
            Vector3 candidatePos = finalPos;

            if (Physics.Raycast(ray, out hit, 100, ballSpawnLayer))
            {
                // get a halfway point, so beam isn't always flush against a wall
                var posMiddle = (curbHeight + hit.point) * 0.5f;
                // prevent from spawning outside the walls and guardian
                if (_vrRoom.IsPositionInRoom(posMiddle, 0f) && (bounds.size.Equals(Vector3.zero) || bounds.Contains(posMiddle)))
                {
                    candidatePos = posMiddle;
                }
            }

            float distanceToCandidate = Vector3.Distance(curbHeight, candidatePos);
            if (distanceToCandidate > farthestPosition)
            {
                farthestPosition = distanceToCandidate;
                finalPos = candidatePos;
            }

            direction = Quaternion.Euler(0, -180.0f / sliceCount, 0) * direction;
        }

        return MovePointAwayFromWalls(new Vector3(finalPos.x, 1.0f + GetFloorHeight(), finalPos.z), bounds);
    }

    /// <summary>
    /// Raycasts every 45 degrees from point towards walls, move the point away from the wall if it's too close
    /// </summary>
    private Vector3 MovePointAwayFromWalls(Vector3 pos, Bounds bounds)
    {
        var point = pos;
        var direction = Vector3.right;
        RaycastHit hit;
        LayerMask ballSpawnLayer = LayerMask.GetMask("RoomBox", "Furniture");
        var attempt = 0;
        for (int i = 0; i < 8; i++)
        {
            if (Physics.Raycast(point, direction, out hit, 0.5f, ballSpawnLayer))
            {
                var safePos = point - direction * 0.5f;
                if (_vrRoom.IsPositionInRoom(safePos, 0f) && (bounds.size.Equals(Vector3.zero) || bounds.Contains(safePos)))
                {
                    point = safePos;
                }
                // reset count so new position gets checked too
                i = 0;
                attempt++;
            }
            direction = Quaternion.Euler(0, 45, 0) * direction;

            // prevent infinite loop if multitoy can't be placed safely
            if (attempt > 25)
            {
                Debug.LogError("Failed to safely move point away from walls after " + attempt + " attempts");
                return pos;
            }
        }
        return point;
    }

    /// <summary>
    /// Start the coroutine that plays the UFO exit sequence.
    /// </summary>
    public void FlyAwayUFO()
    {
        StartCoroutine(DoEndingSequence());
    }

    /// <summary>
    /// End game sequence and cleanup: fade to black, trigger the UFO animation, reset the game.
    /// </summary>
    IEnumerator DoEndingSequence()
    {
        AudioManager.SetSnapshot_Ending();
        _fadeSphere.gameObject.SetActive(true);
        _fadeSphere.sharedMaterial.SetColor("_Color", Color.clear);
        if (_spaceShipAnimator)
        {
            _spaceShipAnimator.TriggerAnim();
            float flyingAwayTime = 15.5f;
            float timer = 0.0f;
            while (timer < flyingAwayTime)
            {
                timer += Time.deltaTime;
                float fadeValue = timer / flyingAwayTime;
                fadeValue = Mathf.Clamp01((fadeValue - 0.9f) * 10.0f);
                _fadeSphere.sharedMaterial.SetColor("_Color", Color.Lerp(Color.clear, Color.white, fadeValue));
                WorldBeyondEnvironment.Instance.FadeOutdoorAudio(1 - fadeValue);
                if (timer >= flyingAwayTime)
                {
                    WorldBeyondEnvironment.Instance.SetOutdoorAudioParams(Vector3.zero, false);
                    _endScreen.SetActive(true);
                    PositionTitleScreens(true);
                    _fadeSphere.sharedMaterial.SetColor("_Color", Color.white);
                    MultiToy.Instance.EndGame();
                    DestroyAllBalls();
                    _spaceShipAnimator.ResetAnim();
                }
                yield return null;
            }

            AudioManager.SetSnapshot_Reset();
            yield return new WaitForSeconds(13.0f);
            ForceChapter(GameChapter.Title);
        }
    }

    /// <summary>
    /// Choose a random animation for Oppy to play when the flashlight shines on her.
    /// </summary>
    public void PlayOppyDiscoveryAnim()
    {
        if (!_oppyDiscovered)
        {
            _oppyDiscovered = true;
            // the final discovery, after which Oppy enters reality
            if (_oppyDiscoveryCount == 2)
            {
                _pet._boneSim.gameObject.SetActive(true);
                _pet.PlayRandomOppyDiscoveryAnim();
                StartCoroutine(MalfunctionFlashlight());
            }
            // first discovery, play the unique discovery anim
            else if (_oppyDiscoveryCount == 0)
            {
                _pet.PlayInitialDiscoveryAnim();
                StartCoroutine(FlickerFlashlight(4.0f));
            }
            // second discovery, play a random "delight" anim
            else
            {
                _pet.PlayRandomOppyDiscoveryAnim();
                StartCoroutine(FlickerFlashlight(2.0f));
            }
        }
    }

    /// <summary>
    /// Dolly/rotate the title and end screens
    /// </summary>
    void PositionTitleScreens(bool firstFrame)
    {
        _titleFadeTimer += Time.deltaTime;
        if (firstFrame)
        {
            _titleFadeTimer = 0.0f;
        }

        Vector3 camFwd = new Vector3(_mainCamera.transform.forward.x, 0, _mainCamera.transform.forward.z).normalized;
        Vector3 currentLook = (_titleScreen.transform.position - _mainCamera.transform.position).normalized;
        const float posLerp = 0.95f;
        Vector3 targetLook = firstFrame ? camFwd : Vector3.Slerp(camFwd, currentLook, posLerp);
        Vector3 pitch = Vector3.Lerp(Vector3.down * 0.05f, Vector3.up * 0.05f, Mathf.Clamp01(_titleFadeTimer / 8.0f));
        Quaternion targetRotation = Quaternion.LookRotation(-targetLook + pitch, Vector3.up);

        float dollyDirection = _currentChapter == GameChapter.Title ? -1.0f : 1.0f;
        float textDistance = (_currentChapter == GameChapter.Title ? 5 : 4) + (dollyDirection * _titleFadeTimer * 0.1f);

        _titleScreen.transform.position = _mainCamera.transform.position + targetLook * textDistance;
        _titleScreen.transform.rotation = targetRotation;

        _endScreen.transform.position = _titleScreen.transform.position;
        _endScreen.transform.rotation = _titleScreen.transform.rotation;

        // hardcoded according to the fade out time of 13 seconds
        // fade in for 2 seconds, fade out after 8 seconds
        float endFade = Mathf.Clamp01(_titleFadeTimer * 0.5f) * (1.0f - Mathf.Clamp01((_titleFadeTimer - 8) * 0.25f));
        _endScreen.GetComponent<MeshRenderer>().sharedMaterial.SetColor("_Color", Color.Lerp(Color.black, Color.white, endFade));
    }

    /// <summary>
    /// Adjust the desaturation range of the environment shaders.
    /// </summary>
    void SetEnvironmentSaturation(float normSat)
    {
        // convert a normalized value to what the shader intakes
        float actualSat = Mathf.Lerp(1.0f, 0.08f, normSat);
        foreach (Material mtl in _environmentMaterials)
        {
            mtl.SetFloat("_SaturationDistance", actualSat);
        }
    }

    /// <summary>
    /// When a passthrough wall is first opened, the virtual environment appears greyscale to match Passthrough.
    /// Over a few seconds, the desaturation range shrinks.
    /// </summary>
    IEnumerator SaturateEnvironmentColor()
    {
        yield return new WaitForSeconds(4.0f);
        float timer = 0.0f;
        float lerpTime = 4.0f;
        while (timer <= lerpTime)
        {
            timer += Time.deltaTime;
            float normTime = IsGreyPassthrough() ? Mathf.Clamp01(timer / lerpTime) : 1.0f;
            SetEnvironmentSaturation(normTime);
            yield return null;
        }
    }

    /// <summary>
    /// Track the balls in the game so they can be safely managed.
    /// </summary>
    public void AddBallToWorld(BallCollectable newBall)
    {
        newBall.gameObject.transform.parent = _ballContainer;
    }

    /// <summary>
    /// When debris is created from a ball collision, track and delete the old pieces so we don't overflow.
    /// </summary>
    public void AddBallDebrisToWorld(GameObject newDebris)
    {
        _ballDebrisObjects.Add(newDebris.GetComponent<BallDebris>());
    }

    /// <summary>
    /// Perform physics on the debris gems, from a position.
    /// </summary>
    public void AffectDebris(Vector3 effectPosition, bool repel)
    {
        for (int i = 0; i < _ballDebrisObjects.Count; i++)
        {
            if (_ballDebrisObjects[i] != null)
            {
                Vector3 forceDirection = _ballDebrisObjects[i].transform.position - effectPosition;
                if (repel)
                {
                    if (forceDirection.magnitude < 0.5f)
                    {
                        float strength = 1.0f - Mathf.Clamp01(forceDirection.magnitude * 2);
                        _ballDebrisObjects[i].AddForce(forceDirection.normalized, strength * 2);
                    }
                }
                else // absorb
                {
                    float range = Vector3.Dot(MultiToy.Instance.GetFlashlightDirection(), -forceDirection.normalized);
                    if (range < -0.8f)
                    {
                        float strength = (-range - 0.8f) / 0.2f;
                        _ballDebrisObjects[i].AddForce(-forceDirection.normalized, strength);
                    }
                }
            }
        }
    }

    /// <summary>
    /// When new debris has been created from a ball collision, delete old debris to manage performance.
    /// </summary>
    public void DeleteOldDebris()
    {
        _ballDebrisObjects.RemoveAll(item => item == null);
        // there's too much debris in the world, start removing some FIFO
        if (_ballDebrisObjects.Count > _maxBallDebris)
        {
            int ballsToDestroy = _ballDebrisObjects.Count - _maxBallDebris;
            for (int i = 0; i < ballsToDestroy; i++)
            {
                // this shrinks the item before self-destructing
                _ballDebrisObjects[i].Kill();
            }
        }
    }

    /// <summary>
    /// A central place to manage ball deletion, such as during Multitoy absorption or death
    /// </summary>
    public void RemoveBallFromWorld(BallCollectable newBall)
    {
        Destroy(newBall.gameObject);
    }

    /// <summary>
    /// Destroy all balls and their debris, when the game ends.
    /// </summary>
    void DestroyAllBalls()
    {
        foreach (Transform child in _ballContainer)
        {
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }
        if (_hiddenBallCollectable)
        {
            Destroy(_hiddenBallCollectable.gameObject);
        }
        // destroy debris also
        foreach (BallDebris child in _ballDebrisObjects)
        {
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }
    }

    /// <summary>
    /// Return a pose for the Multitoy, depending on controller type.
    /// </summary>
    public void GetDominantHand(ref Vector3 handPos, ref Quaternion handRot)
    {
        if (_usingHands)
        {
            bool L_hand = _gameController == OVRInput.Controller.LHand;
            OVRSkeleton refHand = (_gameController == OVRInput.Controller.LHand) ? _leftHand : _rightHand;
            Oculus.Interaction.HandVisual refHandVisual = (_gameController == OVRInput.Controller.LHand) ? _leftHandVisual : _rightHandVisual;
            if (refHandVisual.ForceOffVisibility)
            {
                return;
            }
            // if tuning these values, make your life easier by enabling the DebugAxis objects on the Multitoy prefab
            handPos = L_hand ? _leftPointerOffset.transform.position : _rightPointerOffset.transform.position;
            Vector3 handFwd = L_hand ? _leftPointerOffset.transform.rotation * _leftPointerOffset.Rotation * Vector3.up : _rightPointerOffset.transform.rotation * _rightPointerOffset.Rotation * Vector3.up;
            Vector3 handRt = (refHand.Bones[12].Transform.position - refHand.Bones[6].Transform.position) * (L_hand ? -1.0f : 1.0f);
            Vector3.OrthoNormalize(ref handFwd, ref handRt);
            Vector3 handUp = Vector3.Cross(handFwd, handRt);
            handRot = Quaternion.LookRotation(-handFwd, -handUp);
        }
        else
        {
            handPos = OVRInput.GetLocalControllerPosition(_gameController);
            handRot = OVRInput.GetLocalControllerRotation(_gameController);
        }
    }

    /// <summary>
    /// Simple 0-1 value to decide if the player has made a fist: if all fingers have "curled" enough.
    /// </summary>
    public void CalculateFistStrength()
    {
        OVRSkeleton refHand = (_gameController == OVRInput.Controller.LHand) ? _leftHand : _rightHand;
        Oculus.Interaction.HandVisual refHandVisual = (_gameController == OVRInput.Controller.LHand) ? _leftHandVisual : _rightHandVisual;
        if (!_usingHands || refHandVisual.ForceOffVisibility)
        {
            _fistValue = 1; // Hand is not visible, make it a fist to hide the flashlight and keep holding the held ball
            return;
        }
        Vector3 bone1 = (refHand.Bones[20].Transform.position - refHand.Bones[8].Transform.position).normalized;
        Vector3 bone2 = (refHand.Bones[21].Transform.position - refHand.Bones[11].Transform.position).normalized;
        Vector3 bone3 = (refHand.Bones[22].Transform.position - refHand.Bones[14].Transform.position).normalized;
        Vector3 bone4 = (refHand.Bones[23].Transform.position - refHand.Bones[18].Transform.position).normalized;
        Vector3 bone5 = (refHand.Bones[9].Transform.position - refHand.Bones[0].Transform.position).normalized;

        Vector3 avg = (bone1 + bone2 + bone3 + bone4) * 0.25f;
        _fistValue = Vector3.Dot(-bone5, avg.normalized) * 0.5f + 0.5f;
    }

    /// <summary>
    /// Self-explanatory
    /// </summary>
    public OVRSkeleton GetActiveHand()
    {
        if (_usingHands)
        {
            OVRSkeleton refHand = _gameController == OVRInput.Controller.LHand ? _leftHand : _rightHand;
            return refHand;
        }
        return null;
    }

    /// <summary>
    /// Get a transform for attaching the UI.
    /// </summary>
    public Transform GetControllingHand(int boneID)
    {
        bool usingLeft = _gameController == OVRInput.Controller.LTouch || _gameController == OVRInput.Controller.LHand;
        Transform hand = usingLeft ? _leftHandAnchor : _rightHandAnchor;
        if (_usingHands)
        {
            if (_rightHand && _leftHand)
            {
                // thumb tips, so menu is within view
                if (boneID >= 0 && boneID < _leftHand.Bones.Count)
                {
                    hand = usingLeft ? _leftHand.Bones[boneID].Transform : _rightHand.Bones[boneID].Transform;
                }
            }
        }
        return hand;
    }

    /// <summary>
    /// Someday, passthrough might be color...
    /// </summary>
    public bool IsGreyPassthrough()
    {
        // the headset identifier for Cambria has changed and will change last minute
        // this function serves to slightly change the color tuning of the experience depending on device
        // until things stabilize, force the EXPERIENCE to assume greyscale, but Passthrough itself is default to the device (line 124)
        // see this thread: https://fb.workplace.com/groups/272459344365710/permalink/479111297033846/
        return true;
    }

    /// <summary>
    /// Because of anchors, the ground floor may not be perfectly at y=0.
    /// </summary>
    public float GetFloorHeight()
    {
        return _floorHeight;
    }

    /// <summary>
    /// The floor is generally at y=0, but in cases where the Scene floor anchor isn't, shift the whole world.
    /// </summary>
    public void MoveGroundFloor(float height)
    {
        _floorHeight = height;
        WorldBeyondEnvironment.Instance.MoveGroundFloor(height);
    }
}
