// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Meta.XR.MRUtilityKit;
using Oculus.Interaction;
using Oculus.Interaction.DistanceReticles;
using TheWorldBeyond.Audio;
using TheWorldBeyond.Character;
using TheWorldBeyond.Character.Oppy;
using TheWorldBeyond.Environment;
using TheWorldBeyond.Environment.RoomEnvironment;
using TheWorldBeyond.Toy;
using TheWorldBeyond.Utils;
using TheWorldBeyond.VFX;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TheWorldBeyond.GameManagement
{
    public class WorldBeyondManager : MonoBehaviour
    {
        public static WorldBeyondManager Instance = null;

        [Header("Scene Preview")]
        [SerializeField] private OVRPassthroughLayer m_passthroughLayer;
        private bool m_sceneModelLoaded = false;
        private float m_floorHeight = 0.0f;

        // after the Scene has been loaded successfuly, we still wait a frame before the data has "settled"
        // e.g. VolumeAndPlaneSwitcher needs to happen first, and script execution order also isn't fixed by default
        private int m_frameWait = 0;

        [Header("Game Pieces")]
        public VirtualPet Pet;
        private int m_oppyDiscoveryCount = 0;
        [HideInInspector]
        public bool OppyDiscovered = false;
        public VirtualRoom VrRoom;
        public LightBeam LightBeam;
        private Vector3 m_toyBasePosition = Vector3.zero;
        public Transform FinalUfoTarget;
        public Transform FinalUfoRamp;
        [HideInInspector]
        public SpaceshipTrigger SpaceShipAnimator;

        // Energy balls
        private Transform m_ballContainer;
        public GameObject BallPrefab;
        private BallCollectable m_hiddenBallCollectable = null;
        private Vector3 m_hiddenBallPosition = Vector3.zero;

        // the little gems spawned when a ball collides
        private List<BallDebris> m_ballDebrisObjects;
        private const int MAX_BALL_DEBRIS = 100;

        // How many balls the player currently has
        [HideInInspector]
        public int BallCount = 0;
        private const int STARTING_BALL_COUNT = 0;
        // How many balls Oppy should eat before heading to the UFO
        // only starts incrementing during TheGreatBeyond chapter
        public int OppyTargetBallCount { private set; get; } = 2;

        private float m_ballSpawnTimer = 0.0f;
        private const float SPAWN_TIME_MIN = 3.0f;
        private const float SPAWN_TIME_MAX = 6.0f;
        private bool m_shouldSpawnBall = false;
        public GameObject WorldShockwave;
        public Material[] EnvironmentMaterials;

        [Header("Overlays")]
        public Camera MainCamera;
        public MeshRenderer FadeSphere;
        private GameObject m_backgroundFadeSphere;
        public GameObject TitleScreenPrefab;
        public GameObject EndScreenPrefab;
        private GameObject m_titleScreen;
        private GameObject m_endScreen;
        private float m_vrRoomEffectTimer = 0.0f;
        private float m_vrRoomEffectMaskTimer = 0.0f;
        private float m_titleFadeTimer = 0.0f;
        private PassthroughStylist m_passthroughStylist;
        private Color m_cameraDark = new(0, 0, 0, 0.75f);

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
        public GameChapter CurrentChapter { get; private set; }

        [Header("Hands")]
        public OVRSkeleton LeftHand;
        public OVRSkeleton RightHand;
        private OVRHand m_leftOVR;
        private OVRHand m_rightOVR;
        public Transform LeftHandAnchor;
        public Transform RightHandAnchor;
        public OVRInput.Controller GameController { get; private set; }
        // hand input for grabbing is handled by the Interaction SDK
        // otherwise, we track some basic custom poses (palm up/out, hand closed)
        public bool UsingHands { get; private set; }

        private bool m_handClosed = false;
        public delegate void OnHand();
        public OnHand OnHandOpenDelegate;
        public OnHand OnHandClosedDelegate;
        public OnHand OnHandDelegate;
        [HideInInspector]
        public float FistValue = 0.0f;
        public HandVisual LeftHandVisual;
        public HandVisual RightHandVisual;
        public HandRootOffset LeftPointerOffset;
        public HandRootOffset RightPointerOffset;

        public DistantInteractionLineVisual InteractionLineLeft;
        public DistantInteractionLineVisual InteractionLineRight;

        private float m_leftHandGrabbedBallLastDistance = Mathf.Infinity;
        private float m_rightHandGrabbedBallLastDistance = Mathf.Infinity;
        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
            }

            CurrentChapter = GameChapter.Void;
            GameController = OVRInput.Controller.RTouch;
            FadeSphere.gameObject.SetActive(true);
            FadeSphere.sharedMaterial.SetColor("_Color", Color.black);

            // copy the black fade sphere to be behind the intro title
            // this shouldn't be necessary once color controls can be added to color PT
            m_backgroundFadeSphere = Instantiate(FadeSphere.gameObject, FadeSphere.transform.parent);

            UsingHands = false;
            m_leftOVR = LeftHand.GetComponent<OVRHand>();
            m_rightOVR = RightHand.GetComponent<OVRHand>();

            m_passthroughLayer.colorMapEditorType = OVRPassthroughLayer.ColorMapEditorType.None;

            var spawnedBalls = new GameObject("SpawnedBalls");
            m_ballContainer = spawnedBalls.transform;

            m_ballDebrisObjects = new List<BallDebris>();

            m_passthroughLayer.textureOpacity = 0;
            m_passthroughStylist = gameObject.AddComponent<PassthroughStylist>();
            m_passthroughStylist.Init(m_passthroughLayer);
            var darkPassthroughStyle = new PassthroughStylist.PassthroughStyle(
                new Color(0, 0, 0, 0),
                1.0f,
                0.0f,
                0.0f,
                0.0f,
                true,
                Color.black,
                Color.black,
                Color.black);
            m_passthroughStylist.ForcePassthroughStyle(darkPassthroughStyle);

            SpaceShipAnimator = FinalUfoTarget.GetComponent<SpaceshipTrigger>();

            m_titleScreen = Instantiate(TitleScreenPrefab);
            m_titleScreen.SetActive(false);
            m_endScreen = Instantiate(EndScreenPrefab);
            // end screen needs to render above the black fade sphere, which is 4999
            m_endScreen.GetComponent<MeshRenderer>().sharedMaterial.renderQueue = 5000;
            m_endScreen.SetActive(false);
        }

        public void Start()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_ANDROID
            OVRManager.eyeFovPremultipliedAlphaModeEnabled = false;
#endif
            if (MultiToy.Instance)
            {
                MultiToy.Instance.InitializeToys();
                MultiToy.Instance.ChildLightCone(false);
            }
            Pet.Initialize();

            MRUK.Instance.SceneLoadedEvent.AddListener(SceneModelLoaded);
        }

        public void Update()
        {
            CalculateFistStrength();
            if (m_handClosed)
            {
                if (FistValue < 0.2f)
                {
                    m_handClosed = false;
                    OnHandOpenDelegate?.Invoke();
                }
                else
                {
                    OnHandDelegate?.Invoke();
                }
            }
            else
            {
                if (FistValue > 0.3f)
                {
                    m_handClosed = true;
                    OnHandClosedDelegate?.Invoke();
                }
            }

            var usingHands =
                    OVRInput.GetActiveController() is OVRInput.Controller.Hands or
                    OVRInput.Controller.LHand or
                    OVRInput.Controller.RHand or
                    OVRInput.Controller.None;
            if (usingHands != UsingHands)
            {
                UsingHands = usingHands;
                if (usingHands)
                {
                    if (GameController == OVRInput.Controller.LTouch)
                    {
                        GameController = OVRInput.Controller.LHand;
                    }
                    if (GameController == OVRInput.Controller.RTouch)
                    {
                        GameController = OVRInput.Controller.RHand;
                    }
                }
                else
                {
                    if (GameController == OVRInput.Controller.RHand)
                    {
                        GameController = OVRInput.Controller.RTouch;
                    }
                    if (GameController == OVRInput.Controller.LHand)
                    {
                        GameController = OVRInput.Controller.LTouch;
                    }
                }

                MultiToy.Instance.UseHands(UsingHands, GameController == OVRInput.Controller.RTouch);
                MultiToy.Instance.EnableCollision(!UsingHands);

                // update tutorial text when switching input, if onscreen
                WorldBeyondTutorial.Instance.UpdateMessageTextForInput();
            }

            // constantly check if the player is within the polygonal floorplan of the room
            if (CurrentChapter >= GameChapter.Title)
            {
                if (!VrRoom.IsPlayerInRoom())
                {
                    WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.ERROR_USER_WALKED_OUTSIDE_OF_ROOM);
                }
                else if (WorldBeyondTutorial.Instance.CurrentMessage == WorldBeyondTutorial.TutorialMessage.ERROR_USER_WALKED_OUTSIDE_OF_ROOM ||
                         WorldBeyondTutorial.Instance.CurrentMessage == WorldBeyondTutorial.TutorialMessage.ERROR_USER_STARTED_OUTSIDE_OF_ROOM)
                {
                    WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.None);
                }
            }

            // disable a hand if it's not tracked (avoiding ghost hands)
            if (m_leftOVR && m_rightOVR)
            {
                LeftHandVisual.ForceOffVisibility = !m_leftOVR.IsTracked;
                RightHandVisual.ForceOffVisibility = !m_rightOVR.IsTracked;
            }

            switch (CurrentChapter)
            {
                case GameChapter.Void:
                    if (m_sceneModelLoaded) GetRoomFromScene();
                    break;
                case GameChapter.Title:
                    PositionTitleScreens(false);
                    break;
                case GameChapter.Introduction:
                    // Passthrough fading is done in the PlayIntroPassthrough coroutine
                    break;
                case GameChapter.OppyBaitsYou:
                    // if either hand is getting close to the toy, grab it and start the experience
                    var handRange = 0.2f;
                    var leftRange = Vector3.Distance(OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch), MultiToy.Instance.transform.position);
                    var rightRange = Vector3.Distance(OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch), MultiToy.Instance.transform.position);
                    var leftHandApproaching = leftRange <= handRange;
                    var rightHandApproaching = rightRange <= handRange;
                    if (MultiToy.Instance.ToyVisible && (leftHandApproaching || rightHandApproaching))
                    {
                        if (usingHands)
                        {
                            GameController = leftRange < rightRange ? OVRInput.Controller.LHand : OVRInput.Controller.RHand;
                            MultiToy.Instance.SetToyMesh(MultiToy.ToyOption.None);
                        }
                        else
                        {
                            GameController = leftRange < rightRange ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
                            MultiToy.Instance.ShowPassthroughGlove(true, GameController == OVRInput.Controller.RTouch);
                        }
                        UsingHands = usingHands;

                        LightBeam.CloseBeam();
                        MultiToy.Instance.GrabToy_1.Play();
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
                    if (m_endScreen.activeSelf)
                    {
                        PositionTitleScreens(false);
                    }
                    break;
            }

            // make sure there's never a situation with no balls to grab
            var noHiddenBall = m_hiddenBallCollectable == null;
            var flashlightActive = MultiToy.Instance.IsFlashlightActive();
            var validMode = CurrentChapter is > GameChapter.SearchForOppy and < GameChapter.Ending;

            // note: this logic only executes after Oppy enters reality
            // before that, the experience is scripted, so balls shouldn't spawn so randomly
            if (flashlightActive && noHiddenBall && m_oppyDiscoveryCount >= 2 && validMode)
            {
                m_ballSpawnTimer -= Time.deltaTime;
                if (m_ballSpawnTimer <= 0.0f)
                {
                    m_shouldSpawnBall = true;
                    m_ballSpawnTimer = Random.Range(SPAWN_TIME_MIN, SPAWN_TIME_MAX);
                }
            }

            if (m_shouldSpawnBall)
            {
                SpawnHiddenBall();
                m_shouldSpawnBall = false;
            }


            var roomSparkleRingVisible = CurrentChapter >= GameChapter.OppyExploresReality && m_hiddenBallCollectable;
            roomSparkleRingVisible |= CurrentChapter == GameChapter.SearchForOppy && (Pet.gameObject.activeSelf || (m_hiddenBallCollectable && !m_hiddenBallCollectable.WasShot));

            var ripplePosition = m_hiddenBallCollectable ? m_hiddenBallPosition : Vector3.one * -1000.0f;
            if (CurrentChapter == GameChapter.SearchForOppy)
            {
                ripplePosition = Pet.gameObject.activeSelf ? Pet.transform.position : ripplePosition;
            }
            var effectSpeed = Time.deltaTime * 2.0f;
            m_vrRoomEffectTimer += roomSparkleRingVisible ? effectSpeed : -effectSpeed;

            // to make balls easier to find, display a ripple effect on Passthrough
            var showRippleMask = m_hiddenBallCollectable != null && flashlightActive && m_hiddenBallCollectable.BallState == BallCollectable.BallStatus.Hidden;
            m_vrRoomEffectMaskTimer += showRippleMask ? effectSpeed : -effectSpeed;
            if (m_vrRoomEffectTimer >= 0.0f || m_vrRoomEffectMaskTimer >= 0.0f)
            {
                VirtualRoom.Instance.SetWallEffectParams(ripplePosition, Mathf.Clamp01(m_vrRoomEffectTimer), m_vrRoomEffectMaskTimer);
            }
            m_vrRoomEffectTimer = Mathf.Clamp01(m_vrRoomEffectTimer);
            m_vrRoomEffectMaskTimer = Mathf.Clamp01(m_vrRoomEffectMaskTimer);
            if (UsingHands)
            {
                HideInvisibleHandAccessories();
            }
        }

        /// <summary>
        /// Calculates whether each hand should be visible or not
        /// </summary>
        private void HideInvisibleHandAccessories()
        {
            var leftHandHidden = !LeftHand.IsDataValid || LeftHandVisual.ForceOffVisibility;
            var rightHandHidden = !RightHand.IsDataValid || RightHandVisual.ForceOffVisibility;
            var grabbedBall = MultiToy.Instance.GrabbedBall;

            // Called before updating distance so that the hidden property is set while the ball is close to the hand
            UpdateHandVisibility(leftHandHidden, InteractionLineLeft, m_leftHandGrabbedBallLastDistance, GameController == OVRInput.Controller.LHand, grabbedBall);
            UpdateHandVisibility(rightHandHidden, InteractionLineRight, m_rightHandGrabbedBallLastDistance, GameController == OVRInput.Controller.RHand, grabbedBall);

            // Hidden hands have a Position of 0, only update if the hand is visible.
            if (!leftHandHidden) m_leftHandGrabbedBallLastDistance = grabbedBall ? Vector3.Distance(LeftHandAnchor.position, grabbedBall.transform.position) : Mathf.Infinity;
            if (!rightHandHidden) m_rightHandGrabbedBallLastDistance = grabbedBall ? Vector3.Distance(RightHandAnchor.position, grabbedBall.transform.position) : Mathf.Infinity;

            if (!UsingHands)
            {
                InteractionLineLeft.enabled = false;
                InteractionLineRight.enabled = false;
            }
        }

        /// <summary>
        /// Hides the ball, reticule and tutorial if the hand is not tracked anymore.
        /// Using previousHandToBallDistance to determine whether the current hand is holding the ball
        /// </summary>
        private void UpdateHandVisibility(bool handHidden, DistantInteractionLineVisual interactionLine, float previousHandToBallDistance, bool primary, [CanBeNull] BallCollectable grabbedBall)
        {
            var holdingBall = grabbedBall != null && previousHandToBallDistance < 0.2f;
            if (handHidden)
            {
                interactionLine.gameObject.SetActive(false);
                if (primary) WorldBeyondTutorial.Instance.ForceInvisible();
                if (holdingBall) grabbedBall.ForceInvisible();
            }
            else
            {
                interactionLine.gameObject.SetActive(UsingHands && MultiToy.Instance.GetCurrentToy() == MultiToy.ToyOption.Flashlight);
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
            CurrentChapter = forcedChapter;
            WorldBeyondEnvironment.Instance.ShowEnvironment((int)CurrentChapter > (int)GameChapter.SearchForOppy);

            if ((int)CurrentChapter < (int)GameChapter.SearchForOppy) MainCamera.backgroundColor = m_cameraDark;

            Pet.gameObject.SetActive((int)CurrentChapter >= (int)GameChapter.OppyExploresReality);
            Pet.SetOppyChapter(CurrentChapter);
            Pet.PlaySparkles(false);

            if (LightBeam) { LightBeam.gameObject.SetActive(false); }
            if (m_titleScreen) m_titleScreen.SetActive(false);
            if (m_endScreen) m_endScreen.SetActive(false);

            switch (CurrentChapter)
            {
                case GameChapter.Title:
                    AudioManager.SetSnapshot_Title();
                    MusicManager.Instance.PlayMusic(MusicManager.Instance.IntroMusic);
                    _ = StartCoroutine(ShowTitleScreen());
                    VirtualRoom.Instance.ShowAllWalls(false);
                    VirtualRoom.Instance.HideEffectMesh();
                    WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.None);
                    WorldBeyondEnvironment.Instance.Sun.enabled = false;
                    break;
                case GameChapter.Introduction:
                    AudioManager.SetSnapshot_Introduction();
                    VirtualRoom.Instance.ShowDarkRoom(true);
                    VirtualRoom.Instance.AnimateEffectMesh();
                    _ = StartCoroutine(PlayIntroPassthrough());
                    break;
                case GameChapter.OppyBaitsYou:
                    m_passthroughStylist.ResetPassthrough(0.1f);
                    _ = StartCoroutine(PlaceToyRandomly(2.0f));
                    break;
                case GameChapter.SearchForOppy:
                    VirtualRoom.Instance.HideEffectMesh();
                    VirtualRoom.Instance.EffectMeshForFloorCeiling.HideMesh = false;
                    OppyDiscovered = false;
                    m_oppyDiscoveryCount = 0;
                    BallCount = STARTING_BALL_COUNT;
                    m_passthroughStylist.ResetPassthrough(0.1f);
                    WorldBeyondEnvironment.Instance.Sun.enabled = true;
                    _ = StartCoroutine(CountdownToFlashlight(5.0f));
                    _ = StartCoroutine(FlickerCameraToClearColor());
                    break;
                case GameChapter.OppyExploresReality:
                    AudioManager.SetSnapshot_OppyExploresReality();
                    m_passthroughStylist.ResetPassthrough(0.1f);
                    VirtualRoom.Instance.ShowAllWalls(true);
                    VirtualRoom.Instance.SetRoomSaturation(1.0f);
                    _ = StartCoroutine(UnlockBallShooter(UsingHands ? 0f : 5.0f));
                    _ = StartCoroutine(UnlockWallToy(UsingHands ? 5f : 20.0f));
                    SpaceShipAnimator.StartIdleSound(); // Start idle sound here - mix will mute it.
                    break;
                case GameChapter.TheGreatBeyond:
                    AudioManager.SetSnapshot_TheGreatBeyond();
                    m_passthroughStylist.ResetPassthrough(0.1f);
                    VirtualRoom.Instance.EffectMeshForFloorCeiling.HideMesh = true;
                    SetEnvironmentSaturation(IsGreyPassthrough() ? 0.0f : 1.0f);
                    if (IsGreyPassthrough()) _ = StartCoroutine(SaturateEnvironmentColor());
                    MusicManager.Instance.PlayMusic(MusicManager.Instance.PortalOpen);
                    MusicManager.Instance.PlayMusic(MusicManager.Instance.TheGreatBeyondMusic);
                    break;
                default:
                    break;
            }
            Debug.Log("TheWorldBeyond: started chapter " + CurrentChapter);
        }

        /// <summary>
        /// After the title screen fades to black, start the transition from black to darkened-Passthrough.
        /// After that, trigger the next chapter that shows the light beam.
        /// </summary>
        private IEnumerator PlayIntroPassthrough()
        {
            VirtualRoom.Instance.EffectMeshForFloorCeiling.HideMesh = true;

            m_backgroundFadeSphere.SetActive(false);
            // first, make everything black
            var darkPassthroughStyle = new PassthroughStylist.PassthroughStyle(
                new Color(0, 0, 0, 0),
                1.0f,
                0.0f,
                0.0f,
                0.0f,
                true,
                Color.black,
                Color.black,
                Color.black);
            m_passthroughStylist.ForcePassthroughStyle(darkPassthroughStyle);

            // fade in edges
            var timer = 0.0f;
            var lerpTime = 4.0f;
            while (timer <= lerpTime)
            {
                timer += Time.deltaTime;

                var edgeColor = Color.white;
                edgeColor.a = Mathf.Clamp01(timer / 3.0f); // fade from transparent
                m_passthroughLayer.edgeColor = edgeColor;

                var normTime = Mathf.Clamp01(timer / lerpTime);
                FadeSphere.sharedMaterial.SetColor("_Color", Color.Lerp(Color.black, Color.clear, normTime));

                VirtualRoom.Instance.SetEdgeEffectIntensity(normTime);

                // once lerpTime is over, fade in normal passthrough
                if (timer >= lerpTime)
                {
                    var normalPassthrough = new PassthroughStylist.PassthroughStyle(
                        new Color(0, 0, 0, 0),
                        1.0f,
                        0.0f,
                        0.0f,
                        0.0f,
                        false,
                        Color.white,
                        Color.black,
                        Color.white);
                    m_passthroughStylist.ShowStylizedPassthrough(normalPassthrough, 5.0f);
                    FadeSphere.gameObject.SetActive(false);
                }
                yield return null;
            }

            yield return new WaitForSeconds(3.0f);
            ForceChapter(GameChapter.OppyBaitsYou);
        }

        /// <summary>
        /// When you first grab the MultiToy, the world flashes for a split second.
        /// </summary>
        private IEnumerator FlickerCameraToClearColor()
        {
            var timer = 0.0f;
            var flickerTimer = 0.5f;
            while (timer <= flickerTimer)
            {
                timer += Time.deltaTime;
                var normTimer = Mathf.Clamp01(0.5f * timer / flickerTimer);
                MainCamera.backgroundColor = Color.Lerp(Color.black, m_cameraDark, MultiToy.Instance.EvaluateFlickerCurve(normTimer));
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
        private IEnumerator ShowTitleScreen()
        {
            FadeSphere.gameObject.SetActive(true);
            m_backgroundFadeSphere.gameObject.SetActive(true);

            var darkPassthroughStyle = new PassthroughStylist.PassthroughStyle(
                   new Color(0, 0, 0, 0),
                   1.0f,
                   0.0f,
                   0.0f,
                   0.0f,
                   true,
                   Color.black,
                   Color.black,
                   Color.black);
            m_passthroughStylist.ForcePassthroughStyle(darkPassthroughStyle);

            FadeSphere.sharedMaterial.SetColor("_Color", Color.black);
            FadeSphere.sharedMaterial.renderQueue = 4999;

            m_backgroundFadeSphere.GetComponent<MeshRenderer>().material.renderQueue = 1997;
            m_backgroundFadeSphere.GetComponent<MeshRenderer>().material.SetColor("_Color", Color.black);

            m_titleScreen.SetActive(true);
            PositionTitleScreens(true);

            // fade/animate title text
            var timer = 0.0f;
            var lerpTime = 8.0f;
            while (timer <= lerpTime)
            {
                timer += Time.deltaTime;

                var normTimer = Mathf.Clamp01(timer / lerpTime);

                // fade black above everything
                var blackFade = Mathf.Clamp01(normTimer * 5) * Mathf.Clamp01((1 - normTimer) * 5);
                FadeSphere.sharedMaterial.SetColor("_Color", Color.Lerp(Color.black, Color.clear, blackFade));

                // once lerpTime is over, fade in normal passthrough
                if (timer >= lerpTime)
                {
                    m_titleScreen.SetActive(false);
                }
                yield return null;
            }
            ForceChapter(GameChapter.Introduction);
        }

        /// <summary>
        /// When Oppy first enters reality, prepare to unlock the ball shooter.
        /// </summary>
        private IEnumerator UnlockBallShooter(float countdown)
        {
            yield return new WaitForSeconds(countdown);
            // ensures the flashlight works again, once it's switched back to
            MultiToy.Instance.SetFlickerTime(0.0f);

            MultiToy.Instance.UnlockBallShooter();
            OVRInput.SetControllerVibration(1, 1, GameController);
            yield return new WaitForSeconds(1.0f);
            KillControllerVibration();
        }

        /// <summary>
        /// After a few seconds of playing with Oppy, unlock the wall toggler toy.
        /// </summary>
        private IEnumerator UnlockWallToy(float countdown)
        {
            yield return new WaitForSeconds(countdown);
            MultiToy.Instance.UnlockWallToy();
            OVRInput.SetControllerVibration(1, 1, GameController);
            yield return new WaitForSeconds(1.0f);
            KillControllerVibration();
        }

        /// <summary>
        /// Prepare the toy and light beam for their initial appearance.
        /// </summary>
        private IEnumerator PlaceToyRandomly(float spawnTime)
        {
            yield return new WaitForSeconds(spawnTime);
            MultiToy.Instance.ShowToy(true);
            MultiToy.Instance.SetToyMesh(MultiToy.ToyOption.Flashlight);
            m_toyBasePosition = GetRandomToyPosition();
            LightBeam.gameObject.SetActive(true);
            LightBeam.transform.localScale = new Vector3(1, VirtualRoom.Instance.GetCeilingHeight(), 1);
            LightBeam.Prepare(m_toyBasePosition);
        }

        /// <summary>
        /// Right after player grabs Multitoy, wait a few seconds before turning on the flashlight.
        /// </summary>
        private IEnumerator CountdownToFlashlight(float spawnTime)
        {
            yield return new WaitForSeconds(spawnTime - 0.5f);
            OVRInput.SetControllerVibration(1, 1, GameController);
            MultiToy.Instance.EnableFlashlightCone(true);
            if (UsingHands)
            {
                WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.EnableFlashlight);
            }
            MultiToy.Instance.FlashlightFlicker_1.Play();
            var timer = 0.0f;
            var lerpTime = 0.5f;
            while (timer <= lerpTime)
            {
                timer += Time.deltaTime;
                MultiToy.Instance.SetFlickerTime(0.5f * timer / lerpTime + 0.5f);
                if (timer >= lerpTime)
                {
                    MultiToy.Instance.SetFlickerTime(1.0f);
                }
                yield return null;
            }
            KillControllerVibration();
            _ = StartCoroutine(SpawnPetRandomly(true, Random.Range(SPAWN_TIME_MIN, SPAWN_TIME_MAX)));
        }

        /// <summary>
        /// Called from OVRSceneManager.SceneModelLoadedSuccessfully().
        /// This only sets a flag, and the game behavior begins in Update().
        /// This is because OVRSceneManager does all the heavy lifting, and this experience requires it to be complete.
        /// </summary>
        private void SceneModelLoaded()
        {
            m_sceneModelLoaded = true;
        }

        /// <summary>
        /// When the Scene has loaded, instantiate all the wall and furniture items.
        /// OVRSceneManager creates proxy anchors, that we use as parent tranforms for these instantiated items.
        /// </summary>
        private void GetRoomFromScene()
        {
            if (m_frameWait < 1)
            {
                m_frameWait++;
                return;
            }
            VrRoom.InitializeMRUK();

            // even though loading has succeeded to this point, do some sanity checks
            if (!VrRoom.IsPlayerInRoom())
            {
                WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.ERROR_USER_STARTED_OUTSIDE_OF_ROOM);
            }
            WorldBeyondEnvironment.Instance.Initialize();
            ForceChapter(GameChapter.Title);
        }

        /// <summary>
        /// When the flashlight shines on an energy ball, advance the story and handle the UI message.
        /// </summary>
        public void DiscoveredBall(BallCollectable collected)
        {
            if (!UsingHands) // Balls are not absorbed, just picked up when using hands
            {
                BallCount++;
            }
            WorldBeyondTutorial.Instance.HideMessage(WorldBeyondTutorial.TutorialMessage.BallSearch);
            WorldBeyondTutorial.Instance.HideMessage(WorldBeyondTutorial.TutorialMessage.NoBalls);

            // when using hands, make sure the discovered ball was actually hidden
            // otherwise, grabbing any ball will advance the script in undesirable ways
            if (UsingHands && collected.WasShot)
            {
                return;
            }
            if (CurrentChapter == GameChapter.SearchForOppy)
            {
                // in case we already picked up a ball and triggered the coroutine, cancel the old one
                // this is only a problem when using hands, since the balls stay around
                if (UsingHands)
                {
                    StopAllCoroutines();
                }
                _ = StartCoroutine(SpawnPetRandomly(false, Random.Range(SPAWN_TIME_MIN, SPAWN_TIME_MAX)));
            }
        }

        /// <summary>
        /// Get the closest ball to Oppy that's available to be eaten. (some are intentionally unavailable, like hidden ones)
        /// </summary>
        public BallCollectable GetClosestEdibleBall(Vector3 petPosition)
        {
            var closestDist = 20.0f;
            BallCollectable closestBall = null;
            foreach (Transform bcXform in m_ballContainer)
            {
                var bc = bcXform.GetComponent<BallCollectable>();
                if (!bc)
                {
                    continue;
                }
                var thisDist = Vector3.Distance(petPosition, bc.gameObject.transform.position);
                if (thisDist < closestDist
                    && bc.BallState == BallCollectable.BallStatus.Released
                    && bc.ShotTimer >= 1.0f)
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
            var closestAngle = 0.9f;

            BallCollectable closestBall = null;
            for (var i = 0; i < m_ballContainer.childCount; i++)
            {
                var bc = m_ballContainer.GetChild(i).GetComponent<BallCollectable>();
                if (!bc)
                {
                    continue;
                }
                var rayFromHand = (bc.gameObject.transform.position - toyPos).normalized;
                var thisViewAngle = Vector3.Dot(rayFromHand, toyFwd);
                if (thisViewAngle > closestAngle)
                {
                    if (bc.BallState is BallCollectable.BallStatus.Available or
                        BallCollectable.BallStatus.Hidden or
                        BallCollectable.BallStatus.Released)
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
        private IEnumerator MalfunctionFlashlight()
        {
            yield return new WaitForSeconds(2.0f);

            var effectRing = Instantiate(WorldShockwave);
            effectRing.transform.position = Pet.transform.position;
            effectRing.GetComponent<ParticleSystem>().Play();

            Pet.EnablePassthroughShell(true);

            var weirdPassthrough = new PassthroughStylist.PassthroughStyle(
                        new Color(0, 0, 0, 0),
                        1.0f,
                        0.0f,
                        0.0f,
                        0.8f,
                        true,
                        new Color(0, 0.5f, 1, 0.5f),
                        Color.black,
                        Color.white);
            m_passthroughStylist.ShowStylizedPassthrough(weirdPassthrough, 0.2f);

            WorldBeyondEnvironment.Instance.FlickerSun();

            // flicker out
            MultiToy.Instance.FlashlightFlicker_1.Play();
            var timer = 0.0f;
            var lerpTime = 0.3f;
            while (timer <= lerpTime)
            {
                timer += Time.deltaTime;
                MultiToy.Instance.SetFlickerTime(0.5f * timer / lerpTime);
                if (timer >= lerpTime)
                {
                    MultiToy.Instance.SetFlickerTime(0.5f);
                    MultiToy.Instance.StopLoopingSound();
                    MultiToy.Instance.Malfunction_1.Play();
                    m_passthroughStylist.ResetPassthrough(0.15f);
                }
                yield return null;
            }

            ForceChapter(GameChapter.OppyExploresReality);
            Pet.EndLookTarget();
        }

        /// <summary>
        /// After shining the flashlight on Oppy the first two times, it flickers so she can disappear.
        /// </summary>
        private IEnumerator FlickerFlashlight(float delayTime = 0.0f)
        {
            yield return new WaitForSeconds(delayTime);
            // flicker out
            MultiToy.Instance.FlashlightFlicker_1.Play();
            MultiToy.Instance.FlashlightLoop_1.Pause();
            var timer = 0.0f;
            var lerpTime = 0.2f;
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
            if (m_oppyDiscoveryCount == 1)
            {
                Pet.PlayTeleport();
            }

            // hide Oppy
            OppyDiscovered = false;
            m_oppyDiscoveryCount++;
            Pet.ResetAnimFlags();
            var colorSaturation = IsGreyPassthrough() ? Mathf.Clamp01(m_oppyDiscoveryCount / 3.0f) : 1.0f;
            Pet.SetMaterialSaturation(colorSaturation);
            Pet.StartLookTarget();
            Pet.gameObject.SetActive(false);

            // increase room saturation while flashlight is off
            VirtualRoom.Instance.SetRoomSaturation(colorSaturation);

            // flicker in
            timer = 0.0f;
            while (timer <= lerpTime)
            {
                timer += Time.deltaTime;
                MultiToy.Instance.SetFlickerTime(0.5f * timer / lerpTime + 0.5f);
                if (timer >= lerpTime)
                {
                    MultiToy.Instance.SetFlickerTime(1.0f);
                }
                yield return null;
            }
            MultiToy.Instance.FlashlightLoop_1.Resume();

            if (m_oppyDiscoveryCount == 1)
            {
                WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.BallSearch);
                m_hiddenBallCollectable.SetState(BallCollectable.BallStatus.Hidden);
            }
            else
            {
                // spawn ball only after the first discovery (first ball already exists)
                yield return new WaitForSeconds(Random.Range(SPAWN_TIME_MIN, SPAWN_TIME_MAX));
                SpawnHiddenBall();
            }
        }

        /// <summary>
        /// During the discovery chapter, Oppy is place in hidden locations in the space.
        /// </summary>
        private IEnumerator SpawnPetRandomly(bool firstTimeSpawning, float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            // because the m_pet has a Navigation component, we must turn off the object before manually moving
            // this is to avoid the m_pet getting "stuck" on the walls
            Pet.gameObject.SetActive(false);
            var spawnPos = GetRandomPetPosition();
            var fwd = MainCamera.transform.position - spawnPos;
            var oppyRotation = Quaternion.LookRotation(new Vector3(fwd.x, 0, fwd.z));
            Pet.transform.rotation = oppyRotation;
            Pet.transform.position = spawnPos - Pet.transform.right * 0.1f;
            Pet.gameObject.SetActive(true);
            Pet.PlaySparkles(true);
            Pet.SetLookDirection(fwd.normalized);
            if (firstTimeSpawning)
            {
                Pet.PrepareInitialDiscoveryAnim();
                Pet.SetMaterialSaturation(IsGreyPassthrough() ? 0.0f : 1.0f);
                var hiddenBall = Instantiate(BallPrefab, spawnPos + Pet.transform.right * 0.05f + Vector3.up * 0.06f, quaternion.identity);
                m_hiddenBallPosition = hiddenBall.transform.position;
                m_hiddenBallCollectable = hiddenBall.GetComponent<BallCollectable>();
                m_hiddenBallCollectable.SetState(BallCollectable.BallStatus.Unavailable);
                m_hiddenBallCollectable.PlaceHiddenBall(hiddenBall.transform.position, -1);
            }
        }

        /// <summary>
        /// When the player has the flashlight active, there should always be a hidden ball to discover.
        /// </summary>
        private void SpawnHiddenBall()
        {
            // if spawning on a wall, track the id:
            // if the wall is toggled off, we need to destroy the ball
            var wallID = -1;
            var hiddenBall = Instantiate(BallPrefab, GetRandomBallPosition(), quaternion.identity);
            m_hiddenBallPosition = hiddenBall.transform.position;
            m_hiddenBallCollectable = hiddenBall.GetComponent<BallCollectable>();
            m_hiddenBallCollectable.PlaceHiddenBall(m_hiddenBallPosition, wallID);
        }

        /// <summary>
        /// Any time the player "opens" a wall with the wall toggler, some special behavior needs to happen:
        /// 1. Any "hidden" ball on that wall needs to be destroyed, otherwise there'd be a weird float passthrough ball.
        /// 2. If it's the first time, advance the story.
        /// </summary>
        public void OpenedWall(int wallID)
        {
            foreach (Transform child in m_ballContainer)
            {
                if (child.GetComponent<BallCollectable>())
                {
                    if (child.GetComponent<BallCollectable>().WallID == wallID)
                    {
                        RemoveBallFromWorld(child.GetComponent<BallCollectable>());
                    }
                }
            }

            if (CurrentChapter == GameChapter.OppyExploresReality)
            {
                WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.None);
                ForceChapter(GameChapter.TheGreatBeyond);
            }
        }

        /// <summary>
        /// Self-explanatory.
        /// </summary>
        private void KillControllerVibration()
        {
            OVRInput.SetControllerVibration(1, 0, GameController);
        }

        /// <summary>
        /// Helper to find a random position on the floor surface with retry and fallback.
        /// </summary>
        /// <param name=\"maxAttempts\">Maximum retry attempts.</param>
        /// <param name=\"surfaceOffset\">Vertical offset to apply to the position.</param>
        /// <param name=\"sceneVolumePadding\">Padding for scene volume check.</param>
        /// <param name=\"attempt\">Current attempt count for recursion.</param>
        /// <returns>Valid position or fallback position.</returns>
        private Vector3 GetRandomPositionOnFloor(
            int maxAttempts = 10,
            float surfaceOffset = 0f,
            float sceneVolumePadding = 0.1f,
            int attempt = 0)
        {
            var room = MRUK.Instance.GetCurrentRoom();
            MRUK.Instance.GetCurrentRoom().GenerateRandomPositionOnSurface(
                MRUK.SurfaceType.FACING_UP, 0.1f,
                new LabelFilter(MRUKAnchor.SceneLabels.FLOOR), out var pos, out var rot);
            if (room.IsPositionInRoom(pos) && !room.IsPositionInSceneVolume(pos, sceneVolumePadding))
            {
                return new Vector3(pos.x, pos.y + surfaceOffset, pos.z);
            }
            if (attempt < maxAttempts - 1)
            {
                return GetRandomPositionOnFloor(maxAttempts, surfaceOffset, sceneVolumePadding, attempt + 1);
            }
            // Fallback after max attempts
            return new Vector3(MainCamera.transform.position.x, GetFloorHeight(), MainCamera.transform.position.z);
        }
        /// <summary>
        /// Find a Position in the room to place Oppy.
        /// </summary>
        public Vector3 GetRandomPetPosition(int attempt = 0)
        {
            return GetRandomPositionOnFloor(maxAttempts: 10, surfaceOffset: 0f, sceneVolumePadding: 0.5f, attempt);
        }
        /// <summary>
        /// Find a room surface upon which to spawn a hidden ball.
        /// </summary>
        public Vector3 GetRandomBallPosition(int attempt = 0)
        {
            var pos = GetRandomPositionOnFloor(maxAttempts: 10, surfaceOffset: 0f, sceneVolumePadding: 0.1f, attempt);
            return pos;
        }
        /// <summary>
        /// Find a clear space on the floor to place the light beam/Multitoy.
        /// </summary>
        public Vector3 GetRandomToyPosition(int attempt = 0)
        {
            return GetRandomPositionOnFloor(maxAttempts: 10, surfaceOffset: 1f, sceneVolumePadding: 0.1f, attempt);
        }

        /// <summary>
        /// Raycasts every 45 degrees from point towards walls, move the point away from the wall if it's too close
        /// </summary>
        private Vector3 MovePointAwayFromWalls(Vector3 pos, Bounds bounds)
        {
            var point = pos;
            var direction = Vector3.right;
            LayerMask ballSpawnLayer = LayerMask.GetMask("RoomBox", "Furniture");
            var attempt = 0;
            for (var i = 0; i < 8; i++)
            {
                if (Physics.Raycast(point, direction, out _, 0.5f, ballSpawnLayer))
                {
                    var safePos = point - direction * 0.5f;
                    if (MRUK.Instance.GetCurrentRoom().IsPositionInRoom(safePos) && (bounds.size.Equals(Vector3.zero) || bounds.Contains(safePos)))
                    {
                        point = safePos;
                    }
                    // reset count so new Position gets checked too
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
            _ = StartCoroutine(DoEndingSequence());
        }

        /// <summary>
        /// End game sequence and cleanup: fade to black, trigger the UFO animation, reset the game.
        /// </summary>
        private IEnumerator DoEndingSequence()
        {
            AudioManager.SetSnapshot_Ending();
            FadeSphere.gameObject.SetActive(true);
            FadeSphere.sharedMaterial.SetColor("_Color", Color.clear);
            if (SpaceShipAnimator)
            {
                SpaceShipAnimator.TriggerAnim();
                var flyingAwayTime = 15.5f;
                var timer = 0.0f;
                while (timer < flyingAwayTime)
                {
                    timer += Time.deltaTime;
                    var fadeValue = timer / flyingAwayTime;
                    fadeValue = Mathf.Clamp01((fadeValue - 0.9f) * 10.0f);
                    FadeSphere.sharedMaterial.SetColor("_Color", Color.Lerp(Color.clear, Color.white, fadeValue));
                    WorldBeyondEnvironment.Instance.FadeOutdoorAudio(1 - fadeValue);
                    if (timer >= flyingAwayTime)
                    {
                        WorldBeyondEnvironment.Instance.SetOutdoorAudioParams(Vector3.zero, false);
                        m_endScreen.SetActive(true);
                        PositionTitleScreens(true);
                        FadeSphere.sharedMaterial.SetColor("_Color", Color.white);
                        MultiToy.Instance.EndGame();
                        DestroyAllBalls();
                        SpaceShipAnimator.ResetAnim();
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
            if (!OppyDiscovered)
            {
                OppyDiscovered = true;
                // the final discovery, after which Oppy enters reality
                if (m_oppyDiscoveryCount == 2)
                {
                    Pet.BoneSim.gameObject.SetActive(true);
                    Pet.PlayRandomOppyDiscoveryAnim();
                    _ = StartCoroutine(MalfunctionFlashlight());
                }
                // first discovery, play the unique discovery anim
                else if (m_oppyDiscoveryCount == 0)
                {
                    Pet.PlayInitialDiscoveryAnim();
                    _ = StartCoroutine(FlickerFlashlight(4.0f));
                }
                // second discovery, play a random "delight" anim
                else
                {
                    Pet.PlayRandomOppyDiscoveryAnim();
                    _ = StartCoroutine(FlickerFlashlight(2.0f));
                }
            }
        }

        /// <summary>
        /// Dolly/rotate the title and end screens
        /// </summary>
        private void PositionTitleScreens(bool firstFrame)
        {
            m_titleFadeTimer += Time.deltaTime;
            if (firstFrame)
            {
                m_titleFadeTimer = 0.0f;
            }

            var camFwd = new Vector3(MainCamera.transform.forward.x, 0, MainCamera.transform.forward.z).normalized;
            var currentLook = (m_titleScreen.transform.position - MainCamera.transform.position).normalized;
            const float POS_LERP = 0.95f;
            var targetLook = firstFrame ? camFwd : Vector3.Slerp(camFwd, currentLook, POS_LERP);
            var pitch = Vector3.Lerp(Vector3.down * 0.05f, Vector3.up * 0.05f, Mathf.Clamp01(m_titleFadeTimer / 8.0f));
            var targetRotation = Quaternion.LookRotation(-targetLook + pitch, Vector3.up);

            var dollyDirection = CurrentChapter == GameChapter.Title ? -1.0f : 1.0f;
            var textDistance = (CurrentChapter == GameChapter.Title ? 5 : 4) + dollyDirection * m_titleFadeTimer * 0.1f;

            m_titleScreen.transform.position = MainCamera.transform.position + targetLook * textDistance;
            m_titleScreen.transform.rotation = targetRotation;

            m_endScreen.transform.position = m_titleScreen.transform.position;
            m_endScreen.transform.rotation = m_titleScreen.transform.rotation;

            // hardcoded according to the fade out time of 13 seconds
            // fade in for 2 seconds, fade out after 8 seconds
            var endFade = Mathf.Clamp01(m_titleFadeTimer * 0.5f) * (1.0f - Mathf.Clamp01((m_titleFadeTimer - 8) * 0.25f));
            m_endScreen.GetComponent<MeshRenderer>().sharedMaterial.SetColor("_Color", Color.Lerp(Color.black, Color.white, endFade));
        }

        /// <summary>
        /// Adjust the desaturation range of the environment shaders.
        /// </summary>
        private void SetEnvironmentSaturation(float normSat)
        {
            // convert a normalized value to what the shader intakes
            var actualSat = Mathf.Lerp(1.0f, 0.08f, normSat);
            foreach (var mtl in EnvironmentMaterials)
            {
                mtl.SetFloat("_SaturationDistance", actualSat);
            }
        }

        /// <summary>
        /// When a passthrough wall is first opened, the virtual environment appears greyscale to match Passthrough.
        /// Over a few seconds, the desaturation range shrinks.
        /// </summary>
        private IEnumerator SaturateEnvironmentColor()
        {
            yield return new WaitForSeconds(4.0f);
            var timer = 0.0f;
            var lerpTime = 4.0f;
            while (timer <= lerpTime)
            {
                timer += Time.deltaTime;
                var normTime = IsGreyPassthrough() ? Mathf.Clamp01(timer / lerpTime) : 1.0f;
                SetEnvironmentSaturation(normTime);
                yield return null;
            }
        }

        /// <summary>
        /// Track the balls in the game so they can be safely managed.
        /// </summary>
        public void AddBallToWorld(BallCollectable newBall)
        {
            newBall.gameObject.transform.SetParent(m_ballContainer, true);
        }

        /// <summary>
        /// When debris is created from a ball collision, track and delete the old pieces so we don't overflow.
        /// </summary>
        public void AddBallDebrisToWorld(GameObject newDebris)
        {
            m_ballDebrisObjects.Add(newDebris.GetComponent<BallDebris>());
        }

        /// <summary>
        /// Perform physics on the debris gems, from a Position.
        /// </summary>
        public void AffectDebris(Vector3 effectPosition, bool repel)
        {
            for (var i = 0; i < m_ballDebrisObjects.Count; i++)
            {
                if (m_ballDebrisObjects[i] != null)
                {
                    var forceDirection = m_ballDebrisObjects[i].transform.position - effectPosition;
                    if (repel)
                    {
                        if (forceDirection.magnitude < 0.5f)
                        {
                            var strength = 1.0f - Mathf.Clamp01(forceDirection.magnitude * 2);
                            m_ballDebrisObjects[i].AddForce(forceDirection.normalized, strength * 2);
                        }
                    }
                    else // absorb
                    {
                        var range = Vector3.Dot(MultiToy.Instance.GetFlashlightDirection(), -forceDirection.normalized);
                        if (range < -0.8f)
                        {
                            var strength = (-range - 0.8f) / 0.2f;
                            m_ballDebrisObjects[i].AddForce(-forceDirection.normalized, strength);
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
            _ = m_ballDebrisObjects.RemoveAll(item => item == null);
            // there's too much debris in the world, start removing some FIFO
            if (m_ballDebrisObjects.Count > MAX_BALL_DEBRIS)
            {
                var ballsToDestroy = m_ballDebrisObjects.Count - MAX_BALL_DEBRIS;
                for (var i = 0; i < ballsToDestroy; i++)
                {
                    // this shrinks the item before self-destructing
                    m_ballDebrisObjects[i].Kill();
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
        private void DestroyAllBalls()
        {
            foreach (Transform child in m_ballContainer)
            {
                if (child != null)
                {
                    Destroy(child.gameObject);
                }
            }
            if (m_hiddenBallCollectable)
            {
                Destroy(m_hiddenBallCollectable.gameObject);
            }
            // destroy debris also
            foreach (var child in m_ballDebrisObjects)
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
            if (UsingHands)
            {
                var lHand = GameController == OVRInput.Controller.LHand;
                var refHand = (GameController == OVRInput.Controller.LHand) ? LeftHand : RightHand;
                var refHandVisual = (GameController == OVRInput.Controller.LHand) ? LeftHandVisual : RightHandVisual;
                if (refHandVisual.ForceOffVisibility)
                {
                    return;
                }
                // if tuning these values, make your life easier by enabling the DebugAxis objects on the Multitoy prefab
                handPos = lHand ? LeftPointerOffset.transform.position : RightPointerOffset.transform.position;
                var handFwd = lHand ? LeftPointerOffset.transform.rotation * LeftPointerOffset.Rotation * Vector3.up : RightPointerOffset.transform.rotation * RightPointerOffset.Rotation * Vector3.up;
                var handRt = (refHand.Bones[12].Transform.position - refHand.Bones[6].Transform.position) * (lHand ? -1.0f : 1.0f);
                Vector3.OrthoNormalize(ref handFwd, ref handRt);
                var handUp = Vector3.Cross(handFwd, handRt);
                handRot = Quaternion.LookRotation(-handFwd, -handUp);
            }
            else
            {
                handPos = GameController == OVRInput.Controller.LTouch ? LeftHandAnchor.position : RightHandAnchor.position;
                handRot = GameController == OVRInput.Controller.LTouch ? LeftHandAnchor.rotation : RightHandAnchor.rotation;
            }
        }

        /// <summary>
        /// Simple 0-1 value to decide if the player has made a fist: if all fingers have "curled" enough.
        /// </summary>
        public void CalculateFistStrength()
        {
            var refHand = (GameController == OVRInput.Controller.LHand) ? LeftHand : RightHand;
            var refHandVisual = (GameController == OVRInput.Controller.LHand) ? LeftHandVisual : RightHandVisual;
            if (!UsingHands || refHandVisual.ForceOffVisibility)
            {
                FistValue = 1; // Hand is not visible, make it a fist to hide the flashlight and keep holding the held ball
                return;
            }
            var bone1 = (refHand.Bones[20].Transform.position - refHand.Bones[8].Transform.position).normalized;
            var bone2 = (refHand.Bones[21].Transform.position - refHand.Bones[11].Transform.position).normalized;
            var bone3 = (refHand.Bones[22].Transform.position - refHand.Bones[14].Transform.position).normalized;
            var bone4 = (refHand.Bones[23].Transform.position - refHand.Bones[18].Transform.position).normalized;
            var bone5 = (refHand.Bones[9].Transform.position - refHand.Bones[0].Transform.position).normalized;

            var avg = (bone1 + bone2 + bone3 + bone4) * 0.25f;
            FistValue = Vector3.Dot(-bone5, avg.normalized) * 0.5f + 0.5f;
        }

        /// <summary>
        /// Self-explanatory
        /// </summary>
        public OVRSkeleton GetActiveHand()
        {
            if (UsingHands)
            {
                var refHand = GameController == OVRInput.Controller.LHand ? LeftHand : RightHand;
                return refHand;
            }
            return null;
        }

        /// <summary>
        /// Get a transform for attaching the UI.
        /// </summary>
        public Transform GetControllingHand(int boneID)
        {
            var usingLeft = GameController is OVRInput.Controller.LTouch or OVRInput.Controller.LHand;
            var hand = usingLeft ? LeftHandAnchor : RightHandAnchor;
            if (UsingHands)
            {
                if (RightHand && LeftHand)
                {
                    // thumb tips, so menu is within view
                    if (boneID >= 0 && boneID < LeftHand.Bones.Count)
                    {
                        hand = usingLeft ? LeftHand.Bones[boneID].Transform : RightHand.Bones[boneID].Transform;
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
            // until things stabilize, m_force the EXPERIENCE to assume greyscale, but Passthrough itself is default to the device (line 124)
            // see this thread: https://fb.workplace.com/groups/272459344365710/permalink/479111297033846/
            return true;
        }

        /// <summary>
        /// Because of anchors, the ground floor may not be perfectly at y=0.
        /// </summary>
        public float GetFloorHeight()
        {
            return m_floorHeight;
        }

        /// <summary>
        /// The floor is generally at y=0, but in cases where the Scene floor anchor isn't, shift the whole world.
        /// </summary>
        public void MoveGroundFloor(float height)
        {
            m_floorHeight = height;
            WorldBeyondEnvironment.Instance.MoveGroundFloor(height);
        }
    }
}
