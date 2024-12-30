// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Interaction.HandGrab;
using TheWorldBeyond.Audio;
using TheWorldBeyond.Character.Oppy;
using TheWorldBeyond.GameManagement;
using TheWorldBeyond.Toy;
using TheWorldBeyond.Wit;
using UnityEngine;

namespace TheWorldBeyond.VFX
{
    public class BallCollectable : MonoBehaviour
    {
        public enum BallStatus
        {
            Unavailable, // Hidden, but can't be grabbed: only for the very first ball
            Hidden, // Available, but hidden under Passthrough; when initially spawned
            Available, // is available to be picked up
            Grabbed, // when the player absorbs the ball, with hands or controller
            Released, // has physics on, was just shot, is available for Oppy to eat
            Eaten // when Oppy has picked up the ball
        };
        public BallStatus BallState { get; private set; } = BallStatus.Unavailable;

        // if the ball falls below this elevation for some reason, destroy it
        private const float KILL_DEPTH = -50.0f;
        private const float ABSORB_TIME_REQUIRED = 0.5f;
        private const float VELOCITY_REST_THRESH = 0.1f;
        private float m_absorbTimer = 0.0f;
        private bool m_isAbsorbing = false;
        public int WallID = -1;
        public Rigidbody RigidBody;

        public GameObject PopFXprefab;
        public GameObject PassthroughImpactFXprefab;

        public SoundEntry SfxBallLoop;
        public SoundEntry SfxBallBounce;

        // disable this upon shooting, or else holding the trigger will immediately grab it
        public DistanceHandGrabInteractable HandDistanceInteractable;
        public HandGrabInteractable HandGrabInteractable;
        private float m_audioTimer = 0.0f;
        public float ShotTimer = 0.0f;
        private const float SHOT_STRENGTH_MULTIPLIER = 1.0f;
        private const float UNGRABBABLE_SHOT_STRENGTH_MULTIPLIER = 0.5f;

        // the object that "hides" the ball behind Passthrough
        public MeshRenderer ShellObject;
        public MeshRenderer ShadowObject;
        public ParticleSystem HiddenFX;
        public GameObject DebrisPrefab;
        [HideInInspector]
        // after the ball was shot
        // this means the ball won't count towards "discovery" during the scripted section
        public bool WasShot = false;

        // if player is intentionally shooting Oppy; set this to false upon any other collision
        private bool m_directOppyShot = true;

        // by default, don't allow a ball to "count" towards Oppy finishing the game
        // this is to artificially gate progress
        public bool BallAdvancesStory { get; private set; } = false;

        private float m_eatStartCooldown = 0.0f;

        private MeshRenderer m_renderer;
        private ParticleSystemRenderer m_particleSystemRenderer;

        public void ForceInvisible()
        {
            m_renderer.enabled = false;
            ShadowObject.enabled = false;
            m_particleSystemRenderer.enabled = false;
        }

        public void ForceVisible()
        {
            m_renderer.enabled = true;
            ShadowObject.enabled = true;
            m_particleSystemRenderer.enabled = true;
        }
        public void Start()
        {
            m_renderer = GetComponent<MeshRenderer>();
            m_particleSystemRenderer = GetComponent<ParticleSystemRenderer>();
            SfxBallLoop.SetVolume(0.0f);
            SfxBallLoop.Play();
            ShadowObject.transform.parent = null;
            ShadowObject.transform.rotation = Quaternion.Euler(90, 0, 0);
            ShadowObject.material.SetFloat("_ZTest", 4);
        }

        private void OnDestroy()
        {
            Destroy(ShadowObject.gameObject);
        }

        public void Update()
        {
            // fade audio when ball is available and flashlight is active
            var audioFadeDirection = ((BallState == BallStatus.Available || BallState == BallStatus.Hidden) && MultiToy.Instance.IsFlashlightActive()) ? 1.0f : -1.5f;
            m_audioTimer += Time.deltaTime * audioFadeDirection;
            m_audioTimer = Mathf.Clamp01(m_audioTimer);
            SfxBallLoop.SetVolume(m_audioTimer);

            RigidBody.useGravity = !m_isAbsorbing;
            m_isAbsorbing = false;

            if (BallState == BallStatus.Released)
            {
                ShotTimer += Time.deltaTime;
            }
            else if (BallState == BallStatus.Eaten)
            {
                // sometimes the Oppy eat animation gets interrupted
                // in that case, the ball needs to be reset so it can be absorbed by the player
                m_eatStartCooldown += Time.deltaTime;
                if (m_eatStartCooldown >= 2.0f)
                {
                    BallState = BallStatus.Available;
                }
            }
            else if (BallState == BallStatus.Grabbed)
            {
                var targetPos = MultiToy.Instance.transform.position;
                if (!WorldBeyondManager.Instance.UsingHands)
                {
                    transform.position = Vector3.Lerp(transform.position, targetPos, 0.2f);

                    if (Vector3.Distance(transform.position, targetPos) <= 0.05f)
                    {
                        WorldBeyondManager.Instance.RemoveBallFromWorld(this);
                        WorldBeyondTutorial.Instance.HideMessage(WorldBeyondTutorial.TutorialMessage.BallSearch);
                    }
                }
            }

            // collide the shadow with room objects
            LayerMask effectLayer = LayerMask.GetMask("RoomBox", "Furniture");
            var shadowDist = 100.0f;
            var closestHit = shadowDist;
            var restedOnFurniture = false;
            var roomboxHit = Physics.RaycastAll(transform.position, -Vector3.up, shadowDist, effectLayer);
            if (BallState == BallStatus.Released && roomboxHit != null && roomboxHit.Length > 0)
            {
                ShadowObject.enabled = true;
                foreach (var hitInfo in roomboxHit)
                {
                    var thisDist = Vector3.Distance(transform.position, hitInfo.point);
                    if (thisDist < closestHit)
                    {
                        closestHit = thisDist;
                        ShadowObject.transform.position = hitInfo.point + Vector3.up * 0.001f;
                        var hoverDist = Vector3.Distance(transform.position, hitInfo.point);
                        ShadowObject.material.SetFloat("_Intensity", Mathf.Clamp01(1 - hoverDist * 2));

                        // if the ball has come to a rest on top of furniture, shoot in a direction towards the m_pet
                        // this avoids a bug where the ball is unreachable
                        if (RigidBody && HasBallRested() && hitInfo.collider.gameObject.layer == LayerMask.NameToLayer("Furniture"))
                        {
                            restedOnFurniture = true;
                        }
                    }
                }

                if (restedOnFurniture)
                {
                    var ejectDirection = (WorldBeyondManager.Instance.Pet.transform.position - transform.position).normalized + Vector3.up;
                    Shoot(transform.position, ejectDirection.normalized * UNGRABBABLE_SHOT_STRENGTH_MULTIPLIER);
                }
            }
            else
            {
                ShadowObject.enabled = false;
            }

            if (WorldBeyondManager.Instance && transform.position.y < WorldBeyondManager.Instance.GetFloorHeight() + KILL_DEPTH)
            {
                ForceKill(Vector3.up);
            }
        }

        public void Absorbing(Vector3 rayFromHand)
        {
            if (BallState is BallStatus.Eaten or BallStatus.Grabbed)
            {
                return;
            }
            SetState(BallStatus.Available);
            m_absorbTimer += Time.deltaTime;
            RigidBody.isKinematic = false;
            m_isAbsorbing = true;
            RigidBody.AddForce(-rayFromHand * 0.03f, ForceMode.Force);
        }

        public bool IsBallAbsorbed()
        {
            var timeLimitHit = m_absorbTimer >= ABSORB_TIME_REQUIRED;
            var vecToPet = transform.position - MultiToy.Instance.transform.position;
            var closeEnough = Mathf.Sqrt(vecToPet.x * vecToPet.x + vecToPet.z * vecToPet.z) <= 0.3f;
            return timeLimitHit || closeEnough;
        }

        public bool HasBallRested()
        {
            return RigidBody.velocity.magnitude < VELOCITY_REST_THRESH;
        }

        public void AbsorbBall()
        {
            SetState(BallStatus.Grabbed);
            m_absorbTimer = 0.0f;
            WallID = -1;
            RigidBody.isKinematic = true;
            RigidBody.velocity = Vector3.zero;
            MultiToy.Instance.FlashlightAbsorb_1.Play();
            SfxBallLoop.SetVolume(0);
            ShellObject.enabled = false;

            // only needed in SearchForOppy, so it renders above passthrough when absorbed
            if (GetComponent<MeshRenderer>())
            {
                GetComponent<MeshRenderer>().material.renderQueue = 3000;
            }
        }

        public void PlaceHiddenBall(Vector3 worldPos, int owningSurface)
        {
            Debug.Log("TWB placing ball: " + worldPos);
            WallID = owningSurface;
            transform.position = worldPos;
            var facingCam = (WorldBeyondManager.Instance.MainCamera.transform.position - worldPos).normalized;
            var fwd = -Vector3.up;
            Vector3.OrthoNormalize(ref facingCam, ref fwd);
            transform.rotation = Quaternion.LookRotation(fwd, facingCam);
            RigidBody.isKinematic = true;
            ShellObject.enabled = true;
            SetState(BallStatus.Hidden);
            WorldBeyondManager.Instance.AddBallToWorld(this);
            Debug.Log("TWB placed ball: " + transform.position);
        }

        public void Shoot(Vector3 pos, Vector3 shootForce)
        {
            SetState(BallStatus.Released); // will kill the ball loop sound too
            transform.position = pos; // set the origin to match target
            RigidBody.isKinematic = false;
            RigidBody.useGravity = true;
            RigidBody.AddForce(shootForce * SHOT_STRENGTH_MULTIPLIER);
            var randomTorque = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f)).normalized;
            RigidBody.AddTorque(randomTorque * 20);
            MultiToy.Instance.BallShoot_1.Play();
            WorldBeyondTutorial.Instance.HideMessage(WorldBeyondTutorial.TutorialMessage.ShootBall);
            var canChaseBall = WorldBeyondManager.Instance.CurrentChapter >= WorldBeyondManager.GameChapter.OppyExploresReality;
            if (canChaseBall) WorldBeyondManager.Instance.Pet.Listening(false);
            WorldBeyondManager.Instance.Pet.HideThought();
            GetComponent<AudioSource>().Stop();
            BallAdvancesStory = WorldBeyondManager.Instance.CurrentChapter >= WorldBeyondManager.GameChapter.TheGreatBeyond;
            WasShot = true;
        }

        /// <summary>
        /// Only called when using hands, triggered by an event (check the prefab)
        /// </summary>
        public void Grab()
        {
            if (MultiToy.Instance) MultiToy.Instance.GrabBall(this);
        }

        /// <summary>
        /// Only called when using hands, triggered by an event (check the prefab)
        /// </summary>
        public void Throw()
        {
            ForceVisible();
            Shoot(transform.position, MultiToy.Instance.GetFlashlightDirection());
            MultiToy.Instance.ThrewBall();
            if (WitConnector.Instance) _ = WitConnector.Instance.WitSwitcher(false);
        }

        /// <summary>
        /// Entry point for properly destroying ball.
        /// </summary>
        public void ForceKill(Vector3 upVec)
        {
            StopAllCoroutines();
            var effectFwd = Vector3.Cross(Vector3.up, upVec);
            var popFX = Instantiate(PopFXprefab);
            popFX.transform.position = transform.position;
            popFX.transform.rotation = Quaternion.LookRotation(effectFwd);
            popFX.GetComponent<ParticleSystem>().Play();
            SfxBallLoop.Stop();
            WorldBeyondManager.Instance.RemoveBallFromWorld(this);
        }

        /// <summary>
        /// Is this ball eligible to be eaten/chased... basically, has it come to a rest?
        /// </summary>
        public bool IsEdible()
        {
            return RigidBody && HasBallRested() && HandDistanceInteractable.enabled;
        }

        /// <summary>
        /// The "point of no return" for the ball, since creature has started her eating animation. Don't allow grabbing, etc.
        /// </summary>
        public void PrepareForEating(Vector3 targetBallPosition)
        {
            BallState = BallStatus.Eaten;
            m_eatStartCooldown = 0.0f;
            StopAllCoroutines();
            SfxBallLoop.SetVolume(0f);

            // disable physics & collision, take manual control
            RigidBody.isKinematic = true;
            RigidBody.useGravity = false;
            HandDistanceInteractable.enabled = false;
            HandGrabInteractable.enabled = false;
        }

        public void SetState(BallStatus bState)
        {
            BallState = bState;

            if (BallState is BallStatus.Hidden or BallStatus.Unavailable)
            {
                if (MultiToy.Instance.IsFlashlightActive())
                {
                    SfxBallLoop.ResetVolume();
                }
                else
                {
                    SfxBallLoop.ResetVolume();
                }
            }

            if (BallState == BallStatus.Released)
            {
                SfxBallLoop.Stop();
            }

            HandDistanceInteractable.enabled = BallState != BallStatus.Unavailable;
            HandGrabInteractable.enabled = HandDistanceInteractable.enabled;
        }

        /// <summary>
        /// This is primarily to decide if Oppy was shot directly.
        /// </summary>
        private void OnCollisionEnter(Collision collision)
        {
            // make sure this was an intentional shot, as opposed to a light graze
            var impactNormal = collision.GetContact(0).normal;
            var velocity = RigidBody.velocity;

            // velocity can be inverted, since physics is polled at a different rate
            // to account for this, accept either velocity (before or after collision)
            var impactDot = Mathf.Abs(Vector3.Dot(impactNormal, velocity.normalized));
            var intentionalHit = impactDot > 0.7f;
            LayerMask roomObjects = LayerMask.GetMask("Default");

            SfxBallBounce.SetVolume(impactDot);
            SfxBallBounce.Play();

            if (m_directOppyShot && collision.collider.gameObject.layer == LayerMask.NameToLayer("Oppy") && intentionalHit)
            {
                if (collision.collider.transform.parent && collision.collider.transform.parent.GetComponent<VirtualPet>())
                {
                    collision.collider.transform.parent.GetComponent<VirtualPet>().ShotOppy();
                }
            }
            else if (impactDot > 0.4f && roomObjects == (roomObjects | (1 << collision.collider.gameObject.layer)))
            {
                var impactFX = Instantiate(PassthroughImpactFXprefab);
                impactFX.transform.position = collision.GetContact(0).point + impactNormal * 0.005f;
                impactFX.transform.rotation = Quaternion.LookRotation(-impactNormal);
                m_directOppyShot = false;
                if (velocity.magnitude >= 1.0f)
                {
                    SpawnImpactDebris(impactFX.transform.position, impactFX.transform.rotation);
                }
            }
            else
            {
                m_directOppyShot = false;
            }
        }

        /// <summary>
        /// When colliding with something, spawn little impact gems that play with the world.
        /// </summary>
        private void SpawnImpactDebris(Vector3 position, Quaternion impactSpace)
        {
            // spawn debris in a cone formation
            var debrisCount = Random.Range(4, 8);
            for (var i = 0; i < debrisCount; i++)
            {
                var angle = Mathf.Deg2Rad * Random.Range(35.0f, 55.0f);
                var localEjectDirection = new Vector3(Mathf.Cos(angle), 0, -Mathf.Sin(angle));
                localEjectDirection = Quaternion.Euler(0, 0, Random.Range(0.0f, 360.0f)) * localEjectDirection;
                var localEjectPosition = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), 0).normalized * 0.03f;
                localEjectPosition = impactSpace * localEjectPosition;
                var debrisInstance = Instantiate(DebrisPrefab, position + localEjectPosition, Quaternion.identity);
                debrisInstance.transform.localScale = Random.Range(0.2f, 0.5f) * Vector3.one;
                debrisInstance.GetComponent<Rigidbody>().AddForce(impactSpace * localEjectDirection * Random.Range(0.5f, 1.5f), ForceMode.Impulse);
                var randomTorque = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f));
                debrisInstance.GetComponent<Rigidbody>().AddTorque(randomTorque * Random.Range(1.0f, 2.0f), ForceMode.Impulse);

                // track the debris so we can delete some if too many spawn
                WorldBeyondManager.Instance.AddBallDebrisToWorld(debrisInstance);
            }
            WorldBeyondManager.Instance.DeleteOldDebris();
        }
    }
}
