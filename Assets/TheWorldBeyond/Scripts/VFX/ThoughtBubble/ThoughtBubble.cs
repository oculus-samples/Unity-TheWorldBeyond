// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using TMPro;

public class ThoughtBubble : MonoBehaviour
{
    public TextMeshProUGUI _thoughtText;
    public Transform _mainDisplay;
    public Transform _bubble1;
    public Transform _bubble2;
    Vector3 b2Offset = Vector3.zero;
    float countdownTimer = 0.0f;
    public Transform _oppyHeadBone;
    public float scaleMultipier = 0.7f;

    private void Awake()
    {
        _thoughtText.fontMaterial.renderQueue = 4501;
        b2Offset = _bubble2.localPosition;
    }
    void Update()
    {
        ForceSizeUpdate();

        // animate things in code
        _bubble1.transform.localRotation = Quaternion.Euler(0, 0, Time.time * 10);
        _bubble2.transform.localRotation = Quaternion.Euler(0, 0, -Time.time * 15);

        _bubble1.transform.localPosition = Vector3.right * Mathf.Sin(Time.time * 1.6f) * 0.005f;
        _bubble2.transform.localPosition = b2Offset + Vector3.right * Mathf.Cos(Time.time * 1.5f) * 0.01f;

        _mainDisplay.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time) * 5);

        countdownTimer -= Time.deltaTime;
        if (countdownTimer <= 0.0f)
        {
            gameObject.SetActive(false);
        }
    }

    public void ForceSizeUpdate()
    {
        if (_oppyHeadBone)
        {
            transform.position = _oppyHeadBone.position - _oppyHeadBone.right * 0.3f;
        }

        Vector3 objFwd = transform.position - WorldBeyondManager.Instance._mainCamera.transform.position;

        // face camera
        transform.rotation = Quaternion.LookRotation(objFwd, Vector3.up);

        // keep uniform size on screen
        objFwd.y = 0;
        transform.localScale = Vector3.one * Mathf.Clamp(objFwd.magnitude * scaleMultipier, 0.8f, 4.0f);
    }

    public void UpdateText(string message, float thoughtDuration = 2)
    {
        countdownTimer = thoughtDuration;
        _thoughtText.text = message;
    }

    public void ShowHint(float hintDuration = 5)
    {
        countdownTimer = hintDuration;
        _thoughtText.text = "<color=#000000>Ask me to <color=#FF0000>come<color=#000000>, <color=#FF0000>jump <color=#000000>or<br> say <color=#FF0000>hi<color=#000000> to me";
    }
}
