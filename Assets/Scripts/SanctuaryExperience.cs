using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SanctuaryExperience : MonoBehaviour
{
    static public SanctuaryExperience Instance = null;

    [Header("Scene Preview")]
    [SerializeField] private OVRSceneManager _sceneManager;
    [SerializeField] private OVRPassthroughLayer _passthroughLayer;
    float _ceilingHeight = 3.0f;
    bool _foundRoom = false;
    float _floorHeight = 0.0f;
    const float _sceneCheckTime = 1.0f;
    int _entCount = 0;
    int _failureCount = 0;
    float _timer = 0.0f;

    [HideInInspector]
    public OVRSceneObject[] _sceneObjects;

    [Header("Game Pieces")]
    public SanctuaryPet _pet;
    int _ozDiscoveryCount = 0;
    [HideInInspector]
    public bool _ozDiscovered = false;
    public VirtualRoom _vrRoom;
    public LightBeam _lightBeam;
    Vector3 _toyBasePosition = Vector3.zero;
    public Transform _finalOzTarget;
    public Transform _finalOzRamp;
    [HideInInspector]
    public SpaceshipTrigger _spaceShipAnimator;

    // Energy balls
    Transform _ballContainer;
    public GameObject _ballPrefab;
    BallCollectable _hiddenBallCollectable = null;

    // the little gems spawned when a ball collides
    List<BallDebris> _ballDebrisObjects;
    const int _maxBallDebris = 100;

    [HideInInspector]
    public int _ballCount = 0;
    const int _startingBallCount = 4;
    [HideInInspector]
    public int _ballTargetCount = 10;
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
    float _titleFadeTimer = 0.0f;
    PassthroughStylist _passthroughStylist;
    Color _cameraDark = new Color(0, 0, 0, 0.75f);

    public enum SanctuaryChapter
    {
        Void,               // waiting to find the Scene objects
        Title,              // the title screen
        Introduction,       // Passthrough fades in from black
        OzBaitsYou,         // light beam is visible
        SearchForOz,        // flashlight-hunting for Oz & balls
        OzExploresReality,  // Oz walks around your room
        TheGreatBeyond,     // room walls come down
        Ending              // Oz has collected all balls, flies away in ship
    };
    public SanctuaryChapter _currentChapter { get; private set; }

    [Header("Hands")]
    public OVRSkeleton _leftHand;
    public OVRSkeleton _rightHand;
    public Transform _leftHandAnchor;
    public Transform _rightHandAnchor;
    public OVRInput.Controller _gameController { get; private set; }
    public bool _usingHands { get; private set; }
    // us a fist open/close as the main button
    bool _handClosed = false;
    public delegate void OnHand();
    public OnHand OnHandOpenDelegate;
    public OnHand OnHandClosedDelegate;
    public OnHand OnHandDelegate;
    [HideInInspector]
    public float _fistValue = 0.0f;
    public SkinnedMeshRenderer _leftHandVisual;
    public SkinnedMeshRenderer _rightHandVisual;

    private void Awake()
    {
        if (!Instance)
        {
            Instance = this;
        }

        _currentChapter = SanctuaryChapter.Void;
        _gameController = OVRInput.Controller.RTouch;
        _fadeSphere.gameObject.SetActive(true);

        // copy the black fade sphere to be behind the intro title
        // this shouldn't be necessary once color controls can be added to color PT
        _backgroundFadeSphere = Instantiate(_fadeSphere.gameObject, _fadeSphere.transform.parent);

        _usingHands = false;

        _passthroughLayer.colorMapEditorType = IsGreyPassthrough() ? OVRPassthroughLayer.ColorMapEditorType.Controls : OVRPassthroughLayer.ColorMapEditorType.None;

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

        _spaceShipAnimator = _finalOzTarget.GetComponent<SpaceshipTrigger>();

        _titleScreen = Instantiate(_titleScreenPrefab);
        _titleScreen.SetActive(false);
        _endScreen = Instantiate(_endScreenPrefab);
        // end screen needs to render above the black fade sphere, which is 4999
        _endScreen.GetComponent<MeshRenderer>().sharedMaterial.renderQueue = 5000;
        _endScreen.SetActive(false);
    }

    public void Start()
    {
        OVRManager.eyeFovPremultipliedAlphaModeEnabled = false;

        if (MultiToy.Instance) MultiToy.Instance.InitializeToys();
        _pet.Initialize();
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

        if (_currentChapter <= SanctuaryChapter.OzBaitsYou && _currentChapter > SanctuaryChapter.Title)
        {
            _usingHands = (
                        OVRInput.GetActiveController() == OVRInput.Controller.Hands ||
                        OVRInput.GetActiveController() == OVRInput.Controller.LHand ||
                        OVRInput.GetActiveController() == OVRInput.Controller.RHand ||
                        OVRInput.GetActiveController() == OVRInput.Controller.None);
            if (_leftHandVisual && _rightHandVisual)
            {
               _leftHandVisual.enabled = _usingHands;
               _rightHandVisual.enabled = _usingHands;
            }
        }

        switch (_currentChapter)
        {
            case SanctuaryChapter.Void:
                GetRoomFromScene();
                break;
            case SanctuaryChapter.Title:
                PositionTitleScreens(false);
                break;
            case SanctuaryChapter.Introduction:
                // Passthrough fading is done in the PlayIntroPassthrough coroutine
                break;
            case SanctuaryChapter.OzBaitsYou:
                // if either hand is getting close to the toy, grab it and start the experience
                float handRange = 0.2f;
                float leftRange = Vector3.Distance(OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch), MultiToy.Instance.transform.position);
                float rightRange = Vector3.Distance(OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch), MultiToy.Instance.transform.position);
                bool leftHandApproaching = leftRange <= handRange;
                bool rightHandApproaching = rightRange <= handRange;
                if (MultiToy.Instance._toyVisible && (leftHandApproaching || rightHandApproaching))
                {
                    if (_usingHands)
                    {
                        _gameController = leftRange < rightRange ? OVRInput.Controller.LHand : OVRInput.Controller.RHand;
                        MultiToy.Instance.SetToyMesh(MultiToy.ToyOption.None);
                    }
                    else
                    {
                        _gameController = leftRange < rightRange ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
                        MultiToy.Instance.ShowPassthroughGlove(true, _gameController == OVRInput.Controller.RTouch);
                    }
                    MultiToy.Instance.EnableCollision(!_usingHands);
                    MultiToy.Instance.ChildLightCone(_usingHands);
                    _lightBeam.CloseBeam();
                    MultiToy.Instance._grabToy_1.Play();
                    ForceChapter(SanctuaryChapter.SearchForOz);
                }
                break;
            case SanctuaryChapter.SearchForOz:
                break;
            case SanctuaryChapter.OzExploresReality:
                break;
            case SanctuaryChapter.TheGreatBeyond:
                VirtualRoom.Instance.CheckForGuardian();
                break;
            case SanctuaryChapter.Ending:
                VirtualRoom.Instance.CheckForGuardian();
                if (_endScreen.activeSelf)
                {
                    PositionTitleScreens(false);
                }
                break;
        }

        // make sure there's never a situation with no balls to grab
        bool noHiddenBall = (_hiddenBallCollectable == null);
        bool flashlightActive = MultiToy.Instance.IsFlashlightActive();
        bool validMode = (_currentChapter > SanctuaryChapter.SearchForOz && _currentChapter < SanctuaryChapter.Ending);
        
        // note: this logic only executes after Oz enters reality
        // before that, the experience is scripted, so balls shouldn't spawn so randomly
        if (flashlightActive && noHiddenBall && _ozDiscoveryCount >= 2 && validMode)
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


        bool roomSparkleRingVisible = (_currentChapter >= SanctuaryChapter.OzExploresReality && _hiddenBallCollectable);
        roomSparkleRingVisible |= (_currentChapter == SanctuaryChapter.SearchForOz && (_pet.gameObject.activeSelf || (_hiddenBallCollectable && !_hiddenBallCollectable._wasShot)));

        Vector3 ripplePosition = _hiddenBallCollectable ? _hiddenBallCollectable.transform.position : Vector3.one * -1000.0f;
        if (_currentChapter == SanctuaryChapter.SearchForOz)
        {
            ripplePosition = _pet.gameObject.activeSelf ? _pet.transform.position : ripplePosition;
        }
        float effectSpeed = Time.deltaTime * 2.0f;
        _vrRoomEffectTimer += roomSparkleRingVisible ? effectSpeed : -effectSpeed;
        if (_vrRoomEffectTimer >= 0.0f)
        {
            VirtualRoom.Instance.SetWallEffectParams(ripplePosition, Mathf.Clamp01(_vrRoomEffectTimer));
        }
        _vrRoomEffectTimer = Mathf.Clamp01(_vrRoomEffectTimer);
    }

    public void ForceChapter(SanctuaryChapter forcedChapter)
    {
        StopAllCoroutines();
        KillControllerVibration();
        MultiToy.Instance.SetToy(forcedChapter);
        _currentChapter = forcedChapter;
        SanctuaryEnvironment.Instance.ShowEnvironment((int)_currentChapter > (int)SanctuaryChapter.SearchForOz);

        if ((int)_currentChapter < (int)SanctuaryChapter.SearchForOz) _mainCamera.backgroundColor = _cameraDark;

        _pet.gameObject.SetActive((int)_currentChapter >= (int)SanctuaryChapter.OzExploresReality);
        if (_pet.gameObject.activeSelf) { _pet.SetOzChapter(_currentChapter); }
        _pet.PlaySparkles(false);

        if (_lightBeam) { _lightBeam.gameObject.SetActive(false); }
        if (_titleScreen) _titleScreen.SetActive(false);
        if (_endScreen) _endScreen.SetActive(false);

        switch (_currentChapter)
        {
            case SanctuaryChapter.Title:
                AudioManager.SetSnapshot_Title();
                MusicManager.Instance.PlayMusic(MusicManager.Instance.IntroMusic);
                StartCoroutine(ShowTitleScreen());
                VirtualRoom.Instance.ShowAllWalls(false);
                SanctuaryTutorial.Instance.DisplayMessage(SanctuaryTutorial.TutorialMessage.None);
                SanctuaryEnvironment.Instance._sun.enabled = false;
                break;
            case SanctuaryChapter.Introduction:
                AudioManager.SetSnapshot_Introduction();
                VirtualRoom.Instance.CreateSpecialEffectMesh();
                VirtualRoom.Instance.AnimateEffectMesh(_mainCamera.transform);
                StartCoroutine(PlayIntroPassthrough());
                break;
            case SanctuaryChapter.OzBaitsYou:
                _passthroughStylist.ResetPassthrough(0.1f);
                StartCoroutine(PlaceToyRandomly(2.0f));
                break;
            case SanctuaryChapter.SearchForOz:
                VirtualRoom.Instance.HideEffectMesh();
                _ozDiscovered = false;
                _ozDiscoveryCount = 0;
                _ballCount = _startingBallCount;
                _passthroughStylist.ResetPassthrough(0.1f);
                SanctuaryEnvironment.Instance._sun.enabled = true;
                StartCoroutine(CountdownToFlashlight(5.0f));
                StartCoroutine(FlickerCameraToClearColor());
                break;
            case SanctuaryChapter.OzExploresReality:
                AudioManager.SetSnapshot_OzExploresReality();
                _passthroughStylist.ResetPassthrough(0.1f);
                VirtualRoom.Instance.ShowAllWalls(true);
                VirtualRoom.Instance.SetRoomSaturation(1.0f);
                StartCoroutine(UnlockBallShooter(5.0f));
                StartCoroutine(UnlockWallToy(20.0f));
                _spaceShipAnimator.StartIdleSound(); // Start idle sound here - mix will mute it.
                break;
            case SanctuaryChapter.TheGreatBeyond:
                AudioManager.SetSnapshot_TheGreatBeyond();
                _passthroughStylist.ResetPassthrough(0.1f);
                SetEnvironmentSaturation(IsGreyPassthrough() ? 0.0f : 1.0f);
                if (IsGreyPassthrough()) StartCoroutine(SaturateEnvironmentColor());
                MusicManager.Instance.PlayMusic(MusicManager.Instance.PortalOpen);
                MusicManager.Instance.PlayMusic(MusicManager.Instance.TheGreatBeyondMusic);
                WitConnector.Instance.ResetInstructionCount();
                break;
            default:
                break;
        }
        Debug.Log("SanctuaryDebug: started chapter " + _currentChapter);
    }

    IEnumerator PlayIntroPassthrough()
    {
        _fadeSphere.gameObject.SetActive(false);
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
            // Passthrough controls don't work on color, so we use the black fade sphere instead
            if (!IsGreyPassthrough())
            {
                _fadeSphere.sharedMaterial.SetColor("_Color", Color.Lerp(Color.black, Color.clear, normTime));
            }

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
        ForceChapter(SanctuaryChapter.OzBaitsYou);
    }

    IEnumerator FlickerCameraToClearColor()
    {
        float timer = 0.0f;
        float flickerTimer = 0.5f;
        while (timer <= flickerTimer)
        {
            timer += Time.deltaTime;
            float normTimer = Mathf.Clamp01(0.5f * timer / flickerTimer);
            _mainCamera.backgroundColor = Color.Lerp(Color.clear, _cameraDark, MultiToy.Instance.EvaluateFlickerCurve(normTimer));
            if (timer >= flickerTimer)
            {
                VirtualRoom.Instance.ShowAllWalls(true);
                VirtualRoom.Instance.SetRoomSaturation(IsGreyPassthrough() ? 0 : 1);
                SanctuaryEnvironment.Instance.ShowEnvironment(true);
            }
            yield return null;
        }
    }

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

            float normTimer = Mathf.Clamp01(timer /lerpTime);

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
        ForceChapter(SanctuaryChapter.Introduction);
    }

    IEnumerator UnlockBallShooter(float countdown)
    {
        yield return new WaitForSeconds(countdown);
        // ensures the flashlight works again, once it's switched back to
        MultiToy.Instance.SetFlickerTime(0.0f);

        if (!_usingHands)
        {
            MultiToy.Instance.UnlockBallShooter();
            OVRInput.SetControllerVibration(1, 1, _gameController);
            yield return new WaitForSeconds(1.0f);
            KillControllerVibration();
        }
    }

    IEnumerator UnlockWallToy(float countdown)
    {
        yield return new WaitForSeconds(countdown);
        MultiToy.Instance.UnlockWallToy();
        OVRInput.SetControllerVibration(1, 1, _gameController);
        yield return new WaitForSeconds(1.0f);
        KillControllerVibration();
    }

    IEnumerator PlaceToyRandomly(float spawnTime)
    {
        yield return new WaitForSeconds(spawnTime);
        MultiToy.Instance.ShowToy(true);
        MultiToy.Instance.SetToyMesh(MultiToy.ToyOption.Flashlight);
        MultiToy.Instance.ShowPassthroughGlove(false);
        _toyBasePosition = GetRandomToyPosition();
        _lightBeam.gameObject.SetActive(true);
        _lightBeam.transform.localScale = new Vector3(1, _ceilingHeight, 1);
        _lightBeam.Prepare(_toyBasePosition);
    }

    IEnumerator CountdownToFlashlight(float spawnTime)
    {
        yield return new WaitForSeconds(spawnTime - 0.5f);
        OVRInput.SetControllerVibration(1, 1, _gameController);
        MultiToy.Instance.EnableFlashlightCone(true);
        if (_usingHands)
        {
            SanctuaryTutorial.Instance.DisplayMessage(SanctuaryTutorial.TutorialMessage.EnableFlashlight);
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
        StartCoroutine(SpawnOzRandomly(true, Random.Range(_spawnTimeMin, _spawnTimeMax)));
    }

    void GetRoomFromScene()
    {
        if (_foundRoom)
        {
            return;
        }

        if (Application.isFocused)
        {
            _timer += Time.deltaTime;
        }
        if (Application.isEditor)
        {
            _vrRoom.Initialize();
            SanctuaryEnvironment.Instance.Initialize();
            _foundRoom = true;

            ForceChapter(SanctuaryChapter.Title);
            return;
        }
        _sceneObjects = FindObjectsOfType<OVRSceneObject>();
        if (_sceneObjects.Length == 0)
        {
            if (_timer >= _sceneCheckTime)
            {
                if (_failureCount >= 3)
                {
                    // TEMP: this is only hit because Scene is unreliable during development, and a roombox is never found
                    // initialize with a default room
                    _vrRoom.Initialize();
                    SanctuaryEnvironment.Instance.Initialize();
                    _foundRoom = true;

                    ForceChapter(SanctuaryChapter.Title);
                    return;
                }
                else
                {
                    Debug.Log("SanctuaryDebug found " + _sceneObjects.Length + " entities");
                    _timer = 0;
                    SanctuaryTutorial.Instance.DisplayMessage(SanctuaryTutorial.TutorialMessage.ERROR_NoScene);
                    _failureCount++;
                }
            }
        }
        else
        {
            if (_sceneObjects.Length > _entCount)
            {
                _timer = 0;
                _entCount = _sceneObjects.Length;
            }
        }

        if (_timer >= _sceneCheckTime)
        {
            // this may not be ideal, as there are inefficiencies
            // basically, we crawl the scene for the Unity GameObjects
            // TODO: find the reliable native "room loaded" event
            _sceneObjects = FindObjectsOfType<OVRSceneObject>();
            Debug.Log("SanctuaryDebug found " + _sceneObjects.Length + " entities");
            if (_sceneObjects.Length > 0)
            {
                for (int i = 0; i< _sceneObjects.Length; i++)
                {
                    Debug.Log("SanctuaryDebug object " + i + ": " + _sceneObjects[i].classification.labels[0]);
                }
                _vrRoom.Initialize(_sceneObjects);
                SanctuaryEnvironment.Instance.Initialize();
                _foundRoom = true;

                ForceChapter(SanctuaryChapter.Title);
            }
        }
    }

    public void DiscoveredBall(BallCollectable collected)
    {
        _ballCount++;
        SanctuaryTutorial.Instance.HideMessage(SanctuaryTutorial.TutorialMessage.BallSearch);
        SanctuaryTutorial.Instance.HideMessage(SanctuaryTutorial.TutorialMessage.NoBalls);

        // when using hands, make sure the discovered ball was actually hidden
        // otherwise, grabbing any ball will advance the script in undesirable ways
        if (_usingHands && collected._wasShot)
        {
            return;
        }
        if (_currentChapter == SanctuaryChapter.SearchForOz)
        {
            // in case we already picked up a ball and triggered the coroutine, cancel the old one
            // this is only a problem when using hands, since the balls stay around
            if (_usingHands)
            {
                StopAllCoroutines();
            }
            StartCoroutine(SpawnOzRandomly(false, Random.Range(_spawnTimeMin, _spawnTimeMax)));
            return;
        }
    }

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
                if (bc._ballState == BallCollectable.BallStatus.Available || bc._ballState == BallCollectable.BallStatus.Released)
                {
                    closestAngle = thisViewAngle;
                    closestBall = bc;
                }  
            }
        }
        return closestBall;
    }

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
                    new Color(0,0.5f,1,0.5f),
                    Color.black,
                    Color.white);
        _passthroughStylist.ShowStylizedPassthrough(weirdPassthrough, 0.2f);

        SanctuaryEnvironment.Instance.FlickerSun();

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

        ForceChapter(SanctuaryChapter.OzExploresReality);
        _pet.EndLookTarget();
    }

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

        // play Oz teleport particles, only on the middle discovery
        if (_ozDiscoveryCount == 1)
        {
            _pet.PlayTeleport();
        }

        // hide Oz
        _ozDiscovered = false;
        _ozDiscoveryCount++;
        _pet.ResetAnimFlags();
        float colorSaturation = IsGreyPassthrough() ? Mathf.Clamp01(_ozDiscoveryCount / 3.0f) : 1.0f;
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

        if (_ozDiscoveryCount == 1)
        {
            SanctuaryTutorial.Instance.DisplayMessage(SanctuaryTutorial.TutorialMessage.BallSearch);
            _hiddenBallCollectable.SetState(BallCollectable.BallStatus.Available);
        }
        else
        {
            // spawn ball only after the first discovery (first ball already exists)
            yield return new WaitForSeconds(Random.Range(_spawnTimeMin, _spawnTimeMax));
            SpawnHiddenBall();
        }
    }

    IEnumerator SpawnOzRandomly(bool firstTimeSpawning, float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        _pet.gameObject.SetActive(true);
        Vector3 spawnPos = GetRandomPositionBehindPlayer();
        Vector3 fwd = _mainCamera.transform.position - spawnPos;
        Quaternion ozRotation = Quaternion.LookRotation(new Vector3(fwd.x, 0, fwd.z));
        _pet.transform.rotation = ozRotation;
        _pet.transform.position = spawnPos - _pet.transform.right * 0.1f;
        _pet.PlaySparkles(true);
        _pet.SetLookDirection(fwd.normalized);
        if (firstTimeSpawning)
        {
            _pet.PrepareInitialDiscoveryAnim();
            _pet.SetMaterialSaturation(IsGreyPassthrough() ? 0.0f : 1.0f);
            GameObject hiddenBall = Instantiate(_ballPrefab);
            _hiddenBallCollectable = hiddenBall.GetComponent<BallCollectable>();
            _hiddenBallCollectable.PlaceHiddenBall(spawnPos + _pet.transform.right * 0.05f + Vector3.up * 0.06f, 0);
            _hiddenBallCollectable.SetState(BallCollectable.BallStatus.Visible);
        }
    }

    void SpawnHiddenBall()
    {
        // if spawning on a wall, track the id:
        // if the wall is toggled off, we need to destroy the ball
        int wallID = -1;
        GameObject hiddenBall = Instantiate(_ballPrefab);
        _hiddenBallCollectable = hiddenBall.GetComponent<BallCollectable>();
        Vector3 spawnPos = GetRandomBallPosition(ref wallID);
        _hiddenBallCollectable.PlaceHiddenBall(spawnPos, wallID);
    }

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

        if (_currentChapter == SanctuaryExperience.SanctuaryChapter.OzExploresReality)
        {
            SanctuaryTutorial.Instance.DisplayMessage(SanctuaryTutorial.TutorialMessage.None);
            ForceChapter(SanctuaryChapter.TheGreatBeyond);
        }
    }

    void KillControllerVibration()
    {
        OVRInput.SetControllerVibration(1, 0, _gameController);
    }

    public Vector3 GetRandomPositionBehindPlayer()
    {
        Vector3 floorPos = new Vector3(_mainCamera.transform.position.x, GetFloorHeight(), _mainCamera.transform.position.z);
        Vector3 randomPos = _mainCamera.transform.position - _mainCamera.transform.forward;

        // shoot rays out from player, a few cm above ground
        // return the farthest hit, behind player
        Vector3 curbHeight = floorPos + Vector3.up * 0.2f;
        Vector3 startingVec = new Vector3(-_mainCamera.transform.right.x, GetFloorHeight(), -_mainCamera.transform.right.z).normalized;

        float farthestHit = 0.0f;
        int sliceCount = 4;
        for (int i = 0; i <= sliceCount; i++)
        {
            RaycastHit hitInfo;
            LayerMask ozSpawnLayer = LayerMask.GetMask("RoomBox", "Furniture");
            if (Physics.Raycast(curbHeight, startingVec, out hitInfo, 100.0f, 1 << ozSpawnLayer))
            {
                if (Vector3.Distance(curbHeight, hitInfo.point) > farthestHit)
                {
                    farthestHit = Vector3.Distance(curbHeight, hitInfo.point);
                    randomPos = hitInfo.point + hitInfo.normal * 0.5f;
                    randomPos = new Vector3(randomPos.x, GetFloorHeight(), randomPos.z);
                }
            }
            startingVec = Quaternion.Euler(0, -180.0f / sliceCount, 0) * startingVec;
        }

        return randomPos;
    }

    // return a random surface position behind player
    public Vector3 GetRandomBallPosition(ref int wallID)
    {
        // default case; spawn it randomly on the floor, which has to exist
        List<Vector3> randomPositions = new List<Vector3>();
        List<int> matchingWallID = new List<int>();

        const int numSamples = 8;
        for (int i = 0; i < numSamples; i++)
        {
            // try a random position behind toy
            float localX = Random.Range(-1.0f, 1.0f);
            float localY = Random.Range(-1.0f, 0.0f);
            float localZ = Random.Range(-1.0f, 0.0f);
            Vector3 randomVector = MultiToy.Instance.transform.rotation * (new Vector3(localX, localY, localZ).normalized);
            LayerMask ballSpawnLayer = LayerMask.GetMask("RoomBox", "Furniture");
            RaycastHit[] roomboxHit = Physics.RaycastAll(MultiToy.Instance.transform.position, randomVector, 100, ballSpawnLayer);
            float closestSurface = 100.0f;
            bool foundSurface = false;
            Vector3 bestPos = Vector3.zero;
            int bestID = -1;
            foreach (RaycastHit hit in roomboxHit)
            {
                GameObject hitObj = hit.collider.gameObject;
                if (hitObj.GetComponent<SanctuaryRoomObject>() && !hitObj.GetComponent<SanctuaryRoomObject>()._passthroughWallActive)
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
                    if (hitObj.GetComponent<SanctuaryRoomObject>())
                    {
                        surfId = hitObj.GetComponent<SanctuaryRoomObject>()._surfaceID;
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

    public Vector3 GetRandomToyPosition()
    {
        // default position is where camera is.  Shouldn't happen, but it's a fallback
        Vector3 finalPos = new Vector3(_mainCamera.transform.position.x, GetFloorHeight(), _mainCamera.transform.position.z);

        // shoot rays out from player, a few cm above ground
        Vector3 curbHeight = finalPos + Vector3.up * 0.1f;
        Vector3 startingVec = new Vector3(_mainCamera.transform.right.x, GetFloorHeight(), _mainCamera.transform.right.z).normalized;

        // select the farthest candidate position
        float farthestPosition = 0.0f;
        int sliceCount = 4;
        for (int i = 0; i <= sliceCount; i++)
        {
            float closestHit = 1000.0f;
            Vector3 closestPos = finalPos;
            LayerMask ballSpawnLayer = LayerMask.GetMask("RoomBox", "Furniture");
            RaycastHit[] roomboxHit = Physics.RaycastAll(curbHeight, startingVec, 100, ballSpawnLayer);
            foreach (RaycastHit hit in roomboxHit)
            {
                // need to find the closest impact, because hit order isn't guaranteed
                float thisHit = Vector3.Distance(curbHeight, hit.point);
                if (thisHit < closestHit)
                {
                    closestHit = thisHit;
                    closestPos = (hit.point + hit.normal * 0.3f);
                }
            }
            // get a halfway point, so beam isn't always flush against a wall
            Vector3 candidatePos = (curbHeight + closestPos) * 0.5f;
            float distanceToCandidate = Vector3.Distance(curbHeight, candidatePos);
            if (distanceToCandidate > farthestPosition)
            {
                farthestPosition = distanceToCandidate;
                finalPos = candidatePos;
            }
            
            startingVec = Quaternion.Euler(0, -180.0f / sliceCount, 0) * startingVec;
        }

        return new Vector3(finalPos.x, 1.0f + GetFloorHeight(), finalPos.z); ;
    }

    public void FlyAwayUFO()
    {
        StartCoroutine(DoEndingSequence());
    }

    IEnumerator DoEndingSequence()
    {
        AudioManager.SetSnapshot_Ending();
        _fadeSphere.gameObject.SetActive(true);
        _fadeSphere.sharedMaterial.renderQueue = 4999;
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
                SanctuaryEnvironment.Instance.FadeOutdoorAudio(1 - fadeValue);
                if (timer >= flyingAwayTime)
                {
                    SanctuaryEnvironment.Instance.SetOutdoorAudioParams(Vector3.zero, false);
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
            ForceChapter(SanctuaryChapter.Title);
        }
    }

    public void PlayOzDiscoveryAnim()
    {
        if (!_ozDiscovered)
        {
            _ozDiscovered = true;
            // the final discovery, after which Oz enters reality
            if (_ozDiscoveryCount == 2)
            {
                _pet._boneSim.gameObject.SetActive(true);
                _pet.PlayRandomOzDiscoveryAnim();
                StartCoroutine(MalfunctionFlashlight());
            }
            // first discovery, play the unique discovery anim
            else if (_ozDiscoveryCount == 0)
            {
                _pet.PlayInitialDiscoveryAnim();
                StartCoroutine(FlickerFlashlight(4.0f));
            }
            // second discovery, play a random "delight" anim
            else
            {
                _pet.PlayRandomOzDiscoveryAnim();
                StartCoroutine(FlickerFlashlight(2.0f));
            }
        }
    }

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
        Vector3 pitch = Vector3.Lerp(Vector3.down * 0.05f, Vector3.up * 0.05f,  Mathf.Clamp01(_titleFadeTimer/8.0f));
        Quaternion targetRotation = Quaternion.LookRotation(-targetLook + pitch, Vector3.up);

        float dollyDirection = _currentChapter == SanctuaryChapter.Title ? -1.0f : 1.0f;
        float textDistance = (_currentChapter == SanctuaryChapter.Title ? 5 : 4) + (dollyDirection * _titleFadeTimer * 0.1f);

        _titleScreen.transform.position = _mainCamera.transform.position + targetLook * textDistance;
        _titleScreen.transform.rotation = targetRotation;

        _endScreen.transform.position = _titleScreen.transform.position;
        _endScreen.transform.rotation = _titleScreen.transform.rotation;

        // hardcoded according to the fade out time of 13 seconds
        // fade in for 2 seconds, fade out after 8 seconds
        float endFade = Mathf.Clamp01(_titleFadeTimer * 0.5f) * (1.0f - Mathf.Clamp01((_titleFadeTimer - 8) * 0.25f));
        _endScreen.GetComponent<MeshRenderer>().sharedMaterial.SetColor("_Color", Color.Lerp(Color.black, Color.white, endFade));
    }

    void SetEnvironmentSaturation(float normSat)
    {
        // convert a normalized value to what the shader intakes
        float actualSat = Mathf.Lerp(1.0f, 0.08f, normSat);
        foreach (Material mtl in _environmentMaterials)
        {
            mtl.SetFloat("_SaturationDistance", actualSat);
        }
    }

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

    public void AddBallToWorld(BallCollectable newBall)
    {
        newBall.gameObject.transform.parent = _ballContainer;
    }

    public void AddBallDebrisToWorld(GameObject newDebris)
    {
        _ballDebrisObjects.Add(newDebris.GetComponent<BallDebris>());
    }

    public void AbsorbDebris(Vector3 absorbPosition)
    {
        for (int i = 0; i < _ballDebrisObjects.Count; i++)
        {
            if (_ballDebrisObjects[i] != null)
            {
                Vector3 toHand = absorbPosition - _ballDebrisObjects[i].transform.position;
                float range = Vector3.Dot(MultiToy.Instance.GetFlashlightDirection(), toHand.normalized);
                if (range < -0.8f)
                {
                    float strength = (-range - 0.8f) / 0.2f;
                    _ballDebrisObjects[i].AddForce(toHand.normalized, strength);
                }
            }
        }
    }

    public void RepelDebris(Vector3 repelPosition)
    {
        for (int i = 0; i < _ballDebrisObjects.Count; i++)
        {
            if (_ballDebrisObjects[i] != null)
            {
                Vector3 forceDirection = _ballDebrisObjects[i].transform.position - repelPosition;
                if (forceDirection.magnitude < 0.5f)
                {
                    float strength = 1.0f - Mathf.Clamp01(forceDirection.magnitude * 2);
                    _ballDebrisObjects[i].AddForce(forceDirection.normalized, strength * 2);
                }
            }
        }
    }

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

    public void RemoveBallFromWorld(BallCollectable newBall)
    {
        Destroy(newBall.gameObject);
    }

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

    public void GetDominantHand(ref Vector3 handPos, ref Quaternion handRot)
    {
        if (_usingHands)
        {
            bool L_hand = _gameController == OVRInput.Controller.LHand;
            OVRSkeleton refHand = (_gameController == OVRInput.Controller.LHand) ? _leftHand : _rightHand;

            Vector3 handFwd = refHand.Bones[9].Transform.position - refHand.Bones[0].Transform.position;
            Vector3 handRt = (refHand.Bones[9].Transform.position - refHand.Bones[6].Transform.position) * (L_hand ? -1.0f : 1.0f);
            Vector3.OrthoNormalize(ref handFwd, ref handRt);
            Vector3 handUp = Vector3.Cross(handFwd, handRt);
            handRot = Quaternion.LookRotation(handFwd, handUp) * Quaternion.Euler(60,0,0);
            handPos = refHand.Bones[9].Transform.position - handRot * Vector3.up * 0.05f;
        }
        else
        {
            handPos = OVRInput.GetLocalControllerPosition(_gameController);
            handRot = OVRInput.GetLocalControllerRotation(_gameController);
        }
    }

    public void CalculateFistStrength()
    {
        OVRSkeleton refHand = (_gameController == OVRInput.Controller.LHand) ? _leftHand : _rightHand;
        if (!refHand || !_usingHands) 
        {
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

    public Vector3 GetPalmBallPosition(Quaternion handRotation)
    {
        bool lefty = _gameController == OVRInput.Controller.LHand;
        OVRSkeleton refHand = lefty ? _leftHand : _rightHand;
        Vector3 centroid = refHand.Bones[9].Transform.position;
        Vector3 offset = handRotation * (Vector3.up * -0.04f + Vector3.forward * 0.05f);

        return (centroid + offset);
    }

    public OVRSkeleton GetActiveHand()
    {
        if (_usingHands)
        {
            OVRSkeleton refHand = _gameController == OVRInput.Controller.LHand ? _leftHand : _rightHand;
            return refHand;
        }
        return null;
    }

    public Transform GetControllingHand(int boneID)
    {
        bool usingLeft = SanctuaryExperience.Instance._gameController == OVRInput.Controller.LTouch || SanctuaryExperience.Instance._gameController == OVRInput.Controller.LHand;
        Transform hand = usingLeft ? _leftHandAnchor : _rightHandAnchor;
        if (SanctuaryExperience.Instance._usingHands)
        {
            OVRSkeleton refLeft = SanctuaryExperience.Instance._leftHand;
            OVRSkeleton refRight = SanctuaryExperience.Instance._rightHand;
            if (refRight && refLeft)
            {
                // thumb tips, so menu is within view
                if (boneID >= 0 && boneID < refLeft.Bones.Count)
                {
                    hand = usingLeft ? refLeft.Bones[boneID].Transform : refRight.Bones[boneID].Transform;
                }
            }
        }
        return hand;
    }

    public bool IsGreyPassthrough()
    {
        return (OVRPlugin.GetSystemHeadsetType() == OVRPlugin.SystemHeadset.Oculus_Quest ||
            OVRPlugin.GetSystemHeadsetType() == OVRPlugin.SystemHeadset.Oculus_Quest_2);
    }

    public float GetFloorHeight()
    {
        return _floorHeight;
    }

    public void MoveGroundFloor(float height)
    {
        _floorHeight = height;
        SanctuaryEnvironment.Instance.MoveGroundFloor(height);
    }
}
