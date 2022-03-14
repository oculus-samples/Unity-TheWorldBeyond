using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SanctuaryTutorial : MonoBehaviour
{
    static public SanctuaryTutorial Instance = null;
    public Transform _canvasObject;
    public Image _labelBackground;
    public TextMeshProUGUI _tutorialText;
    bool _attachToView = false;

    public enum TutorialMessage
    {
        EnableFlashlight, // hands only
        BallSearch,
        ShootBall,
        AimWall, // hands only
        ShootWall,
        SwitchToy,
        NoBalls,
        ERROR_NoScene,
        None
    };
    public TutorialMessage _currentMessage { private set; get; } = TutorialMessage.BallSearch;

    private void Awake()
    {
        Instance = this;
        DisplayMessage(TutorialMessage.None);

        // ensure the UI renders above Passthrough and hands
        _labelBackground.material.renderQueue = 4500;
        _tutorialText.fontMaterial.renderQueue = 4501;
    }

    void Update()
    {
        UpdatePosition();
    }

    public void DisplayMessage(TutorialMessage message)
    {
        _canvasObject.gameObject.SetActive(message != TutorialMessage.None);

        AttachToView(message == TutorialMessage.ERROR_NoScene);

        switch (message)
        {
            case TutorialMessage.EnableFlashlight:
                _tutorialText.text = "Open palm outward to enable flashlight";
                break;
            case TutorialMessage.BallSearch:
                _tutorialText.text = SanctuaryExperience.Instance._usingHands ?
                    "Search around your room for energy balls. Make a fist to absorb them." :
                    "Search around your room for energy balls. Aim and hold Index Trigger to absorb them.";
                break;
            case TutorialMessage.NoBalls:
                _tutorialText.text = "You're out of energy balls... switch back to the flashlight to find more";
                break;
            case TutorialMessage.ShootBall:
                _tutorialText.text = SanctuaryExperience.Instance._usingHands ?
                    "Throw ball by opening fist" :
                    "Shoot balls with index trigger";
                break;
            case TutorialMessage.AimWall: // only used for hands
                _tutorialText.text = "With your palm facing up, aim at a wall";
                break;
            case TutorialMessage.ShootWall:
                _tutorialText.text = SanctuaryExperience.Instance._usingHands ?
                    "Close hand to open/close wall" :
                    "Shoot walls to open/close them";
                break;
            case TutorialMessage.SwitchToy:
                _tutorialText.text = "Use thumbstick left/right to switch toys";
                break;
            case TutorialMessage.ERROR_NoScene:
                _tutorialText.text = "Scene Info not detected. Enter Room Capture or try restarting The World Beyond.";
                break;
            case TutorialMessage.None:
                break;
        }
        _currentMessage = message;
    }

    public void HideMessage(TutorialMessage message)
    {
        if (_currentMessage == message)
        {
            _canvasObject.gameObject.SetActive(false);
        }
    }

    void AttachToView(bool doAttach)
    {
        _attachToView = doAttach;

        // for now, attaching to the view is a special case and only used when there is no Scene detected
        // the black fade sphere needs to render before the UI
        if (SanctuaryExperience.Instance)
        {
            SanctuaryExperience.Instance._fadeSphere.sharedMaterial.renderQueue = doAttach ? 4497 : 4999;
        }
    }

    void UpdatePosition()
    {
        Transform centerEye = SanctuaryExperience.Instance._mainCamera.transform;

        float smoothing = Mathf.SmoothStep(0.3f, 0.9f, Time.deltaTime / 50.0f);

        Vector3 targetPosition = _attachToView ? centerEye.position + centerEye.forward * 0.7f : SanctuaryExperience.Instance.GetControllingHand(19).position;
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothing);

        Quaternion targetRotation = Quaternion.LookRotation(transform.position - centerEye.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothing);
    }
}
