// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WorldBeyondTutorial : MonoBehaviour
{
    static public WorldBeyondTutorial Instance = null;
    public Canvas _canvasObject;
    public Image _labelBackground;
    public TextMeshProUGUI _tutorialText;
    // used for full-screen passthrough, only when player walks out of the room
    public MeshRenderer _passthroughSphere;
    bool _attachToView = false;
    // don't allow any other messages after this one, because the app is quitting
    bool _hitCriticalError = false;
    // when hitting a game-breaking error, quit the app after displaying the message
    const float _errorMessageDisplayTime = 5.0f;

    public enum TutorialMessage
    {
        EnableFlashlight, // hands only
        BallSearch,
        ShootBall,
        AimWall, // hands only
        ShootWall,
        SwitchToy,
        NoBalls,
        None,
        // Only Scene Error messages after this entry
        ERROR_USER_WALKED_OUTSIDE_OF_ROOM,    // runs every frame, to force on Passthrough. The only error that doesn't quit the app.
        ERROR_NO_SCENE_DATA,                  // by default, Room Setup is launched; if the user cancels, this message displays
        ERROR_NO_SCENE_DATA_LINK,             // same as above, but on PC, Room Setup doesn't launch; so error message is slightly different
        ERROR_USER_STARTED_OUTSIDE_OF_ROOM,   // user is outside of the room volume, likely from starting in a different guardian/room
        ERROR_NOT_ENOUGH_WALLS,               // fewer than 3 walls, or only non-walls discovered (e.g. user has only set up a desk)
        ERROR_TOO_MANY_WALLS,                 // a closed loop of walls was found, but there are other rooms/walls
        ERROR_ROOM_IS_OPEN,                   // walls don't form a closed loop
        ERROR_INTERSECTING_WALLS              // walls are intersecting
    };
    public TutorialMessage _currentMessage { private set; get; } = TutorialMessage.BallSearch;

    private void Awake()
    {
        Instance = this;
        DisplayMessage(TutorialMessage.None);

        // ensure the UI renders above Passthrough and hands
        _passthroughSphere.material.renderQueue = 4499;
        _labelBackground.material.renderQueue = 4500;
        _tutorialText.fontMaterial.renderQueue = 4501;

        _passthroughSphere.gameObject.SetActive(false);
    }

    void Update()
    {
        UpdatePosition();
    }

    public void UpdateMessageTextForInput()
    {
        DisplayMessage(_currentMessage);
    }

    public void DisplayMessage(TutorialMessage message)
    {
        if (_hitCriticalError)
        {
            return;
        }

        _canvasObject.gameObject.SetActive(message != TutorialMessage.None);

        _passthroughSphere.gameObject.SetActive(message == TutorialMessage.ERROR_USER_WALKED_OUTSIDE_OF_ROOM);

        AttachToView(message >= TutorialMessage.ERROR_USER_WALKED_OUTSIDE_OF_ROOM);

        _hitCriticalError = message >= TutorialMessage.ERROR_NO_SCENE_DATA;
        if (_hitCriticalError)
        {
            StartCoroutine(KillApp());
        }

        switch (message)
        {
            case TutorialMessage.EnableFlashlight:
                _tutorialText.text = "Open palm outward to enable flashlight";
                break;
            case TutorialMessage.BallSearch:
                _tutorialText.text = WorldBeyondManager.Instance._usingHands ?
                    "Search around your room for energy balls. Make a fist to grab them from afar." :
                    "Search around your room for energy balls. Aim and hold Index Trigger to absorb them.";
                break;
            case TutorialMessage.NoBalls:
                _tutorialText.text = "You're out of energy balls... switch back to the flashlight to find more";
                break;
            case TutorialMessage.ShootBall:
                _tutorialText.text = WorldBeyondManager.Instance._usingHands ?
                    "Shoot balls by opening fist" :
                    "Shoot balls with index trigger";
                break;
            case TutorialMessage.AimWall: // only used for hands
                _tutorialText.text = "With your palm facing up, aim at a wall";
                break;
            case TutorialMessage.ShootWall:
                _tutorialText.text = WorldBeyondManager.Instance._usingHands ?
                    "Close hand to open/close wall" :
                    "Shoot walls to open/close them";
                break;
            case TutorialMessage.SwitchToy:
                _tutorialText.text = "Use thumbstick left/right to switch toys";
                break;
            case TutorialMessage.None:
                break;
            case TutorialMessage.ERROR_USER_WALKED_OUTSIDE_OF_ROOM:
                _tutorialText.text = "Out of bounds. Please return to your room.";
                break;
            case TutorialMessage.ERROR_NO_SCENE_DATA:
                _tutorialText.text = "The World Beyond requires Scene data. Please run Space Setup in Settings.";
                break;
            case TutorialMessage.ERROR_NO_SCENE_DATA_LINK:
                _tutorialText.text = "The World Beyond requires Scene data. Please disable Link, then run Space Setup in Settings. Then enable Link and try again.";
                break;
            case TutorialMessage.ERROR_USER_STARTED_OUTSIDE_OF_ROOM:
                _tutorialText.text = "It appears you're outside of your room. Please enter your room and restart.";
                break;
            case TutorialMessage.ERROR_NOT_ENOUGH_WALLS:
                _tutorialText.text = "You haven't set up enough walls. Please run Room Setup in Settings.";
                break;
            case TutorialMessage.ERROR_TOO_MANY_WALLS:
                _tutorialText.text = "Somehow, you have more walls than you should. Please re-create your walls in Space Setup in Settings.";
                break;
            case TutorialMessage.ERROR_ROOM_IS_OPEN:
                _tutorialText.text = "The World Beyond requires a closed space. Please re-create your walls in Space Setup in Settings.";
                break;
            case TutorialMessage.ERROR_INTERSECTING_WALLS:
                _tutorialText.text = "It appears that some of your walls overlap. Please re-create your walls in Room Setup in Settings.";
                break;
        }
        _currentMessage = message;
    }

    public void ForceInvisible()
    {
        // Don't hide the popup if it's attached to your view
        if (_attachToView) return;
        _canvasObject.gameObject.SetActive(false);
    }

    public void ForceVisible()
    {
        if (_currentMessage != TutorialMessage.None)
        {
            _canvasObject.gameObject.SetActive(true);
        }
    }

    public void HideMessage(TutorialMessage message)
    {
        if (_currentMessage == message)
        {
            _canvasObject.gameObject.SetActive(false);
            _currentMessage = TutorialMessage.None;
        }
    }

    void AttachToView(bool doAttach)
    {
        _attachToView = doAttach;
        // snap it to the view
        if (doAttach)
        {
            UpdatePosition(false);
            ForceVisible();
        }

        // for now, attaching to the view is a special case and only used when there is no Scene detected
        // the black fade sphere needs to render before the UI
        if (WorldBeyondManager.Instance)
        {
            WorldBeyondManager.Instance._fadeSphere.sharedMaterial.renderQueue = doAttach ? 4497 : 4999;
        }
    }

    void UpdatePosition(bool useSmoothing = true)
    {
        Transform centerEye = WorldBeyondManager.Instance._mainCamera.transform;

        float smoothing = useSmoothing ? Mathf.SmoothStep(0.3f, 0.9f, Time.deltaTime / 50.0f) : 1.0f;

        Vector3 targetPosition = _attachToView ? centerEye.position + centerEye.forward * 0.7f : WorldBeyondManager.Instance.GetControllingHand(19).position;
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothing);

        Vector3 lookDir = transform.position - centerEye.position;
        if (lookDir.magnitude > Mathf.Epsilon && lookDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothing);
        }
    }

    IEnumerator KillApp()
    {
        yield return new WaitForSeconds(_errorMessageDisplayTime);
        Application.Quit();
    }
}
