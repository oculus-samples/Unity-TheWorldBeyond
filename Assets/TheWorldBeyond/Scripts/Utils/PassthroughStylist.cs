// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using TheWorldBeyond.GameManagement;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheWorldBeyond.Utils
{
    /// <summary>
    /// A helper to manage blending Passthrough between different styles.
    /// </summary>
    public class PassthroughStylist : MonoBehaviour
    {

        // this is auto-populated with the one in the scene, if not declared explicitly
        // if there's more than one OVRPassthroughLayer, then you know what you're doing and should really be setting it in the inspector
        public OVRPassthroughLayer SceneOverlay = null;

        public struct PassthroughStyle
        {
            public PassthroughStyle(
              Color cameraClearColor,
              float passthroughOpacity,
              float passthroughContrast,
              float passthroughBrightness,
              float passthroughPosterize,
              bool passthroughEdgeEnabled,
              Color passthroughEdge,
              Color passthroughBlack,
              Color passthroughWhite)
            {
                CameraClearColor = cameraClearColor;
                PassthroughOpacity = passthroughOpacity;
                PassthroughContrast = passthroughContrast;
                PassthroughBrightness = passthroughBrightness;
                PassthroughPosterize = passthroughPosterize;
                PassthroughEdgeEnabled = passthroughEdgeEnabled;
                PassthroughEdge = passthroughEdge;
                PassthroughBlack = passthroughBlack;
                PassthroughWhite = passthroughWhite;
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

        private PassthroughStyle m_savedPassthroughStyle;

        public enum PassthroughColorSwatch
        {
            PassthroughEdge,
            PassthroughBlack,
            PassthroughWhite
        };

        private PassthroughColorSwatch m_currentEditingColor;
        private Gradient m_passthroughGradient;
        private GradientColorKey[] m_colorKey;
        private GradientAlphaKey[] m_alphaKey;

        // UI elements
        public TextMeshProUGUI TextOpacity;
        public TextMeshProUGUI TextBrightness;
        public TextMeshProUGUI TextContrast;
        public TextMeshProUGUI TextPosterize;
        public TextMeshProUGUI TextColorR;
        public TextMeshProUGUI TextColorG;
        public TextMeshProUGUI TextColorB;
        public TextMeshProUGUI TextColorA;
        public Toggle EdgeToggle;
        public Slider EdgeAlpha;
        public Slider ColorRed;
        public Slider ColorGreen;
        public Slider ColorBlue;
        public Slider ColorAlpha;
        public Image EdgeColorSwatch;
        public Image GradientStartColorSwatch;
        public Image GradientEndColorSwatch;
        public Image ColorPickerSwatch;
        public Slider SliderOpacity;
        public Slider SliderBrightness;
        public Slider SliderContrast;
        public Slider SliderPosterize;

        public void Init(OVRPassthroughLayer scenePassthrough)
        {
            SceneOverlay = scenePassthrough;

            m_passthroughGradient = new Gradient();
            m_colorKey = new GradientColorKey[2];
            m_colorKey[0].time = 0.0f;
            m_colorKey[0].color = Color.black;
            m_colorKey[1].time = 1.0f;
            m_colorKey[1].color = Color.white;
            m_alphaKey = new GradientAlphaKey[2];
            m_alphaKey[0].alpha = 1.0f;
            m_alphaKey[0].time = 0.0f;
            m_alphaKey[1].alpha = 1.0f;
            m_alphaKey[1].time = 1.0f;
            m_passthroughGradient.SetKeys(m_colorKey, m_alphaKey);

            m_savedPassthroughStyle = new PassthroughStyle(
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

        private void Update()
        {
            if (SceneOverlay == null)
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
            m_savedPassthroughStyle.PassthroughOpacity = value;
            RestoreStylizedPassthrough();
            TextOpacity.text = string.Format("{0, 0:0.0}", value);
            SliderOpacity.SetValueWithoutNotify(value);
        }

        public void SetPassthroughContrast(float value)
        {
            m_savedPassthroughStyle.PassthroughContrast = value;
            RestoreStylizedPassthrough();
            TextContrast.text = string.Format("{0, 0:0.0}", value);
            SliderContrast.SetValueWithoutNotify(value);
        }

        public void SetPassthroughBrightness(float value)
        {
            m_savedPassthroughStyle.PassthroughBrightness = value;
            RestoreStylizedPassthrough();
            TextBrightness.text = string.Format("{0, 0:0.0}", value);
            SliderBrightness.SetValueWithoutNotify(value);
        }

        public void SetPassthroughPosterize(float value)
        {
            m_savedPassthroughStyle.PassthroughPosterize = value;
            RestoreStylizedPassthrough();
            TextPosterize.text = string.Format("{0, 0:0.0}", value);
            SliderPosterize.SetValueWithoutNotify(value);
        }

        public void SetCurrentEditingColor(int colorSwatch)
        {
            m_currentEditingColor = (PassthroughColorSwatch)colorSwatch;
            // update sliders
            var selectedColor = Color.white;
            switch (m_currentEditingColor)
            {
                case PassthroughColorSwatch.PassthroughBlack:
                    selectedColor = m_savedPassthroughStyle.PassthroughBlack;
                    break;
                case PassthroughColorSwatch.PassthroughEdge:
                    selectedColor = m_savedPassthroughStyle.PassthroughEdge;
                    break;
                case PassthroughColorSwatch.PassthroughWhite:
                    selectedColor = m_savedPassthroughStyle.PassthroughWhite;
                    break;
            }
            ColorRed.value = selectedColor.r;
            ColorGreen.value = selectedColor.g;
            ColorBlue.value = selectedColor.b;
            ColorAlpha.value = selectedColor.a;
            ColorPickerSwatch.color = selectedColor;
        }

        public void SetSelectedColorRed(float value)
        {
            UpdateColorChannel(0, value);
            RestoreStylizedPassthrough();
            TextColorR.text = string.Format("{0,0:0}", value * 255);
        }
        public void SetSelectedColorGreen(float value)
        {
            UpdateColorChannel(1, value);
            RestoreStylizedPassthrough();
            TextColorG.text = string.Format("{0,0:0}", value * 255);
        }
        public void SetSelectedColorBlue(float value)
        {
            UpdateColorChannel(2, value);
            RestoreStylizedPassthrough();
            TextColorB.text = string.Format("{0,0:0}", value * 255);
        }
        public void SetSelectedColorAlpha(float value)
        {
            UpdateColorChannel(3, value);
            RestoreStylizedPassthrough();
            EdgeAlpha.value = value;
            TextColorA.text = string.Format("{0,0:0}", value * 255);
        }

        private void UpdateColorChannel(int channel, float value)
        {
            //TODO do this more elegantly
            var selectedColor = m_savedPassthroughStyle.PassthroughEdge;
            if (m_currentEditingColor == PassthroughColorSwatch.PassthroughBlack)
            {
                selectedColor = m_savedPassthroughStyle.PassthroughBlack;
            }
            else if (m_currentEditingColor == PassthroughColorSwatch.PassthroughWhite)
            {
                selectedColor = m_savedPassthroughStyle.PassthroughWhite;
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
            switch (m_currentEditingColor)
            {
                case PassthroughColorSwatch.PassthroughEdge:
                    m_savedPassthroughStyle.PassthroughEdge = selectedColor;
                    EdgeColorSwatch.color = selectedColor;
                    break;
                case PassthroughColorSwatch.PassthroughBlack:
                    m_savedPassthroughStyle.PassthroughBlack = selectedColor;
                    GradientStartColorSwatch.color = selectedColor;
                    break;
                case PassthroughColorSwatch.PassthroughWhite:
                    m_savedPassthroughStyle.PassthroughWhite = selectedColor;
                    GradientEndColorSwatch.color = selectedColor;
                    break;
            }
            ColorPickerSwatch.color = selectedColor;
        }

        public void SetEdgeRendering(bool doEnable)
        {
            m_savedPassthroughStyle.PassthroughEdgeEnabled = doEnable;
            RestoreStylizedPassthrough();
        }

        public void ShowStylizedPassthrough(PassthroughStyle newStyle, float fadeTime = 0.5f)
        {
            StopAllCoroutines();
            _ = StartCoroutine(SetTargetPassthroughStyle(newStyle, fadeTime));
        }

        public void ForcePassthroughStyle(PassthroughStyle newStyle)
        {
            SceneOverlay.edgeColor = newStyle.PassthroughEdge;
            SceneOverlay.textureOpacity = newStyle.PassthroughOpacity;
            SceneOverlay.colorMapEditorContrast = newStyle.PassthroughContrast;
            SceneOverlay.colorMapEditorBrightness = newStyle.PassthroughBrightness;
            SceneOverlay.colorMapEditorPosterize = newStyle.PassthroughPosterize;

            var blendedStart = newStyle.PassthroughBlack;
            var blendedEnd = newStyle.PassthroughWhite;
            m_colorKey[0].color = blendedStart;
            m_alphaKey[0].alpha = blendedStart.a;
            m_colorKey[1].color = blendedEnd;
            m_alphaKey[1].alpha = blendedEnd.a;
            m_passthroughGradient.SetKeys(m_colorKey, m_alphaKey);
            SceneOverlay.colorMapEditorGradient = m_passthroughGradient;
            SceneOverlay.edgeRenderingEnabled = newStyle.PassthroughEdgeEnabled;
        }

        private IEnumerator SetTargetPassthroughStyle(PassthroughStyle newStyle, float lerpTime)
        {
            // first, grab the current style
            var camClearColor = WorldBeyondManager.Instance ? WorldBeyondManager.Instance.MainCamera.backgroundColor : Color.clear;
            var currentStyle = new PassthroughStyle(
                camClearColor,
                SceneOverlay.textureOpacity,
                SceneOverlay.colorMapEditorContrast,
                SceneOverlay.colorMapEditorBrightness,
                SceneOverlay.colorMapEditorPosterize,
                SceneOverlay.edgeRenderingEnabled,
                SceneOverlay.edgeColor,
                SceneOverlay.colorMapEditorGradient.colorKeys[0].color,
                SceneOverlay.colorMapEditorGradient.colorKeys[1].color);

            // if the edge rendering bool has changed, handle that by fading to or from nothing first
            if (newStyle.PassthroughEdgeEnabled && !currentStyle.PassthroughEdgeEnabled)
            {
                SceneOverlay.edgeRenderingEnabled = true;
                currentStyle.PassthroughEdge = new Color(newStyle.PassthroughEdge.r, newStyle.PassthroughEdge.g, newStyle.PassthroughEdge.b, 0);
            }
            if (!newStyle.PassthroughEdgeEnabled && currentStyle.PassthroughEdgeEnabled)
            {
                newStyle.PassthroughEdge = new Color(currentStyle.PassthroughEdge.r, currentStyle.PassthroughEdge.g, currentStyle.PassthroughEdge.b, 0);
            }

            var timer = 0.0f;
            while (timer < lerpTime)
            {
                timer += Time.deltaTime;
                var lerpValue = Mathf.Clamp01(timer / lerpTime);
                // then, blend all the things
                SceneOverlay.edgeColor = Color.Lerp(currentStyle.PassthroughEdge, newStyle.PassthroughEdge, lerpValue);
                SceneOverlay.textureOpacity = Mathf.Lerp(currentStyle.PassthroughOpacity, newStyle.PassthroughOpacity, lerpValue);
                SceneOverlay.colorMapEditorContrast = Mathf.Lerp(currentStyle.PassthroughContrast, newStyle.PassthroughContrast, lerpValue);
                SceneOverlay.colorMapEditorBrightness = Mathf.Lerp(currentStyle.PassthroughBrightness, newStyle.PassthroughBrightness, lerpValue);
                SceneOverlay.colorMapEditorPosterize = Mathf.Lerp(currentStyle.PassthroughPosterize, newStyle.PassthroughPosterize, lerpValue);

                var blendedStart = Color.Lerp(currentStyle.PassthroughBlack, newStyle.PassthroughBlack, lerpValue);
                var blendedEnd = Color.Lerp(currentStyle.PassthroughWhite, newStyle.PassthroughWhite, lerpValue);
                m_colorKey[0].color = blendedStart;
                m_alphaKey[0].alpha = blendedStart.a;
                m_colorKey[1].color = blendedEnd;
                m_alphaKey[1].alpha = blendedEnd.a;
                m_passthroughGradient.SetKeys(m_colorKey, m_alphaKey);
                SceneOverlay.colorMapEditorGradient = m_passthroughGradient;
                if (timer >= lerpTime)
                {
                    if (!newStyle.PassthroughEdgeEnabled)
                    {
                        SceneOverlay.edgeRenderingEnabled = false;
                    }
                }
                yield return null;
            }
        }

        public void RestoreStylizedPassthrough(float lerpTime = 0.0f)
        {
            ShowStylizedPassthrough(m_savedPassthroughStyle, lerpTime);
        }

        public void ResetPassthrough(float lerpTime = 0.5f)
        {
            var defaultStyle = new PassthroughStyle(
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
            return SceneOverlay.edgeColor;
        }

        public void KillCoroutines()
        {
            StopAllCoroutines();
        }
    }
}
