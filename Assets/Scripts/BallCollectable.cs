// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using Oculus.Interaction.HandGrab;

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
    public BallStatus _ballState { get; private set; } = BallStatus.Unavailable;

    // if the ball falls below this elevation for some reason, destroy it
    const float _killDepth = -50.0f;
    const float _absorbTimeRequired = 0.5f;
    const float _velocityRestThresh = 0.1f;
    float _absorbTimer = 0.0f;
    bool _absorbing = false;
    public int _wallID = -1;
    public Rigidbody _rigidBody;

    public GameObject _popFXprefab;
    public GameObject _passthroughImpactFXprefab;

    public SoundEntry _sfxBallLoop;
    public SoundEntry _sfxBallBounce;

    // disable this upon shooting, or else holding the trigger will immediately grab it
    public DistanceHandGrabInteractable _handDistanceInteractable;
    public HandGrabInteractable _handGrabInteractable;

    float _audioTimer = 0.0f;
    public float _shotTimer = 0.0f;
    const float _shotStrengthMultiplier = 1.0f;
    const float _ungrabbableShotStrengthMultiplier = 0.5f;

    // the object that "hides" the ball behind Passthrough
    public MeshRenderer _shellObject;
    public MeshRenderer _shadowObject;
    public ParticleSystem _hiddenFX;
    public GameObject _debrisPrefab;
    [HideInInspector]
    // after the ball was shot
    // this means the ball won't count towards "discovery" during the scripted section
    public bool _wasShot = false;

    // if player is intentionally shooting Oppy; set this to false upon any other collision
    private bool _directOppyShot = true;

    // by default, don't allow a ball to "count" towards Oppy finishing the game
    // this is to artificially gate progress
    public bool _ballAdvancesStory { get; private set; } = false;

    float _eatStartCooldown = 0.0f;

    private MeshRenderer _renderer;
    private ParticleSystemRenderer _particleSystemRenderer;

    public void ForceInvisible()
    {
        _renderer.enabled = false;
        _shadowObject.enabled = false;
        _particleSystemRenderer.enabled = false;
    }

    public void ForceVisible()
    {
        _renderer.enabled = true;
        _shadowObject.enabled = true;
        _particleSystemRenderer.enabled = true;
    }
    public void Start()
    {
        _renderer = GetComponent<MeshRenderer>();
        _particleSystemRenderer = GetComponent<ParticleSystemRenderer>();
        _sfxBallLoop.SetVolume(0.0f);
        _sfxBallLoop.Play();
        _shadowObject.transform.parent = null;
        _shadowObject.transform.rotation = Quaternion.Euler(90, 0, 0);
        _shadowObject.material.SetFloat("_ZTest", 4);
    }

    private void OnDestroy()
    {
        Destroy(_shadowObject.gameObject);
    }

    public void Update()
    {
        // fade audio when ball is available and flashlight is active
        float audioFadeDirection = ((_ballState == BallStatus.Available || _ballState == BallStatus.Hidden) && MultiToy.Instance.IsFlashlightActive()) ? 1.0f : -1.5f;
        _audioTimer += Time.deltaTime * audioFadeDirection;
        _audioTimer = Mathf.Clamp01(_audioTimer);
        _sfxBallLoop.SetVolume(_audioTimer);

        _rigidBody.useGravity = !_absorbing;
        _absorbing = false;

        if (_ballState == BallStatus.Released)
        {
            _shotTimer += Time.deltaTime;
        }
        else if (_ballState == BallStatus.Eaten)
        {
            // sometimes the Oppy eat animation gets interrupted
            // in that case, the ball needs to be reset so it can be absorbed by the player
            _eatStartCooldown += Time.deltaTime;
            if (_eatStartCooldown >= 2.0f)
            {
                _ballState = BallStatus.Available;
            }
        }
        else if (_ballState == BallStatus.Grabbed)
        {
            Vector3 targetPos = MultiToy.Instance.transform.position;
            if (!WorldBeyondManager.Instance._usingHands)
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
        float shadowDist = 100.0f;
        float closestHit = shadowDist;
        bool restedOnFurniture = false;
        RaycastHit[] roomboxHit = Physics.RaycastAll(transform.position, -Vector3.up, shadowDist, effectLayer);
        if (_ballState == BallStatus.Released && roomboxHit != null && roomboxHit.Length > 0)
        {
            _shadowObject.enabled = true;
            foreach (RaycastHit hitInfo in roomboxHit)
            {
                float thisDist = Vector3.Distance(transform.position, hitInfo.point);
                if (thisDist < closestHit)
                {
                    closestHit = thisDist;
                    _shadowObject.transform.position = hitInfo.point + Vector3.up * 0.001f;
                    float hoverDist = Vector3.Distance(transform.position, hitInfo.point);
                    _shadowObject.material.SetFloat("_Intensity", Mathf.Clamp01(1 - (hoverDist * 2)));

                    // if the ball has come to a rest on top of furniture, shoot in a direction towards the pet
                    // this avoids a bug where the ball is unreachable
                    if (_rigidBody && HasBallRested() && hitInfo.collider.gameObject.layer == LayerMask.NameToLayer("Furniture"))
                    {
                        restedOnFurniture = true;
                    }
                }
            }

            if (restedOnFurniture)
            {
                Vector3 ejectDirection = (WorldBeyondManager.Instance._pet.transform.position - transform.position).normalized + Vector3.up;
                Shoot(transform.position, ejectDirection.normalized * _ungrabbableShotStrengthMultiplier);
            }
        }
        else
        {
            _shadowObject.enabled = false;
        }

        if (WorldBeyondManager.Instance && transform.position.y < WorldBeyondManager.Instance.GetFloorHeight() + _killDepth)
        {
            ForceKill(Vector3.up);
        }
    }

    public void Absorbing(Vector3 rayFromHand)
    {
        if (_ballState == BallStatus.Eaten || _ballState == BallStatus.Grabbed)
        {
            return;
        }
        SetState(BallStatus.Available);
        _absorbTimer += Time.deltaTime;
        _rigidBody.isKinematic = false;
        _absorbing = true;
        _rigidBody.AddForce(-rayFromHand * 0.03f, ForceMode.Force);
    }

    public bool IsBallAbsorbed()
    {
        bool timeLimitHit = _absorbTimer >= _absorbTimeRequired;
        Vector3 vecToPet = transform.position - MultiToy.Instance.transform.position;
        bool closeEnough = Mathf.Sqrt(vecToPet.x * vecToPet.x + vecToPet.z * vecToPet.z) <= 0.3f;
        return (timeLimitHit || closeEnough);
    }

    public bool HasBallRested()
    {
        return _rigidBody.velocity.magnitude < _velocityRestThresh;
    }

    public void AbsorbBall()
    {
        SetState(BallStatus.Grabbed);
        _absorbTimer = 0.0f;
        _wallID = -1;
        _rigidBody.isKinematic = true;
        _rigidBody.velocity = Vector3.zero;
        MultiToy.Instance._flashlightAbsorb_1.Play();
        _sfxBallLoop.SetVolume(0);
        _shellObject.enabled = false;

        // only needed in SearchForOppy, so it renders above passthrough when absorbed
        if (GetComponent<MeshRenderer>())
        {
            GetComponent<MeshRenderer>().material.renderQueue = 3000;
        }
    }

    public void PlaceHiddenBall(Vector3 worldPos, int owningSurface)
    {
        Debug.Log("TWB placing ball: " + worldPos);
        _wallID = owningSurface;
        transform.position = worldPos;
        Vector3 facingCam = (WorldBeyondManager.Instance._mainCamera.transform.position - worldPos).normalized;
        Vector3 fwd = -Vector3.up;
        Vector3.OrthoNormalize(ref facingCam, ref fwd);
        transform.rotation = Quaternion.LookRotation(fwd, facingCam);
        _rigidBody.isKinematic = true;
        _shellObject.enabled = true;
        SetState(BallStatus.Hidden);
        WorldBeyondManager.Instance.AddBallToWorld(this);
        Debug.Log("TWB placed ball: " + transform.position);
    }

    public void Shoot(Vector3 pos, Vector3 shootForce)
    {
        SetState(BallStatus.Released); // will kill the ball loop sound too
        transform.position = pos; // set the origin to match target
        _rigidBody.isKinematic = false;
        _rigidBody.useGravity = true;
        _rigidBody.AddForce(shootForce * _shotStrengthMultiplier);
        Vector3 randomTorque = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f)).normalized;
        _rigidBody.AddTorque(randomTorque * 20);
        MultiToy.Instance._ballShoot_1.Play();
        WorldBeyondTutorial.Instance.HideMessage(WorldBeyondTutorial.TutorialMessage.ShootBall);
        bool canChaseBall = WorldBeyondManager.Instance._currentChapter >= WorldBeyondManager.GameChapter.OppyExploresReality;
        if (canChaseBall) WorldBeyondManager.Instance._pet.Listening(false);
        WorldBeyondManager.Instance._pet.HideThought();
        GetComponent<AudioSource>().Stop();
        _ballAdvancesStory = WorldBeyondManager.Instance._currentChapter >= WorldBeyondManager.GameChapter.TheGreatBeyond;
        _wasShot = true;
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
        if (WitConnector.Instance) WitConnector.Instance.WitSwitcher(false);
    }

    /// <summary>
    /// Entry point for properly destroying ball.
    /// </summary>
    public void ForceKill(Vector3 upVec)
    {
        StopAllCoroutines();
        Vector3 effectFwd = Vector3.Cross(Vector3.up, upVec);
        GameObject popFX = Instantiate(_popFXprefab);
        popFX.transform.position = transform.position;
        popFX.transform.rotation = Quaternion.LookRotation(effectFwd);
        popFX.GetComponent<ParticleSystem>().Play();
        _sfxBallLoop.Stop();
        WorldBeyondManager.Instance.RemoveBallFromWorld(this);
    }

    /// <summary>
    /// Is this ball eligible to be eaten/chased... basically, has it come to a rest?
    /// </summary>
    public bool IsEdible()
    {
        return (_rigidBody && HasBallRested() && _handDistanceInteractable.enabled);
    }

    /// <summary>
    /// The "point of no return" for the ball, since creature has started her eating animation. Don't allow grabbing, etc.
    /// </summary>
    public void PrepareForEating(Vector3 targetBallPosition)
    {
        _ballState = BallStatus.Eaten;
        _eatStartCooldown = 0.0f;
        StopAllCoroutines();
        _sfxBallLoop.SetVolume(0f);

        // disable physics & collision, take manual control
        _rigidBody.isKinematic = true;
        _rigidBody.useGravity = false;
        _handDistanceInteractable.enabled = false;
        _handGrabInteractable.enabled = false;
    }

    public void SetState(BallStatus bState)
    {
        _ballState = bState;

        if (_ballState == BallStatus.Hidden || _ballState == BallStatus.Unavailable)
        {
            if (MultiToy.Instance.IsFlashlightActive())
            {
                _sfxBallLoop.ResetVolume();
            }
            else
            {
                _sfxBallLoop.ResetVolume();
            }
        }

        if (_ballState == BallStatus.Released)
        {
            _sfxBallLoop.Stop();
        }

        _handDistanceInteractable.enabled = _ballState != BallStatus.Unavailable;
        _handGrabInteractable.enabled = _handDistanceInteractable.enabled;
    }

    /// <summary>
    /// This is primarily to decide if Oppy was shot directly.
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        // make sure this was an intentional shot, as opposed to a light graze
        Vector3 impactNormal = collision.GetContact(0).normal;
        Vector3 velocity = _rigidBody.velocity;

        // velocity can be inverted, since physics is polled at a different rate
        // to account for this, accept either velocity (before or after collision)
        float impactDot = Mathf.Abs(Vector3.Dot(impactNormal, velocity.normalized));
        bool intentionalHit = impactDot > 0.7f;
        LayerMask roomObjects = LayerMask.GetMask("Default");

        _sfxBallBounce.SetVolume(impactDot);
        _sfxBallBounce.Play();

        if (_directOppyShot && collision.collider.gameObject.layer == LayerMask.NameToLayer("Oppy") && intentionalHit)
        {
            if (collision.collider.transform.parent && collision.collider.transform.parent.GetComponent<VirtualPet>())
            {
                collision.collider.transform.parent.GetComponent<VirtualPet>().ShotOppy();
            }
        }
        else if (impactDot > 0.4f && roomObjects == (roomObjects | (1 << collision.collider.gameObject.layer)))
        {
            GameObject impactFX = Instantiate(_passthroughImpactFXprefab);
            impactFX.transform.position = collision.GetContact(0).point + impactNormal * 0.005f;
            impactFX.transform.rotation = Quaternion.LookRotation(-impactNormal);
            _directOppyShot = false;
            if (velocity.magnitude >= 1.0f)
            {
                SpawnImpactDebris(impactFX.transform.position, impactFX.transform.rotation);
            }
        }
        else
        {
            _directOppyShot = false;
        }
    }

    /// <summary>
    /// When colliding with something, spawn little impact gems that play with the world.
    /// </summary>
    void SpawnImpactDebris(Vector3 position, Quaternion impactSpace)
    {
        // spawn debris in a cone formation
        int debrisCount = Random.Range(4, 8);
        for (int i = 0; i < debrisCount; i++)
        {
            float angle = Mathf.Deg2Rad * Random.Range(35.0f, 55.0f);
            Vector3 localEjectDirection = new Vector3(Mathf.Cos(angle), 0, -Mathf.Sin(angle));
            localEjectDirection = Quaternion.Euler(0, 0, Random.Range(0.0f, 360.0f)) * localEjectDirection;
            Vector3 localEjectPosition = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), 0).normalized * 0.03f;
            localEjectPosition = impactSpace * localEjectPosition;
            GameObject debrisInstance = Instantiate(_debrisPrefab, position + localEjectPosition, Quaternion.identity);
            debrisInstance.transform.localScale = Random.Range(0.2f, 0.5f) * Vector3.one;
            debrisInstance.GetComponent<Rigidbody>().AddForce(impactSpace * localEjectDirection * Random.Range(0.5f, 1.5f), ForceMode.Impulse);
            Vector3 randomTorque = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f));
            debrisInstance.GetComponent<Rigidbody>().AddTorque(randomTorque * Random.Range(1.0f, 2.0f), ForceMode.Impulse);

            // track the debris so we can delete some if too many spawn
            WorldBeyondManager.Instance.AddBallDebrisToWorld(debrisInstance);
        }
        WorldBeyondManager.Instance.DeleteOldDebris();
    }
}
