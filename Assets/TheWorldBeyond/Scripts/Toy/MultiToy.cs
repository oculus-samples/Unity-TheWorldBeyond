// Copyright (c) Meta Platforms, Inc. and affiliates.

using TheWorldBeyond.Audio;
using TheWorldBeyond.GameManagement;
using TheWorldBeyond.VFX;
using UnityEngine;

namespace TheWorldBeyond.Toy
{
    public class MultiToy : MonoBehaviour
    {
        public static MultiToy Instance = null;

        [Header("Toys")]
        public WorldBeyondToy[] Toys;
        private int m_toyIndexController = (int)ToyOption.None;
        private int m_toyIndexHand = (int)ToyOption.None;
        private bool m_canSwitchToys = false;
        private VirtualFlashlight m_toyFlashlight;
        private float m_flickerFlashlightStrength = 1.0f;
        private float m_ballTossCooldownFactor = 1.0f;
        private RoomFramer m_roomFramer;

        [Header("Meshes")]
        public GameObject AccessoryBase;
        public GameObject AccessoryFlashlight;
        public GameObject AccessoryBallGun;
        public GameObject AccessoryWallToy;

        public GameObject MeshParent;
        public bool ToyVisible { get; private set; }

        public Animator Animator;

        [Header("Sounds")]
        public AudioClip WallToyLoop;
        public AudioClip WallToyWallOpen;
        public AudioClip WallToyWallClose;

        public SoundEntry UnlockToy_1;
        public SoundEntry SwitchToy_1;
        public SoundEntry GrabToy_1;
        public SoundEntry Malfunction_1;
        public SoundEntry BallShoot_1;
        public SoundEntry FlashlightLoop_1;
        public SoundEntry FlashlightFlicker_1;
        public SoundEntry FlashlightAbsorb_1;
        public SoundEntry FlashlightAbsorbLoop_1;
        public SoundEntry WallToyLoop_1;
        public SoundEntry WallToyWallOpen_1;
        public SoundEntry WallToyWallClose_1;
        private AudioSource m_audioSource;

        [HideInInspector]
        public Transform WallToyTarget;
        private bool m_flashLightUnlocked = false;
        private bool m_wallToyUnlocked = false;
        private float m_pointingAtWallTimer = 0.0f; // ensure player reads the first instructions

        public enum ToyOption
        {
            None = -1,
            Flashlight = 0,
            BallGun = 1,
            WallToy = 2,
        };

        // only used by hands
        [HideInInspector]
        public BallCollectable GrabbedBall = null;
        private bool m_throwBallTaught = false;
        public MeshRenderer HandGlove;

        private void Start()
        {
            WorldBeyondManager.Instance.OnHandClosedDelegate += HandClosed;
        }

        /// <summary>
        /// Only used for triggering the wall toy.
        /// </summary>
        private void HandClosed()
        {
            if (WorldBeyondManager.Instance.CurrentChapter >= WorldBeyondManager.GameChapter.SearchForOppy)
            {
                if (m_toyIndexHand == (int)ToyOption.WallToy)
                {
                    Toys[m_toyIndexHand].ActionUp();
                }
            }
        }

        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
            }
            m_audioSource = GetComponent<AudioSource>();
            m_toyFlashlight = GetComponent<VirtualFlashlight>();
            m_roomFramer = GetComponent<RoomFramer>();
            ShowToy(false);
            ShowPassthroughGlove(false);
            // render after passthrough.
            var parts = MeshParent.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var part in parts)
            {
                foreach (var mat in part.materials)
                {
                    mat.renderQueue = 3000;
                }
            }
        }

        private void Update()
        {
            var currentChapter = WorldBeyondManager.Instance.CurrentChapter;

            var controllerPos = Vector3.zero;
            var controllerRot = Quaternion.identity;
            WorldBeyondManager.Instance.GetDominantHand(ref controllerPos, ref controllerRot);

            if (currentChapter != WorldBeyondManager.GameChapter.OppyBaitsYou)
            {
                transform.position = controllerPos;
                transform.rotation = controllerRot;
            }

            if (!TriggerHeld() && m_canSwitchToys)
            {
                var lastToy = m_toyIndexController;
                var highestToyID = (int)ToyOption.WallToy;
                if (CycleToy(false))
                {
                    m_toyIndexController = m_toyIndexController - 1 < 0 ? highestToyID : m_toyIndexController - 1;
                }
                if (CycleToy(true))
                {
                    m_toyIndexController = m_toyIndexController + 1 > highestToyID ? 0 : m_toyIndexController + 1;
                }
                if (lastToy != m_toyIndexController)
                {
                    Toys[lastToy].Deactivate();
                    Toys[m_toyIndexController].Activate();

                    SwitchToy_1.Play();
                    WorldBeyondTutorial.Instance.HideMessage(WorldBeyondTutorial.TutorialMessage.SwitchToy);

                    SetToyMesh((ToyOption)m_toyIndexController);
                }
            }

            if (currentChapter == WorldBeyondManager.GameChapter.SearchForOppy)
            {
                var oppyPos = WorldBeyondManager.Instance.Pet.transform.position + Vector3.up * 0.2f;
                var oppyDot = Vector3.Dot(GetFlashlightDirection(), (oppyPos - transform.position).normalized);
                var oppyActive = WorldBeyondManager.Instance.Pet.gameObject.activeSelf;
                if (!WorldBeyondManager.Instance.OppyDiscovered)
                {
                    if (oppyDot >= 0.95f && oppyActive)
                    {
                        WorldBeyondManager.Instance.PlayOppyDiscoveryAnim();
                        WorldBeyondManager.Instance.Pet.PlaySparkles(false);
                    }
                }
            }

            var currentToyIndex = m_toyIndexController;
            // switch toys, using hands
            if (WorldBeyondManager.Instance.UsingHands)
            {
                m_ballTossCooldownFactor += Time.deltaTime;
                currentToyIndex = m_toyIndexHand;
                if (currentChapter >= WorldBeyondManager.GameChapter.SearchForOppy)
                {
                    var mainCam = WorldBeyondManager.Instance.MainCamera.transform;
                    _ = Vector3.Dot(mainCam.forward, (controllerPos - mainCam.position).normalized) < 0.5f;
                    var lastToy = m_toyIndexHand;

                    // palm out: flashlight
                    var palmOut = Vector3.Dot(mainCam.forward, controllerRot * Vector3.forward);
                    var fistStrength = Mathf.Clamp01((WorldBeyondManager.Instance.FistValue - 0.3f) * 5.0f);
                    var flashL = Mathf.Clamp01(palmOut * 10) * (1 - fistStrength) * (GrabbedBall ? 0 : 1);

                    currentToyIndex = (flashL > 0.5f && m_flashLightUnlocked) ? (int)ToyOption.Flashlight : currentToyIndex;
                    m_toyFlashlight.SetLightStrength(flashL * m_flickerFlashlightStrength * Mathf.Clamp01(m_ballTossCooldownFactor - 1));

                    // palm up: prepare room framer
                    var palmUp = Vector3.Dot(mainCam.right, controllerRot * Vector3.right);
                    currentToyIndex = (palmUp < 0.0f && m_wallToyUnlocked && !GrabbedBall) ? (int)ToyOption.WallToy : currentToyIndex;

                    if (lastToy != currentToyIndex)
                    {
                        if (lastToy > (int)ToyOption.None)
                        {
                            Toys[lastToy].Deactivate();
                        }
                        if (currentToyIndex > (int)ToyOption.None)
                        {
                            if (currentToyIndex == (int)ToyOption.Flashlight)
                            {
                                WorldBeyondTutorial.Instance.HideMessage(WorldBeyondTutorial.TutorialMessage.EnableFlashlight);
                            }
                            Toys[currentToyIndex].Activate();
                        }
                    }

                    m_toyIndexHand = currentToyIndex;
                }
            }

            if (currentChapter >= WorldBeyondManager.GameChapter.SearchForOppy)
            {
                if (currentToyIndex > (int)ToyOption.None)
                {
                    if (TriggerPressed())
                    {
                        if (currentToyIndex == (int)ToyOption.BallGun && WorldBeyondManager.Instance.BallCount > 0)
                        {
                            Animator.SetTrigger("Shoot");
                        }

                        Toys[currentToyIndex].ActionDown();
                    }

                    if (TriggerReleased())
                    {
                        Toys[currentToyIndex].ActionUp();
                    }

                    if (TriggerHeld())
                    {
                        Toys[currentToyIndex].Action();
                    }
                }

                var tbc = WorldBeyondManager.Instance.GetTargetedBall(transform.position, transform.forward);
                if (tbc)
                {
                    if (tbc.BallState is BallCollectable.BallStatus.Available or
                        BallCollectable.BallStatus.Hidden or
                        BallCollectable.BallStatus.Released)
                    {
                        if (!WorldBeyondManager.Instance.UsingHands)
                        {
                            if (IsFlashlightAbsorbing())
                            {
                                tbc.Absorbing((tbc.transform.position - transform.position).normalized);
                                if (tbc.IsBallAbsorbed())
                                {
                                    tbc.AbsorbBall();
                                    WorldBeyondManager.Instance.DiscoveredBall(GrabbedBall);
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
            foreach (var toy in Toys)
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
                Toys[(int)ToyOption.Flashlight].Activate();
                m_flashLightUnlocked = true;
                if (!WorldBeyondManager.Instance.UsingHands)
                {
                    m_toyIndexController = (int)ToyOption.Flashlight;
                }
            }
            else
            {
                Toys[(int)ToyOption.Flashlight].Deactivate();
            }
            m_toyFlashlight.SetLightStrength(1.0f);
        }

        /// <summary>
        /// The direction the light cone is pointing; not always forward, due to the wobbly head.
        /// </summary>
        public Vector3 GetFlashlightDirection()
        {
            return m_toyFlashlight ? m_toyFlashlight.LightVolume.transform.up : Vector3.forward;
        }

        /// <summary>
        /// Assign the strength of the flashlight cone, based upon an evaluation of the curve set in the Inspector.
        /// </summary>
        public void SetFlickerTime(float normTime)
        {
            var flickerValue = 0.0f;
            if (m_toyFlashlight) flickerValue = EvaluateFlickerCurve(normTime);
            m_flickerFlashlightStrength = flickerValue;
            if (!WorldBeyondManager.Instance.UsingHands)
            {
                m_toyFlashlight.SetLightStrength(flickerValue);
            }
        }

        /// <summary>
        /// Sample the flicker curve at a normalized time.
        /// </summary>
        public float EvaluateFlickerCurve(float normTime)
        {
            return m_toyFlashlight.FlickerStrength.Evaluate(normTime);
        }

        /// <summary>
        /// Cleanly shut down the toys.
        /// </summary>
        public void DeactivateAllToys()
        {
            foreach (var toy in Toys)
            {
                toy.Deactivate();
            }
        }

        /// <summary>
        /// Show/hide the toy mesh.
        /// </summary>
        public void ShowToy(bool doShow)
        {
            MeshParent.SetActive(doShow);
            ToyVisible = doShow;
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
                    m_toyIndexController = (int)ToyOption.Flashlight;
                    m_toyIndexHand = (int)ToyOption.None;
                    m_canSwitchToys = false;
                    m_throwBallTaught = false;
                    m_wallToyUnlocked = false;
                    m_flashLightUnlocked = false;
                    m_ballTossCooldownFactor = 1.0f;
                    break;
                case WorldBeyondManager.GameChapter.OppyBaitsYou:
                    DeactivateAllToys();
                    ShowToy(false);
                    m_canSwitchToys = false;
                    m_flashLightUnlocked = false;
                    m_toyFlashlight.SetLightStrength(WorldBeyondManager.Instance.UsingHands ? 0.0f : 1.0f);
                    break;
                case WorldBeyondManager.GameChapter.SearchForOppy:
                    ShowToy(true);
                    m_canSwitchToys = false;
                    break;
                case WorldBeyondManager.GameChapter.OppyExploresReality:
                    DeactivateAllToys();
                    ShowToy(true);
                    m_toyIndexController = (int)ToyOption.None;
                    m_toyIndexHand = (int)ToyOption.None;
                    m_canSwitchToys = false;
                    m_wallToyUnlocked = false;
                    break;
                case WorldBeyondManager.GameChapter.TheGreatBeyond:
                    ShowToy(true);
                    m_canSwitchToys = true;
                    if (!WorldBeyondManager.Instance.UsingHands)
                    {
                        WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.SwitchToy);
                    }
                    break;
                case WorldBeyondManager.GameChapter.Ending:
                    m_canSwitchToys = true;
                    break;
            }
        }

        /// <summary>
        /// Activate the ball shooter component of the MultiToy.
        /// </summary>
        public void UnlockBallShooter()
        {
            if (!WorldBeyondManager.Instance.UsingHands)
            {
                //Ballcount can be 0 when using hands, and you are not able to change to pickup balls at this point when using controllers.
                if (WorldBeyondManager.Instance.BallCount == 0)
                {
                    // Give two free balls so that you don't get stuck
                    WorldBeyondManager.Instance.BallCount = 2;
                }
                UnlockToy_1.Play();
                WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.ShootBall);
                Toys[(int)ToyOption.Flashlight].Deactivate();
                Toys[(int)ToyOption.BallGun].Activate();
                m_toyIndexController = (int)ToyOption.BallGun;
                SetToyMesh(ToyOption.BallGun);
            }
            FlashlightLoop_1.Stop();
            FlashlightAbsorbLoop_1.Stop();
        }

        /// <summary>
        /// Activate the wall toggler component of the MultiToy.
        /// </summary>
        public void UnlockWallToy()
        {
            FlashlightLoop_1.Stop();
            FlashlightAbsorbLoop_1.Stop();
            UnlockToy_1.Play();
            if (!WorldBeyondManager.Instance.UsingHands)
            {
                WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.ShootWall);
                Toys[(int)ToyOption.BallGun].Deactivate();
                Toys[(int)ToyOption.WallToy].Activate();
                m_toyIndexController = (int)ToyOption.WallToy;
                SetToyMesh(ToyOption.WallToy);
            }
            else
            {
                WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.AimWall);
            }
            m_wallToyUnlocked = true;
        }

        /// <summary>
        /// Handler to abstract input.
        /// </summary>
        private bool TriggerPressed()
        {
            var triggerButton = WorldBeyondManager.Instance.GameController == OVRInput.Controller.RTouch ? OVRInput.RawButton.RIndexTrigger : OVRInput.RawButton.LIndexTrigger;
            return OVRInput.GetDown(triggerButton);
        }

        /// <summary>
        /// Handler to abstract input.
        /// </summary>
        private bool TriggerReleased()
        {
            var triggerButton = WorldBeyondManager.Instance.GameController == OVRInput.Controller.RTouch ? OVRInput.RawButton.RIndexTrigger : OVRInput.RawButton.LIndexTrigger;
            return OVRInput.GetUp(triggerButton);
        }

        /// <summary>
        /// Handler to abstract input.
        /// </summary>
        private bool TriggerHeld()
        {
            var triggerButton = WorldBeyondManager.Instance.GameController == OVRInput.Controller.RTouch ? OVRInput.RawButton.RIndexTrigger : OVRInput.RawButton.LIndexTrigger;
            return OVRInput.Get(triggerButton);
        }

        /// <summary>
        /// Cycle through toy options.
        /// </summary>
        private bool CycleToy(bool cycleForward)
        {
            var usingRightController = WorldBeyondManager.Instance.GameController == OVRInput.Controller.RTouch;
            var rightCycleButton = cycleForward ? OVRInput.RawButton.RThumbstickRight : OVRInput.RawButton.RThumbstickLeft;
            var leftCycleButton = cycleForward ? OVRInput.RawButton.LThumbstickRight : OVRInput.RawButton.LThumbstickLeft;
            var selectionButton = usingRightController ? rightCycleButton : leftCycleButton;
            return OVRInput.GetUp(selectionButton);
        }

        /// <summary>
        /// Effectively, "lock" toy
        /// </summary>
        public void EndGame()
        {
            StopLoopingSound();
            m_canSwitchToys = false;
        }

        /// <summary>
        /// Kill all audio associated with Multitoy.
        /// </summary>
        public void StopLoopingSound()
        {
            WallToyLoop_1.Stop();
            FlashlightLoop_1.Stop();
            FlashlightAbsorbLoop_1.Stop();
            if (m_audioSource)
            {
                m_audioSource.Stop();
            }
        }

        /// <summary>
        /// Self-explanatory.
        /// </summary>
        public bool IsFlashlightActive()
        {
            return m_toyIndexController == (int)ToyOption.Flashlight || m_toyIndexHand == (int)ToyOption.Flashlight;
        }

        /// <summary>
        /// If the wall toy is active, and it's highlighting a valid surface.
        /// </summary>
        public bool IsWalltoyActive()
        {
            return m_roomFramer ? m_roomFramer.IsHighlightingWall() : false;
        }

        /// <summary>
        /// Is player holding down the trigger while using the flashlight.
        /// </summary>
        public bool IsFlashlightAbsorbing()
        {
            return m_toyFlashlight.AbsorbingBall;
        }

        /// <summary>
        /// Show the correct Multitoy attachment, depending on the toy
        /// </summary>
        public void SetToyMesh(ToyOption newToy)
        {
            AccessoryBase.SetActive(newToy != ToyOption.None);
            AccessoryFlashlight.SetActive(newToy == ToyOption.Flashlight);
            AccessoryBallGun.SetActive(newToy == ToyOption.BallGun);
            AccessoryWallToy.SetActive(newToy == ToyOption.WallToy);

            switch (newToy)
            {
                case ToyOption.Flashlight:
                    Animator.SetTrigger("SetFlashlight");
                    break;
                case ToyOption.BallGun:
                    Animator.SetTrigger("SetShooter");
                    break;
                case ToyOption.WallToy:
                    Animator.SetTrigger("SetWallBeam");
                    break;
            }
        }

        /// <summary>
        /// Toggle the passthrough mesh that occludes the Multitoy handle.
        /// </summary>
        public void ShowPassthroughGlove(bool doShow, bool isRightHand = true)
        {
            if (HandGlove)
            {
                HandGlove.gameObject.SetActive(doShow);
                HandGlove.transform.localScale = new Vector3(isRightHand ? 1.0f : -1.0f, 1.0f, 1.0f);
            }
        }

        /// <summary>
        /// The starting point for any Multitoy effects.
        /// </summary>
        public Vector3 GetMuzzlePosition()
        {
            var endPosition = transform.position + transform.forward * 0.109f;
            if (WorldBeyondManager.Instance.UsingHands)
            {
                var refHand = WorldBeyondManager.Instance.GetActiveHand();
                return refHand.Bones[21].Transform.position;
            }
            return endPosition;
        }

        /// <summary>
        /// Track & handle the UI prompt informing the player about the wall.
        /// </summary>
        public void PointingAtWall()
        {
            if (WorldBeyondTutorial.Instance.CurrentMessage == WorldBeyondTutorial.TutorialMessage.AimWall)
            {
                m_pointingAtWallTimer += Time.deltaTime;
                if (m_pointingAtWallTimer >= 3.0f)
                {
                    m_pointingAtWallTimer = 0.0f;
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
                m_toyFlashlight.Flashlight.transform.SetParent(transform);
                m_toyFlashlight.Flashlight.transform.localPosition = Vector3.zero;
                m_toyFlashlight.Flashlight.transform.localRotation = Quaternion.identity;
            }
            else
            {
                m_toyFlashlight.Flashlight.transform.parent = m_toyFlashlight.LightBone;
                m_toyFlashlight.Flashlight.transform.localPosition = -0.1f * Vector3.forward;
                m_toyFlashlight.Flashlight.transform.localRotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// Handle when a ball is grabbed, when using hands only.
        /// </summary>
        public void GrabBall(BallCollectable bc)
        {
            if (GrabbedBall)
            {
                return;
            }
            bc.AbsorbBall();
            bc.SetState(BallCollectable.BallStatus.Grabbed);
            GrabbedBall = bc;
            WorldBeyondManager.Instance.DiscoveredBall(bc);
            WorldBeyondTutorial.Instance.HideMessage(WorldBeyondTutorial.TutorialMessage.BallSearch);

            if (!m_throwBallTaught)
            {
                WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.ShootBall);
                m_throwBallTaught = true;
            }
        }

        /// <summary>
        /// Handle when a ball is thrown, when using hands only.
        /// </summary>
        public void ThrewBall()
        {
            m_ballTossCooldownFactor = 0.0f;
            GrabbedBall = null;
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
        /// When pressing the Oculus button, hide the toy or else it snaps to the camera Position.
        /// </summary>
        private void OnApplicationFocus(bool pause)
        {
            if (WorldBeyondManager.Instance.CurrentChapter >= WorldBeyondManager.GameChapter.SearchForOppy)
            {
                if (m_toyIndexController < 0 || m_toyIndexController >= Toys.Length || Toys[m_toyIndexController] == null)
                {
                    return;
                }
                ShowToy(pause);
                if (pause)
                {
                    Toys[m_toyIndexController].Activate();
                }
                else
                {

                    Toys[m_toyIndexController].Deactivate();
                }
            }
        }

        public void UseHands(bool usingHands, bool rightHand = false)
        {
            if (usingHands)
            {
                if (ToyVisible && WorldBeyondManager.Instance.CurrentChapter >= WorldBeyondManager.GameChapter.SearchForOppy)
                {
                    SetToyMesh(ToyOption.None);
                }
                ShowPassthroughGlove(false);
            }
            else
            {
                SetToyMesh((ToyOption)m_toyIndexController);
                if (ToyVisible && WorldBeyondManager.Instance.CurrentChapter >= WorldBeyondManager.GameChapter.SearchForOppy)
                {
                    ShowPassthroughGlove(true, rightHand);
                }
            }
            m_toyFlashlight.SetLightStrength(usingHands ? 0.0f : 1.0f);
            ChildLightCone(usingHands);
        }

        public ToyOption GetCurrentToy()
        {
            return WorldBeyondManager.Instance.UsingHands ? (ToyOption)m_toyIndexHand : (ToyOption)m_toyIndexController;
        }
    }
}
