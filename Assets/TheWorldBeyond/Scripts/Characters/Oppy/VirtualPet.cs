// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using TheWorldBeyond.Audio;
using TheWorldBeyond.GameManagement;
using TheWorldBeyond.Toy;
using TheWorldBeyond.VFX;
using TheWorldBeyond.Wit;
using UnityEngine;
using UnityEngine.AI;

namespace TheWorldBeyond.Character.Oppy
{
    public class VirtualPet : MonoBehaviour
    {
        private BallCollectable m_ballEatTarget = null;

        // we need a cooldown before eating the next ball
        private float m_eatCooldown = 0.0f;
        private Vector3 m_moveTargetPos = Vector3.zero;
        private Vector3 m_moveTargetDir = Vector3.forward;
        private int m_collectedBalls = 0;
        private float m_glowOvershoot = 0.0f;
        private NavMeshAgent m_agent;
        private const float RUN_SPEED = 2.0f;
        private const float WALK_SPEED = 1.0f;
        private Animator m_animator;
        public Transform PetRoot;
        public BoneSimManager BoneSim;
        private Vector3 m_lastPetPosition;
        private Vector3 m_faceDirection = Vector3.forward;
        private float m_lookAtPlayerDistance = 7.0f;
        private const float MAX_HEAD_ANGLE = 45.0f;
        private const float MAX_EYE_ANGLE = 30.0f;
        // head is Z-left, X-down, and Y-forward
        public Transform HeadBone;
        private Quaternion m_targetRotation = Quaternion.identity;
        private bool m_lookingAtTarget = false;
        private BallCollectable m_ballLookTarget = null;
        // eyes are Z-forward
        public Transform LeftEye;
        public Transform RightEye;
        private Quaternion m_originalLeftEyeRot = Quaternion.identity;
        private Quaternion m_originalRightEyeRot = Quaternion.identity;
        public Transform BallAttachmentBoneRight = null;
        public Transform BallAttachmentBoneLeft = null;
        private bool m_ballAttached = false;

        public SkinnedMeshRenderer[] BodyMeshes;

        public GameObject ListeningIndicator;
        public GameObject FullyChargedPrefab;

        // this sphere object is what "hides" Oppy during SearchForOppy
        // simpler to do this than modify Oppy's materials
        public GameObject PassthroughShell;

        public enum PetState
        {
            Idle,
            Chasing, // Oppy's default state
            Angry,
            Petting,
            Eating,
            Listening,
            Ending // running to ship to take off, m_pet is on auto-pilot
        };
        public PetState OppyState { private set; get; } = PetState.Idle;

        public GameObject TeleportPrefab;
        public AudioClip SparkleSound;
        private AudioSource m_audioSource;

        public Transform ShadowMesh;
        public Transform ShadowRoot;
        private bool m_rampBaseHit = false;

        public ThoughtBubble ThoughtBubble;
        private Transform m_mainCam;

        private void Awake()
        {
            transform.position = Vector3.zero;
            m_audioSource = GetComponent<AudioSource>();
            m_animator = GetComponent<Animator>();

            ListeningIndicator.SetActive(false);
        }

        public void Initialize()
        {
            m_originalLeftEyeRot = LeftEye.localRotation;
            m_originalRightEyeRot = RightEye.localRotation;
            m_agent = GetComponent<NavMeshAgent>();
            _ = m_agent.SetDestination(Vector3.zero);
            // Body rotation is controlled in FacePosition()
            // Look direction is controlled in DoLookAtBehavior()
            m_agent.updateRotation = false;
            ResetAnimFlags();

            ThoughtBubble.gameObject.SetActive(false);

            // turn off ball/oppy collision
            // instead of dealing with players intentionally shooting Oppy, and the added complexity of her reacting, just disable it
            Physics.IgnoreLayerCollision(13, 14, true);
        }

        public void LateUpdate()
        {
            if (OVRInput.GetUp(OVRInput.RawButton.A))
            {
                PrintDebugOutput();
            }

            if (!m_mainCam)
            {
                m_mainCam = WorldBeyondManager.Instance.MainCamera.transform;
            }

            m_animator.SetBool("Running", m_agent.velocity.magnitude > Mathf.Epsilon);
            // agent.velocity.magnitude is animator.speed (between 1 and 2, set in DoChaseBehavior)
            m_animator.SetFloat("Speed", Mathf.Clamp01(m_agent.velocity.magnitude * 0.25f + 0.5f));

            if (m_glowOvershoot > 0.0f)
            {
                m_glowOvershoot -= Time.deltaTime * 2.0f;
                m_glowOvershoot = Mathf.Clamp01(m_glowOvershoot);
                foreach (var smr in BodyMeshes)
                {
                    smr.sharedMaterial.SetFloat("_GlowStrength", m_glowOvershoot);
                }
            }

            switch (OppyState)
            {
                case PetState.Idle:
                    break;
                case PetState.Chasing:
                    DoChaseBehavior();
                    CheckForPetting();
                    m_eatCooldown += Time.deltaTime;
                    break;
                case PetState.Eating:
                    if (m_ballEatTarget && m_ballAttached)
                    {
                        var targetGrabPos = (BallAttachmentBoneRight.position + BallAttachmentBoneLeft.position) * 0.5f;
                        m_ballEatTarget.gameObject.transform.position = Vector3.Lerp(m_ballEatTarget.gameObject.transform.position, targetGrabPos, 0.5f);
                    }
                    break;
                case PetState.Listening:
                    CheckForPetting();
                    break;
                case PetState.Ending:
                    // trigger the UFO animation upon approaching
                    var targetPos = m_rampBaseHit ? WorldBeyondManager.Instance.FinalUfoTarget.position + Vector3.up * 3.0f : WorldBeyondManager.Instance.FinalUfoRamp.position;
                    var targetDistance = transform.position - targetPos;
                    var dist = m_rampBaseHit ? 1.0f : 0.5f;
                    var endGame = false;
                    if (targetDistance.magnitude <= dist)
                    {
                        if (!m_rampBaseHit)
                        {
                            m_rampBaseHit = true;
                            _ = m_agent.SetDestination(WorldBeyondManager.Instance.FinalUfoTarget.position + Vector3.up * 3.0f);
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
                        WorldBeyondManager.Instance.FlyAwayUFO();
                        OppyState = PetState.Idle;
                        gameObject.transform.position = m_mainCam.position;
                        gameObject.SetActive(false);
                    }
                    break;
            }

            var targetLookDirection = m_moveTargetDir;

            // rotate Oppy to look in the correct direction
            var currentVelocity = transform.position - m_lastPetPosition;
            if (currentVelocity.magnitude > Mathf.Epsilon)
            {
                targetLookDirection = currentVelocity.normalized;
            }
            m_lastPetPosition = transform.position;
            m_faceDirection = Vector3.Slerp(m_faceDirection, targetLookDirection, 0.1f);
            FacePosition(transform.position + m_faceDirection);

            if (m_lookingAtTarget)
            {
                DoLookAtBehavior(false, true);
            }

            // Position shadow
            if (ShadowMesh && ShadowRoot)
            {
                ShadowMesh.position = ShadowRoot.position;
                ShadowMesh.localPosition = new Vector3(ShadowMesh.transform.localPosition.x, 0.005f, ShadowMesh.transform.localPosition.z);
                if (PassthroughShell && PassthroughShell.activeSelf)
                {
                    PassthroughShell.transform.position = ShadowMesh.position + Vector3.up * 0.3f;
                }
            }
        }

        private void DoLookAtBehavior(bool instantLook, bool eyesAlso)
        {
            var inDistance = Vector3.Distance(transform.position, m_mainCam.position) <= m_lookAtPlayerDistance;
            var headLook = HeadBone.transform.rotation * Vector3.up;
            var headUp = HeadBone.transform.rotation * Vector3.right;
            var lookPosition = HeadBone.position + headLook;

            if (WorldBeyondManager.Instance.CurrentChapter == WorldBeyondManager.GameChapter.OppyExploresReality
                && MultiToy.Instance.IsWalltoyActive())
            {
                lookPosition = MultiToy.Instance.WallToyTarget.position;
            }
            else if (m_ballLookTarget && OppyState != PetState.Listening)
            {
                lookPosition = new Vector3(m_ballLookTarget.gameObject.transform.position.x, Mathf.Clamp(m_ballLookTarget.gameObject.transform.position.y, 0.0f, 2.0f), m_ballLookTarget.gameObject.transform.position.z);
            }
            else
            {
                if (HeadBone && inDistance)
                {
                    lookPosition = m_mainCam.position;
                }
            }

            // the head RootBone is at a neck Position
            // so the illusion requires it to look at a point slightly below the camera
            var petHeadY = lookPosition - Vector3.up * 0.2f - HeadBone.position;

            // clamp angle so m_pet doesn't break its neck
            var currentAngle = Vector3.Angle(petHeadY, headLook);
            petHeadY = Vector3.Slerp(headLook, petHeadY, Mathf.Clamp01(MAX_HEAD_ANGLE / currentAngle)).normalized;

            var petHeadX = headUp;
            Vector3.OrthoNormalize(ref petHeadY, ref petHeadX);
            var petHeadZ = Vector3.Cross(petHeadX, petHeadY);
            var lookRot = Quaternion.LookRotation(petHeadZ, petHeadY);
            m_targetRotation = instantLook ? lookRot : Quaternion.Lerp(m_targetRotation, lookRot, 0.05f);

            HeadBone.rotation = m_targetRotation;

            if (eyesAlso)
            {
                var leftLook = lookPosition - LeftEye.position;
                var rightLook = lookPosition - RightEye.position;
                var leftEyeFwd = LeftEye.parent.rotation * m_originalLeftEyeRot * Vector3.forward;
                var rightEyeFwd = RightEye.parent.rotation * m_originalRightEyeRot * Vector3.forward;
                var leftAngle = Vector3.Angle(leftEyeFwd, leftLook);
                var rightAngle = Vector3.Angle(rightEyeFwd, rightLook);
                leftLook = Vector3.Slerp(leftEyeFwd, leftLook, Mathf.Clamp01(MAX_EYE_ANGLE / leftAngle)).normalized;
                rightLook = Vector3.Slerp(rightEyeFwd, rightLook, Mathf.Clamp01(MAX_EYE_ANGLE / rightAngle)).normalized;

                LeftEye.rotation = Quaternion.LookRotation(leftLook);
                RightEye.rotation = Quaternion.LookRotation(rightLook);
            }
        }

        public void StartLookTarget()
        {
            m_lookingAtTarget = true;
            DoLookAtBehavior(true, true);
        }

        public void EndLookTarget()
        {
            m_lookingAtTarget = false;
        }

        public void SetLookDirection(Vector3 lookDirection)
        {
            m_moveTargetDir = lookDirection;
        }

        private void DoChaseBehavior()
        {
            if (m_animator.GetBool("Eating") || m_animator.GetBool("Petting"))
            {
                return;
            }

            // get the closest one
            var ballBC = WorldBeyondManager.Instance.GetClosestEdibleBall(transform.position);
            if (WorldBeyondManager.Instance.CurrentChapter == WorldBeyondManager.GameChapter.OppyExploresReality
                && MultiToy.Instance.IsWalltoyActive())
            {
                SetMoveTarget(MultiToy.Instance.WallToyTarget.position - MultiToy.Instance.WallToyTarget.forward * 0.5f, MultiToy.Instance.WallToyTarget.forward);
                m_agent.speed = WALK_SPEED;
                return;
            }

            m_ballLookTarget = ballBC;

            if (ballBC)
            {
                // put Oppy target "behind" ball so we get a good view of her eating it
                var ballDirectionToOppy = Vector3.Scale(transform.position - ballBC.gameObject.transform.position, new Vector3(1, 0, 1));
                var viewDirectionToBall = Vector3.Scale(ballBC.gameObject.transform.position - m_mainCam.position, new Vector3(1, 0, 1));
                var currentAngle = Vector3.Angle(ballDirectionToOppy, viewDirectionToBall);
                if (ballDirectionToOppy.magnitude > 0.5f)
                {
                    if (currentAngle > 45.0f)
                    {
                        ballDirectionToOppy = Vector3.Slerp(viewDirectionToBall, ballDirectionToOppy, Mathf.Clamp01(45.0f / currentAngle)).normalized;
                        // make sure there's space for Oppy there
                        var idealOppyPosition = GetIdealOppyPosition(ballBC.gameObject.transform.position, ballDirectionToOppy);
                        SetMoveTarget(idealOppyPosition, -ballDirectionToOppy.normalized, true);
                    }
                    else
                    {
                        SetMoveTarget(new Vector3(ballBC.gameObject.transform.position.x, WorldBeyondManager.Instance.GetFloorHeight(), ballBC.gameObject.transform.position.z), -ballDirectionToOppy.normalized, true);
                    }
                }
                // once close enough, start the eat animation & lock the ball in
                else if (ballDirectionToOppy.magnitude <= 0.5f
                    && !ballBC.RigidBody.isKinematic
                    && ballBC.RigidBody.velocity.magnitude <= 0.5f
                    && m_eatCooldown >= 1.0f)
                {
                    m_animator.SetBool("Eating", true);
                    OppyState = PetState.Eating;

                    var idealBallPosition = m_moveTargetPos + m_moveTargetDir * 0.1f;
                    ballBC.PrepareForEating(idealBallPosition);

                    m_ballEatTarget = ballBC;
                    m_ballLookTarget = ballBC;
                }

                // slow down when approaching target
                var locomotionBlend = Mathf.Clamp01((transform.position - ballBC.gameObject.transform.position).magnitude - 1.0f);
                m_agent.speed = Mathf.Lerp(WALK_SPEED, RUN_SPEED, locomotionBlend);
            }
        }

        // make sure there's space for Oppy to eat the ball
        private Vector3 GetIdealOppyPosition(Vector3 ballPos, Vector3 ballDirectionToOppy)
        {
            var idealPos = ballPos + ballDirectionToOppy.normalized * 0.2f;
            LayerMask roomLayer = LayerMask.GetMask("RoomBox", "Furniture");
            var roomboxHit = Physics.RaycastAll(ballPos, ballDirectionToOppy, 0.2f, roomLayer);
            foreach (var hit in roomboxHit)
            {
                idealPos = hit.point + hit.normal * 0.3f;
            }

            return idealPos;
        }

        private void CheckForPetting()
        {
            var petting = false;
            // get the center eye Position, close enough to head top
            var foreheadPos = (LeftEye.position + RightEye.position) * 0.5f;
            // only do calculation if the m_pet is close
            if (m_mainCam && Vector3.Distance(m_mainCam.transform.position, foreheadPos) < 1.5f)
            {
                var lHand = WorldBeyondManager.Instance.LeftHandAnchor;
                var rHand = WorldBeyondManager.Instance.RightHandAnchor;
                if (WorldBeyondManager.Instance.UsingHands)
                {
                    if (WorldBeyondManager.Instance.LeftHand && WorldBeyondManager.Instance.RightHand)
                    {
                        lHand = WorldBeyondManager.Instance.LeftHand.Bones[9].Transform;
                        rHand = WorldBeyondManager.Instance.RightHand.Bones[9].Transform;
                    }
                }
                const float MIN_DIST = 0.3f;
                petting = Vector3.Distance(lHand.position, foreheadPos) < MIN_DIST || Vector3.Distance(rHand.position, foreheadPos) < MIN_DIST;
            }

            // check eat hand for distance to head
            m_animator.SetBool("Petting", petting);
            if (petting)
            {
                _ = WitConnector.Instance.WitSwitcher(false);
            }
        }

        public void SetOppyChapter(WorldBeyondManager.GameChapter newChapter)
        {
            StopAllCoroutines();
            ResetAnimFlags();
            switch (newChapter)
            {
                case WorldBeyondManager.GameChapter.Title:
                    WorldBeyondManager.Instance.SpaceShipAnimator.StopIdleSound();
                    break;
                case WorldBeyondManager.GameChapter.Introduction:
                    m_collectedBalls = 0;
                    OppyState = PetState.Idle;
                    transform.position = m_mainCam.position;
                    break;
                case WorldBeyondManager.GameChapter.OppyBaitsYou:
                    m_collectedBalls = 0;
                    OppyState = PetState.Idle;
                    break;
                case WorldBeyondManager.GameChapter.SearchForOppy:
                    m_collectedBalls = 0;
                    OppyState = PetState.Idle;
                    SetMaterialSaturation(0);
                    EnablePassthroughShell(true);
                    break;
                case WorldBeyondManager.GameChapter.OppyExploresReality:
                    WorldBeyondManager.Instance.SpaceShipAnimator.StopIdleSound();
                    m_collectedBalls = 0;
                    m_animator.SetBool("Wonder", true);
                    SetMaterialSaturation(1.0f);
                    EnablePassthroughShell(false);
                    SetLookDirection((m_mainCam.position - transform.position).normalized);
                    break;
                case WorldBeyondManager.GameChapter.TheGreatBeyond:
                    OppyState = PetState.Idle;
                    m_ballEatTarget = null;
                    m_animator.SetBool("Wonder", true);
                    EnablePassthroughShell(false);
                    break;
                case WorldBeyondManager.GameChapter.Ending:
                    EndLookTarget();
                    _ = m_agent.SetDestination(WorldBeyondManager.Instance.FinalUfoRamp.position);
                    m_rampBaseHit = false;
                    OppyState = PetState.Ending;
                    m_agent.speed = RUN_SPEED;
                    break;
            }
        }

        #region AnimEvents
        // these are called from animations
        public void AttachBallToBone()
        {
            if (m_ballEatTarget)
            {
                m_ballAttached = true;
            }
        }

        public void ChompBall()
        {
            if (m_ballEatTarget)
            {
                // only start counting balls after the walls have been opened
                // otherwise there's a chance to finish the game prematurely
                if (m_ballEatTarget.BallAdvancesStory)
                {
                    m_collectedBalls++;
                }
                m_ballEatTarget.ForceKill(HeadBone.up);
                m_ballEatTarget = null;
                m_ballAttached = false;
                m_glowOvershoot = 1.0f;
            }
        }

        public void FinishEating()
        {
            StartLookTarget();

            m_animator.SetBool("Eating", false);

            m_eatCooldown = 0.0f;

            if (m_collectedBalls >= WorldBeyondManager.Instance.OppyTargetBallCount)
            {
                m_animator.SetTrigger("PowerUp");
                OppyState = PetState.Idle;
            }
            else
            {
                ResumeChasing();
            }

            //NOTE: after eating, Oppy will try to listen again, if it doesn't have user's focus or can't enable voice somehow, Oppy moves away.
            if (CanListen() && WitConnector.Instance.CurrentFocus)
            {
                var reactivateWit = WitConnector.Instance.WitSwitcher(true);
                if (!reactivateWit) MoveAway();
            }
        }

        public void PlayPowerUp()
        {
            _ = Instantiate(FullyChargedPrefab, transform.position, transform.rotation);
            AudioManager.SetSnapshot_TheGreatBeyond_AfterPowerup();
        }

        public void GoToUFO()
        {
            WorldBeyondManager.Instance.ForceChapter(WorldBeyondManager.GameChapter.Ending);
        }
        #endregion AnimEvents
        public bool IsGameOver()
        {
            var gameOver = WorldBeyondManager.Instance.CurrentChapter == WorldBeyondManager.GameChapter.Ending;
            gameOver &= m_rampBaseHit;
            return gameOver;
        }

        public void ResetAnimFlags()
        {
            m_animator.SetBool("Running", false);
            m_animator.SetBool("Discovered", false);
            m_animator.SetBool("Listening", false);
            m_animator.SetBool("Petting", false);
            m_animator.ResetTrigger("PowerUp");
            m_animator.SetBool("Dislike", false);
            m_animator.SetBool("Eating", false);
            m_animator.ResetTrigger("Jumping");
            m_animator.SetBool("Wonder", false);
            m_animator.ResetTrigger("Like");
            m_animator.ResetTrigger("Wave");
            m_animator.SetBool("ListenFail", false);
        }

        public bool CanListen()
        {
            var canListen = m_collectedBalls < WorldBeyondManager.Instance.OppyTargetBallCount;
            canListen &= WorldBeyondManager.Instance.CurrentChapter == WorldBeyondManager.GameChapter.TheGreatBeyond;
            canListen &= OppyState == PetState.Chasing;
            canListen &= m_ballEatTarget == null;
            canListen &= !m_animator.GetBool("Petting");
            canListen &= !m_animator.GetBool("Running");
            return canListen;
        }

        public void PlayInitialDiscoveryAnim()
        {
            m_animator.speed = 1.0f;
        }

        public void PrepareInitialDiscoveryAnim()
        {
            m_animator.SetBool("Discovered", true);
            m_animator.speed = 0.0f;
        }

        public void PlayRandomOppyDiscoveryAnim()
        {
            m_lookingAtTarget = true;
            BoneSim.gameObject.SetActive(false);
            var randomChance = Random.Range(0.0f, 1.0f);
            if (randomChance > 0.5f)
            {
                m_animator.SetTrigger("Like");
            }
            else
            {
                m_animator.SetTrigger("Wave");
            }
        }

        public void PlaySparkles(bool doPlay)
        {
            m_audioSource.clip = SparkleSound;
            if (doPlay)
            {
                m_audioSource.time = 0.0f;
                m_audioSource.Play();
            }
            else
            {
                m_audioSource.Stop();
            }
        }

        public void PlayTeleport()
        {
            _ = Instantiate(TeleportPrefab, PetRoot.position, PetRoot.rotation);
        }

        public void SetMaterialSaturation(float matSat)
        {
            foreach (var smr in BodyMeshes)
            {
                smr.sharedMaterial.SetFloat("_SaturationAmount", matSat);
            }
        }

        public void FacePosition(Vector3 worldPosition)
        {
            worldPosition = new Vector3(worldPosition.x, transform.position.y, worldPosition.z);
            PetRoot.rotation = Quaternion.LookRotation(worldPosition - transform.position);
        }

        public void ShotOppy()
        {
            m_animator.SetBool("Dislike", true);
            m_ballEatTarget = null;
            FacePosition(m_mainCam.position);
            OppyState = PetState.Angry;
        }

        public void EnablePassthroughShell(bool doEnable)
        {
            PassthroughShell.SetActive(doEnable);
        }

        public void ResumeChasing()
        {
            OppyState = GetResumedState();
            m_lookingAtTarget = true;
            ResetAnimFlags();
        }

        private PetState GetResumedState()
        {
            return OppyState == PetState.Ending ? PetState.Ending : PetState.Chasing;
        }

        // by default, only move to a new target if it's 1m away from its current one, or forced
        public void SetMoveTarget(Vector3 worldPosition, Vector3 faceDirection, bool force = false)
        {
            var currTargetPos = new Vector3(worldPosition.x, WorldBeyondManager.Instance.GetFloorHeight(), worldPosition.z);

            if (Vector3.Distance(m_moveTargetPos, currTargetPos) > 1.0f || force)
            {
                m_moveTargetPos = currTargetPos;
                m_moveTargetDir = new Vector3(faceDirection.x, WorldBeyondManager.Instance.GetFloorHeight(), faceDirection.z).normalized;
                _ = m_agent.SetDestination(m_moveTargetPos);
            }
        }

        private void ApproachPlayer()
        {
            var targetPos = m_mainCam.position + m_mainCam.forward;
            m_agent.speed = RUN_SPEED;
            _ = m_agent.SetDestination(new Vector3(targetPos.x, WorldBeyondManager.Instance.GetFloorHeight(), targetPos.z));
        }

        private void PrintDebugOutput()
        {
            Debug.Log("TheWorldBeyond: m_pet state: " + OppyState);
            Debug.Log("TheWorldBeyond: valid ball: " + (m_ballEatTarget == null));
            Debug.Log("TheWorldBeyond: bool Running: " + m_animator.GetBool("Running"));
            Debug.Log("TheWorldBeyond: bool Discovered: " + m_animator.GetBool("Discovered"));
            Debug.Log("TheWorldBeyond: bool Listening: " + m_animator.GetBool("Listening"));
            Debug.Log("TheWorldBeyond: bool Petting: " + m_animator.GetBool("Petting"));
            Debug.Log("TheWorldBeyond: bool Dislike: " + m_animator.GetBool("Dislike"));
            Debug.Log("TheWorldBeyond: bool Eating: " + m_animator.GetBool("Eating"));
            Debug.Log("TheWorldBeyond: bool ListenFail: " + m_animator.GetBool("ListenFail"));
            Debug.Log("TheWorldBeyond: bool Wonder: " + m_animator.GetBool("Wonder"));
        }

        public void DisplayThought(string thought = "")
        {
            ThoughtBubble.gameObject.SetActive(true);
            ThoughtBubble.ForceSizeUpdate();
            if (thought == "")
            {
                ThoughtBubble.ShowHint();
            }
            else
            {
                ThoughtBubble.UpdateText(thought);
            }
        }

        public void HideThought()
        {
            ThoughtBubble.gameObject.SetActive(false);
        }

        //NOTE: Called from ListenFailAni which is attached on ListenFail Animation
        public void MoveAway()
        {
            m_animator.SetBool("Listening", false);
            m_animator.SetBool("ListenFail", false);
            ListeningIndicator.SetActive(false);
            var point = Random.insideUnitCircle.normalized * Random.Range(2f, 6f);
            var targetPoint = m_mainCam.position + new Vector3(point.x, WorldBeyondManager.Instance.GetFloorHeight(), point.y);
            targetPoint.y = 0;
            _ = m_agent.SetDestination(targetPoint);
        }

        //NOTE: Called from CheckListenAvailable script which is attached on Run and Pet_End Animations
        public void CheckListenAvailable()
        {
            _ = StartCoroutine(TryToReactivateWit());
        }

        private IEnumerator TryToReactivateWit(float waitTime = 1)
        {
            yield return new WaitForSeconds(waitTime);
            if (CanListen() && WitConnector.Instance.CurrentFocus && ArrivedDestination())
            {
                _ = WitConnector.Instance.WitSwitcher(true);
            }
        }

        private bool ArrivedDestination()
        {
            if (!m_agent.pathPending)
            {
                if (m_agent.remainingDistance <= m_agent.stoppingDistance)
                {
                    if (!m_agent.hasPath || m_agent.velocity.sqrMagnitude == 0f)
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
            if (value) m_animator.SetBool("ListenFail", false);
            ListeningIndicator.SetActive(value);
            // make sure to properly exit the animator state
            // for example when it's interrupted by shooting a ball
            if (!value && m_animator.GetBool("Listening"))
            {
                m_animator.SetTrigger("ForceChase");
            }
            m_animator.SetBool("Listening", value);
            OppyState = value ? PetState.Listening : GetResumedState();
            if (value)
            {
                SetLookDirection((m_mainCam.position - transform.position).normalized);
            }
        }
        public void ListenFail()
        {
            HideThought();
            m_animator.SetBool("ListenFail", true);
            ListeningIndicator.SetActive(false);
        }

        public void VoiceCommandHandler(string actionString)
        {
            m_animator.SetBool("Listening", false);
            switch (actionString)
            {
                case "come":
                    ApproachPlayer();
                    break;
                case "jump":
                    m_animator.SetTrigger("Jumping");
                    break;
                case "hi":
                    m_animator.SetTrigger("Wave");
                    break;
            }
            Listening(false);
            DisplayThought(actionString + "?");
        }
        #endregion VoiceCommand
    }
}
