using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ThoughtBubble : MonoBehaviour
{
    public TextMeshProUGUI thoughtText;
    public Transform mainDisplay;
    public Transform bubble1;
    public Transform bubble2;
    Vector3 b2Offset = Vector3.zero;
    float countdownTimer = 0.0f;
    public Transform ozHeadBone;
    public float scaleMultipier = 0.7f;

    private void Awake()
    {
        thoughtText.fontMaterial.renderQueue = 4501;
        b2Offset = bubble2.localPosition;
    }
    void Update()
    {
        ForceSizeUpdate();
        
        // animate things in code
        bubble1.transform.localRotation = Quaternion.Euler(0, 0, Time.time*10);
        bubble2.transform.localRotation = Quaternion.Euler(0, 0, -Time.time*15);

        bubble1.transform.localPosition = Vector3.right * Mathf.Sin(Time.time * 1.6f) * 0.005f;
        bubble2.transform.localPosition = b2Offset + Vector3.right * Mathf.Cos(Time.time * 1.5f) * 0.01f;
        
        mainDisplay.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time)*5);

        countdownTimer -= Time.deltaTime;
        if (countdownTimer <= 0.0f)
        {
            gameObject.SetActive(false);
        }
    }

    public void ForceSizeUpdate()
    {
        if (ozHeadBone)
        {
            transform.position = ozHeadBone.position - ozHeadBone.right * 0.3f;
        }

        Vector3 objFwd = transform.position - SanctuaryExperience.Instance._mainCamera.transform.position;

        // face camera
        transform.rotation = Quaternion.LookRotation(objFwd, Vector3.up);

        // keep uniform size on screen
        objFwd.y = 0;
        transform.localScale = Vector3.one * Mathf.Clamp(objFwd.magnitude * scaleMultipier, 0.8f, 4.0f);
    }

    public void UpdateText(string message, float thoughtDuration = 2)
    {
        countdownTimer = thoughtDuration;
        thoughtText.text = message;
    }
    public void ShowHint(float hintDuration = 5) {
        countdownTimer = hintDuration;
        thoughtText.text = "<color=#000000>Ask me to <color=#FF0000>come<color=#000000>, <color=#FF0000>jump <color=#000000>or<br> say <color=#FF0000>hi<color=#000000> to me";
    }
}
