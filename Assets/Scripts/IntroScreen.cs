using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IntroScreen : MonoBehaviour
{
    public TextMeshProUGUI debugText;

    private void Start()
    {
        string buildInfo = Application.version;
        buildInfo += SanctuaryExperience.Instance.IsGreyPassthrough() ? " for Quest" : " in color";
        debugText.text = buildInfo;
    }

    private void Update()
    {
        // only show the debug text if a thumbstick is pressed
        bool thumbstickPressed = OVRInput.Get(OVRInput.RawButton.RThumbstick) || OVRInput.Get(OVRInput.RawButton.LThumbstick);
        debugText.enabled = thumbstickPressed;
    }
}
