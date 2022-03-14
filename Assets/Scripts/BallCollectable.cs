using UnityEngine;
using Oculus.Interaction;

public class BallCollectable : MonoBehaviour
{
    public enum BallStatus
    {
        Visible, // can be seen, but can't be grabbed: only for the very first ball
        Available, // is available to be picked up
        Grabbed, // when the player absorbs the ball, wth hands or controller
        Released, // has physics on, was just shot, is available for Oz to eat
        Eaten // when Oz has picked up the ball
    };
    public BallStatus _ballState { get; private set; } = BallStatus.Visible;

    const float _absorbTimeRequired = 0.5f;
    float _absorbTimer = 0.0f;
    bool _absorbing = false;
    public int _wallID = -1;
    public Rigidbody rigidBody;
    public Rigidbody _rigidBody;

    public GameObject _popFXprefab;
    public GameObject _passthroughImpactFXprefab;

    //[SerializeField] private AudioSource audio;
    public SoundEntry _sfxBallLoop;
    public SoundEntry _sfxBallBounce;

    // disable this upon shooting, or else holding the trigger will immediately grab it
    public Grabbable _handGrabbable;
    
    float _audioTimer = 0.0f;
    public float _shotTimer = 0.0f;

    // the object that "hides" the ball behind Passthrough
    public MeshRenderer _shellObject;
    public MeshRenderer _shadowObject;
    public ParticleSystem _hiddenFX;
    public GameObject _debrisPrefab;
    [HideInInspector]
    // after the ball was shot
    // this means the ball won't count towards "discovery" during the scripted section
    public bool _wasShot = false;

    // if player is intentionally shooting Oz; set this to false upon any other collision
    private bool _directOzShot = true;

    float _eatStartCooldown = 0.0f;

    public void Start()
    {
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
        if (Input.GetKeyUp(KeyCode.T))
        {
            SpawnImpactDebris(transform.position, transform.rotation);
        }
        // fade audio when ball is available and flashlight is active
        float audioFadeDirection = (_ballState == BallStatus.Available && MultiToy.Instance.IsFlashlightActive()) ? 1.0f : -1.5f;
        _audioTimer += Time.deltaTime * audioFadeDirection;
        _audioTimer = Mathf.Clamp01(_audioTimer);
        _sfxBallLoop.SetVolume(_audioTimer);

        rigidBody.useGravity = !_absorbing;
        _absorbing = false;

        if (_ballState == BallStatus.Released)
        {
            _shotTimer += Time.deltaTime;
        }
        else if (_ballState == BallStatus.Eaten)
        {
            // sometimes the Oz eat animation gets interrupted
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
            if (!SanctuaryExperience.Instance._usingHands)
            {
                transform.position = Vector3.Lerp(transform.position, targetPos, 0.2f);

                if (Vector3.Distance(transform.position, targetPos) <= 0.05f)
                {
                    SanctuaryExperience.Instance.RemoveBallFromWorld(this);
                    SanctuaryTutorial.Instance.HideMessage(SanctuaryTutorial.TutorialMessage.BallSearch);
                }
            }
        }

        // collide the shadow with room objects
        RaycastHit hitInfo;
        LayerMask effectLayer = LayerMask.GetMask("RoomBox", "Furniture");
        if (_ballState == BallStatus.Released && Physics.Raycast(transform.position, -Vector3.up, out hitInfo, 100.0f, 1 << effectLayer))
        {
            _shadowObject.enabled = true;
            _shadowObject.transform.position = hitInfo.point + Vector3.up * 0.001f;
            float hoverDist = Vector3.Distance(transform.position, hitInfo.point);
            _shadowObject.material.SetFloat("_Intensity", Mathf.Clamp01(1 - (hoverDist * 2)));
        }
        else
        {
            _shadowObject.enabled = false;
        }

        if (transform.position.y < SanctuaryExperience.Instance.GetFloorHeight() - 10.0f)
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
        _absorbTimer += Time.deltaTime;
        rigidBody.isKinematic = false;
        _absorbing = true;
        rigidBody.AddForce(-rayFromHand * 0.03f, ForceMode.Force);
    }

    public bool IsBallAbsorbed()
    {
        bool timeLimitHit = _absorbTimer >= _absorbTimeRequired;
        Vector3 vecToPet = transform.position - MultiToy.Instance.transform.position;
        bool closeEnough = Mathf.Sqrt(vecToPet.x * vecToPet.x + vecToPet.z * vecToPet.z) <= 0.3f;
        return (timeLimitHit || closeEnough);
    }

    public void AbsorbBall()
    {
        SetState(BallStatus.Grabbed);
        _absorbTimer = 0.0f;
        _wallID = -1;
        rigidBody.isKinematic = true;
        rigidBody.velocity = Vector3.zero;
        MultiToy.Instance._flashlightAbsorb_1.Play();
        _sfxBallLoop.SetVolume(0);
        _shellObject.enabled = false;

        // only needed in SearchForOz, so it renders above passthrough when absorbed
        if (GetComponent<MeshRenderer>())
        {
            GetComponent<MeshRenderer>().material.renderQueue = 3000;
        }
    }

    public void PlaceHiddenBall(Vector3 worldPos, int owningSurface)
    {
        transform.position = worldPos;
        Vector3 facingCam = (SanctuaryExperience.Instance._mainCamera.transform.position - worldPos).normalized;
        Vector3 fwd = -Vector3.up;
        Vector3.OrthoNormalize(ref facingCam, ref fwd);
        transform.rotation = Quaternion.LookRotation(fwd,facingCam);
        rigidBody.isKinematic = true;
        _shellObject.enabled = true;
        SetState(BallStatus.Available);
        SanctuaryExperience.Instance.AddBallToWorld(this);
    }

    public void Shoot(Vector3 pos, Vector3 shootForce, float lifeTime = -1.0f)
    {
        SetState(BallStatus.Released); // will kill the ball loop sound too
        transform.position = pos; // set the origin to match target
        rigidBody.isKinematic = false;
        rigidBody.useGravity = true;
        rigidBody.AddForce(shootForce);
        Vector3 randomTorque = new Vector3(UnityEngine.Random.Range(-1.0f, 1.0f), UnityEngine.Random.Range(-1.0f, 1.0f), UnityEngine.Random.Range(-1.0f, 1.0f)).normalized;
        rigidBody.AddTorque(randomTorque * 20);
        MultiToy.Instance._ballShoot_1.Play();
        SanctuaryTutorial.Instance.HideMessage(SanctuaryTutorial.TutorialMessage.ShootBall);
        SanctuaryExperience.Instance._pet.Listening(false);
        SanctuaryExperience.Instance._pet.HideThought();
        GetComponent<AudioSource>().Stop();
        _wasShot = true;
    }

    // this is only called when using hand tracking
    public void Grab()
    {
        MultiToy.Instance.GrabBall(this);
    }

    public void Throw()
    {
        SetState(BallStatus.Released);
        rigidBody.isKinematic = false;
        rigidBody.useGravity = true;
        SanctuaryTutorial.Instance.HideMessage(SanctuaryTutorial.TutorialMessage.ShootBall);
        WitConnector.Instance.WitSwitcher(false);
        GetComponent<AudioSource>().Stop();
        MultiToy.Instance.ThrewBall();
        _wasShot = true;
    }

    public void ForceKill(Vector3 upVec)
    {
        StopAllCoroutines();
        Vector3 effectFwd = Vector3.Cross(Vector3.up, upVec);
        GameObject popFX = Instantiate(_popFXprefab);
        popFX.transform.position = transform.position;
        popFX.transform.rotation = Quaternion.LookRotation(effectFwd);
        popFX.GetComponent<ParticleSystem>().Play();
        _sfxBallLoop.Stop();
        SanctuaryExperience.Instance.RemoveBallFromWorld(this);
    }

    // is this ball eligible to be eaten/chased
    // basically, has it come to a rest?
    public bool IsEdible()
    {
        return (rigidBody && rigidBody.velocity.magnitude < 0.1f && _handGrabbable.enabled);
    }

    // the eating animation will kill this object
    public void PrepareForEating(Vector3 targetBallPosition)
    {
        _ballState = BallStatus.Eaten;
        _eatStartCooldown = 0.0f;
        StopAllCoroutines();
        _sfxBallLoop.SetVolume(0f);

        // disable physics & collision, take manual control
        rigidBody.isKinematic = true;
        rigidBody.useGravity = false;
        _handGrabbable.enabled = false;
    }

    public void SetState(BallStatus bState)
    {
        _ballState = bState;

        if (_ballState == BallStatus.Available || _ballState == BallStatus.Visible)
        {
            if (MultiToy.Instance.IsFlashlightActive()){
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

        _handGrabbable.enabled = _ballState != BallStatus.Visible;
    }

    // this is primarily to decide if Oz was shot directly
    private void OnCollisionEnter(Collision collision)
    {
        // make sure this was an intentional shot, as opposed to a light graze
        Vector3 impactNormal = collision.GetContact(0).normal;
        Vector3 velocity = rigidBody.velocity;

        // velocity can be inverted, since physics is polled at a different rate
        // to account for this, accept either velocity (before or after collision)
        float impactDot = Mathf.Abs(Vector3.Dot(impactNormal, velocity.normalized));
        bool intentionalHit = impactDot > 0.7f;
        LayerMask roomObjects = LayerMask.GetMask("Default");
        
        // TBD - can take the velocity and lerp to set a volume range for the bouncing
        _sfxBallBounce.SetVolume(impactDot);
        _sfxBallBounce.Play();

        if (_directOzShot && collision.collider.gameObject.layer == LayerMask.NameToLayer("Oz") && intentionalHit)
        {
            if (collision.collider.transform.parent && collision.collider.transform.parent.GetComponent<SanctuaryPet>())
            {
                collision.collider.transform.parent.GetComponent<SanctuaryPet>().ShotOz();
            }
        }
        else if (impactDot > 0.4f && roomObjects == (roomObjects | (1 << collision.collider.gameObject.layer)))
        {
            GameObject impactFX = Instantiate(_passthroughImpactFXprefab);
            impactFX.transform.position = collision.GetContact(0).point + impactNormal * 0.005f;
            impactFX.transform.rotation = Quaternion.LookRotation(-impactNormal);
            _directOzShot = false;
            if (velocity.magnitude >= 1.0f)
            {
                SpawnImpactDebris(impactFX.transform.position, impactFX.transform.rotation);
            }
        }
        else
        {
            _directOzShot = false;
        }
    }

    public void OnHoverStart()
    {
        transform.localScale = Vector3.one * 0.11f;
    }

    public void OnHoverEnd()
    {
        transform.localScale = Vector3.one * 0.1f;
    }

    void SpawnImpactDebris(Vector3 position, Quaternion impactSpace)
    {
        // spawn debris in a cone formation
        int debrisCount = Random.Range(4, 8);
        for (int i=0;i<debrisCount;i++)
        {
            float angle = Mathf.Deg2Rad * Random.Range(35.0f, 55.0f);
            Vector3 localEjectDirection = new Vector3(Mathf.Cos(angle), 0, -Mathf.Sin(angle));
            localEjectDirection = Quaternion.Euler(0, 0, Random.Range(0.0f, 360.0f)) * localEjectDirection;
            Vector3 localEjectPosition = new Vector3(Random.Range(-1.0f,1.0f), Random.Range(-1.0f, 1.0f), 0).normalized * 0.03f;
            localEjectPosition = impactSpace * localEjectPosition;
            GameObject debrisInstance = Instantiate(_debrisPrefab, position + localEjectPosition, Quaternion.identity);
            debrisInstance.transform.localScale = Random.Range(0.2f, 0.5f) * Vector3.one;
            debrisInstance.GetComponent<Rigidbody>().AddForce(impactSpace * localEjectDirection * Random.Range(0.5f,1.5f), ForceMode.Impulse);
            Vector3 randomTorque = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f));
            debrisInstance.GetComponent<Rigidbody>().AddTorque(randomTorque * Random.Range(1.0f,2.0f), ForceMode.Impulse);

            // track the debris so we can delete some if too many spawn
            SanctuaryExperience.Instance.AddBallDebrisToWorld(debrisInstance);
        }
        SanctuaryExperience.Instance.DeleteOldDebris();
    }
}
