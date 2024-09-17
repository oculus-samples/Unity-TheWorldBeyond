// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A helper to manage blending Passthrough between different styles.
/// </summary>
public class PassthroughStylist : MonoBehaviour
{

    // this is auto-populated with the one in the scene, if not declared explicitly
    // if there's more than one OVRPassthroughLayer, then you know what you're doing and should really be setting it in the inspector
    public OVRPassthroughLayer sceneOverlay = null;

    public struct PassthroughStyle
    {
        public PassthroughStyle(
          Color _cameraClearColor,
          float _passthroughOpacity,
          float _passthroughContrast,
          float _passthroughBrightness,
          float _passthroughPosterize,
          bool _passthroughEdgeEnabled,
          Color _passthroughEdge,
          Color _passthroughBlack,
          Color _passthroughWhite)
        {
            CameraClearColor = _cameraClearColor;
            PassthroughOpacity = _passthroughOpacity;
            PassthroughContrast = _passthroughContrast;
            PassthroughBrightness = _passthroughBrightness;
            PassthroughPosterize = _passthroughPosterize;
            PassthroughEdgeEnabled = _passthroughEdgeEnabled;
            PassthroughEdge = _passthroughEdge;
            PassthroughBlack = _passthroughBlack;
            PassthroughWhite = _passthroughWhite;
        }

        public Color CameraClearColor;
        public float PassthroughOpacity;
        public float PassthroughContrast;
        public float PassthroughBrightness;
        public float PassthroughPosterize;
        public bool PassthroughEdgeEnabled;
        public Color PassthroughEdge;
        // Lerping between Gradients is a bit more complicated, since key counts can be different
        // for this script, only support start and end colors
        public Color PassthroughBlack;
        public Color PassthroughWhite;
    }
    PassthroughStyle savedPassthroughStyle;

    public enum PassthroughColorSwatch
    {
        PassthroughEdge,
        PassthroughBlack,
        PassthroughWhite
    };
    PassthroughColorSwatch currentEditingColor;

    Gradient PassthroughGradient;
    GradientColorKey[] colorKey;
    GradientAlphaKey[] alphaKey;

    // UI elements
    public TextMeshProUGUI textOpacity;
    public TextMeshProUGUI textBrightness;
    public TextMeshProUGUI textContrast;
    public TextMeshProUGUI textPosterize;
    public TextMeshProUGUI textColorR;
    public TextMeshProUGUI textColorG;
    public TextMeshProUGUI textColorB;
    public TextMeshProUGUI textColorA;
    public Toggle edgeToggle;
    public Slider edgeAlpha;
    public Slider colorRed;
    public Slider colorGreen;
    public Slider colorBlue;
    public Slider colorAlpha;
    public Image edgeColorSwatch;
    public Image gradientStartColorSwatch;
    public Image gradientEndColorSwatch;
    public Image colorPickerSwatch;
    public Slider sliderOpacity;
    public Slider sliderBrightness;
    public Slider sliderContrast;
    public Slider sliderPosterize;

    public void Init(OVRPassthroughLayer scenePassthrough)
    {
        sceneOverlay = scenePassthrough;

        PassthroughGradient = new Gradient();
        colorKey = new GradientColorKey[2];
        colorKey[0].time = 0.0f;
        colorKey[0].color = Color.black;
        colorKey[1].time = 1.0f;
        colorKey[1].color = Color.white;
        alphaKey = new GradientAlphaKey[2];
        alphaKey[0].alpha = 1.0f;
        alphaKey[0].time = 0.0f;
        alphaKey[1].alpha = 1.0f;
        alphaKey[1].time = 1.0f;
        PassthroughGradient.SetKeys(colorKey, alphaKey);

        savedPassthroughStyle = new PassthroughStyle(
            new Color(0, 0, 0, 0),
            1.0f,
            0.0f,
            0.0f,
            0.0f,
            false,
            Color.white,
            Color.black,
            Color.white);
    }

    void Update()
    {
        if (sceneOverlay == null)
        {
            Debug.Log("No OVROverlay found in scene");
            return;
        }
    }

    public void SetCameraBackgroundColor(Color targetColor, float lerpTime)
    {
        StopAllCoroutines();
    }

    public void SetPassthroughOpacity(float value)
    {
        savedPassthroughStyle.PassthroughOpacity = value;
        RestoreStylizedPassthrough();
        textOpacity.text = string.Format("{0, 0:0.0}", value);
        sliderOpacity.SetValueWithoutNotify(value);
    }

    public void SetPassthroughContrast(float value)
    {
        savedPassthroughStyle.PassthroughContrast = value;
        RestoreStylizedPassthrough();
        textContrast.text = string.Format("{0, 0:0.0}", value);
        sliderContrast.SetValueWithoutNotify(value);
    }

    public void SetPassthroughBrightness(float value)
    {
        savedPassthroughStyle.PassthroughBrightness = value;
        RestoreStylizedPassthrough();
        textBrightness.text = string.Format("{0, 0:0.0}", value);
        sliderBrightness.SetValueWithoutNotify(value);
    }

    public void SetPassthroughPosterize(float value)
    {
        savedPassthroughStyle.PassthroughPosterize = value;
        RestoreStylizedPassthrough();
        textPosterize.text = string.Format("{0, 0:0.0}", value);
        sliderPosterize.SetValueWithoutNotify(value);
    }

    public void SetCurrentEditingColor(int colorSwatch)
    {
        currentEditingColor = (PassthroughColorSwatch)colorSwatch;
        // update sliders
        Color selectedColor = Color.white;
        switch (currentEditingColor)
        {
            case PassthroughColorSwatch.PassthroughBlack:
                selectedColor = savedPassthroughStyle.PassthroughBlack;
                break;
            case PassthroughColorSwatch.PassthroughEdge:
                selectedColor = savedPassthroughStyle.PassthroughEdge;
                break;
            case PassthroughColorSwatch.PassthroughWhite:
                selectedColor = savedPassthroughStyle.PassthroughWhite;
                break;
        }
        colorRed.value = selectedColor.r;
        colorGreen.value = selectedColor.g;
        colorBlue.value = selectedColor.b;
        colorAlpha.value = selectedColor.a;
        colorPickerSwatch.color = selectedColor;
    }

    public void SetSelectedColorRed(float value)
    {
        UpdateColorChannel(0, value);
        RestoreStylizedPassthrough();
        textColorR.text = string.Format("{0,0:0}", value * 255);
    }
    public void SetSelectedColorGreen(float value)
    {
        UpdateColorChannel(1, value);
        RestoreStylizedPassthrough();
        textColorG.text = string.Format("{0,0:0}", value * 255);
    }
    public void SetSelectedColorBlue(float value)
    {
        UpdateColorChannel(2, value);
        RestoreStylizedPassthrough();
        textColorB.text = string.Format("{0,0:0}", value * 255);
    }
    public void SetSelectedColorAlpha(float value)
    {
        UpdateColorChannel(3, value);
        RestoreStylizedPassthrough();
        edgeAlpha.value = value;
        textColorA.text = string.Format("{0,0:0}", value * 255);
    }

    void UpdateColorChannel(int channel, float value)
    {
        //TODO do this more elegantly
        Color selectedColor = savedPassthroughStyle.PassthroughEdge;
        if (currentEditingColor == PassthroughColorSwatch.PassthroughBlack)
        {
            selectedColor = savedPassthroughStyle.PassthroughBlack;
        }
        else if (currentEditingColor == PassthroughColorSwatch.PassthroughWhite)
        {
            selectedColor = savedPassthroughStyle.PassthroughWhite;
        }
        switch (channel)
        {
            case 0:
                selectedColor = new Color(value, selectedColor.g, selectedColor.b, selectedColor.a);
                break;
            case 1:
                selectedColor = new Color(selectedColor.r, value, selectedColor.b, selectedColor.a);
                break;
            case 2:
                selectedColor = new Color(selectedColor.r, selectedColor.g, value, selectedColor.a);
                break;
            case 3:
                selectedColor = new Color(selectedColor.r, selectedColor.g, selectedColor.b, value);
                break;
        }
        switch (currentEditingColor)
        {
            case PassthroughColorSwatch.PassthroughEdge:
                savedPassthroughStyle.PassthroughEdge = selectedColor;
                edgeColorSwatch.color = selectedColor;
                break;
            case PassthroughColorSwatch.PassthroughBlack:
                savedPassthroughStyle.PassthroughBlack = selectedColor;
                gradientStartColorSwatch.color = selectedColor;
                break;
            case PassthroughColorSwatch.PassthroughWhite:
                savedPassthroughStyle.PassthroughWhite = selectedColor;
                gradientEndColorSwatch.color = selectedColor;
                break;
        }
        colorPickerSwatch.color = selectedColor;
    }

    public void SetEdgeRendering(bool doEnable)
    {
        savedPassthroughStyle.PassthroughEdgeEnabled = doEnable;
        RestoreStylizedPassthrough();
    }

    public void ShowStylizedPassthrough(PassthroughStyle newStyle, float fadeTime = 0.5f)
    {
        StopAllCoroutines();
        StartCoroutine(SetTargetPassthroughStyle(newStyle, fadeTime));
    }

    public void ForcePassthroughStyle(PassthroughStyle newStyle)
    {
        sceneOverlay.edgeColor = newStyle.PassthroughEdge;
        sceneOverlay.textureOpacity = newStyle.PassthroughOpacity;
        sceneOverlay.colorMapEditorContrast = newStyle.PassthroughContrast;
        sceneOverlay.colorMapEditorBrightness = newStyle.PassthroughBrightness;
        sceneOverlay.colorMapEditorPosterize = newStyle.PassthroughPosterize;

        Color blendedStart = newStyle.PassthroughBlack;
        Color blendedEnd = newStyle.PassthroughWhite;
        colorKey[0].color = blendedStart;
        alphaKey[0].alpha = blendedStart.a;
        colorKey[1].color = blendedEnd;
        alphaKey[1].alpha = blendedEnd.a;
        PassthroughGradient.SetKeys(colorKey, alphaKey);
        sceneOverlay.colorMapEditorGradient = PassthroughGradient;
        sceneOverlay.edgeRenderingEnabled = newStyle.PassthroughEdgeEnabled;
    }

    IEnumerator SetTargetPassthroughStyle(PassthroughStyle newStyle, float lerpTime)
    {
        // first, grab the current style
        Color camClearColor = WorldBeyondManager.Instance ? WorldBeyondManager.Instance._mainCamera.backgroundColor : Color.clear;
        PassthroughStyle currentStyle = new PassthroughStyle(
            camClearColor,
            sceneOverlay.textureOpacity,
            sceneOverlay.colorMapEditorContrast,
            sceneOverlay.colorMapEditorBrightness,
            sceneOverlay.colorMapEditorPosterize,
            sceneOverlay.edgeRenderingEnabled,
            sceneOverlay.edgeColor,
            sceneOverlay.colorMapEditorGradient.colorKeys[0].color,
            sceneOverlay.colorMapEditorGradient.colorKeys[1].color);

        // if the edge rendering bool has changed, handle that by fading to or from nothing first
        if (newStyle.PassthroughEdgeEnabled && !currentStyle.PassthroughEdgeEnabled)
        {
            sceneOverlay.edgeRenderingEnabled = true;
            currentStyle.PassthroughEdge = new Color(newStyle.PassthroughEdge.r, newStyle.PassthroughEdge.g, newStyle.PassthroughEdge.b, 0);
        }
        if (!newStyle.PassthroughEdgeEnabled && currentStyle.PassthroughEdgeEnabled)
        {
            newStyle.PassthroughEdge = new Color(currentStyle.PassthroughEdge.r, currentStyle.PassthroughEdge.g, currentStyle.PassthroughEdge.b, 0);
        }

        float timer = 0.0f;
        while (timer < lerpTime)
        {
            timer += Time.deltaTime;
            float lerpValue = Mathf.Clamp01(timer / lerpTime);
            // then, blend all the things
            sceneOverlay.edgeColor = Color.Lerp(currentStyle.PassthroughEdge, newStyle.PassthroughEdge, lerpValue);
            sceneOverlay.textureOpacity = Mathf.Lerp(currentStyle.PassthroughOpacity, newStyle.PassthroughOpacity, lerpValue);
            sceneOverlay.colorMapEditorContrast = Mathf.Lerp(currentStyle.PassthroughContrast, newStyle.PassthroughContrast, lerpValue);
            sceneOverlay.colorMapEditorBrightness = Mathf.Lerp(currentStyle.PassthroughBrightness, newStyle.PassthroughBrightness, lerpValue);
            sceneOverlay.colorMapEditorPosterize = Mathf.Lerp(currentStyle.PassthroughPosterize, newStyle.PassthroughPosterize, lerpValue);

            Color blendedStart = Color.Lerp(currentStyle.PassthroughBlack, newStyle.PassthroughBlack, lerpValue);
            Color blendedEnd = Color.Lerp(currentStyle.PassthroughWhite, newStyle.PassthroughWhite, lerpValue);
            colorKey[0].color = blendedStart;
            alphaKey[0].alpha = blendedStart.a;
            colorKey[1].color = blendedEnd;
            alphaKey[1].alpha = blendedEnd.a;
            PassthroughGradient.SetKeys(colorKey, alphaKey);
            sceneOverlay.colorMapEditorGradient = PassthroughGradient;
            if (timer >= lerpTime)
            {
                if (!newStyle.PassthroughEdgeEnabled)
                {
                    sceneOverlay.edgeRenderingEnabled = false;
                }
            }
            yield return null;
        }
    }

    public void RestoreStylizedPassthrough(float lerpTime = 0.0f)
    {
        ShowStylizedPassthrough(savedPassthroughStyle, lerpTime);
    }

    public void ResetPassthrough(float lerpTime = 0.5f)
    {
        PassthroughStyle defaultStyle = new PassthroughStyle(
            new Color(0, 0, 0, 0),
            1.0f,
            0.0f,
            0.0f,
            0.0f,
            false,
            Color.white,
            Color.black,
            Color.white);
        ShowStylizedPassthrough(defaultStyle, lerpTime);
    }

    public Color GetEdgeColor()
    {
        return sceneOverlay.edgeColor;
    }

    public void KillCoroutines()
    {
        StopAllCoroutines();
    }
}
