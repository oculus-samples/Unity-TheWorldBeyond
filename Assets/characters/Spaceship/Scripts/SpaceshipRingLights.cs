// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
[CustomEditor(typeof(SpaceshipRingLights))]
[CanEditMultipleObjects]
public class SpaceshipRingLightsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Populate"))
        {
            foreach (var t in targets)
            {
                Populate(t as SpaceshipRingLights);
            }
        }
        DrawDefaultInspector();
        GUILayout.Space(EditorGUIUtility.singleLineHeight);
    }

    private void Populate(SpaceshipRingLights srl)
    {
        Undo.RegisterFullObjectHierarchyUndo(srl.gameObject, "Populate Ring Lights");
        for (int i = srl.transform.childCount - 1; i > 0; i--) // do not destroy base, index 0
        {
            DestroyImmediate(srl.transform.GetChild(i).gameObject);
        }

        srl.Lights = new Renderer[srl.NumLights];
        srl.Shafts = new Renderer[srl.NumLights];
        srl.Lights[0] = srl.BaseLight.GetComponent<Renderer>();
        srl.Shafts[0] = srl.BaseLight.transform.GetChild(0).GetComponent<Renderer>();
        for (int i = 1; i < srl.NumLights; i++)
        {
            GameObject l = Instantiate(srl.BaseLight, srl.transform);
            l.name = srl.BaseLight.name + (i + 1);
            l.transform.RotateAround(srl.transform.position, Vector3.up, (360f / srl.NumLights) * i);
            srl.Lights[i] = l.GetComponent<Renderer>();
            srl.Shafts[i] = l.transform.GetChild(0).GetComponent<Renderer>();
        }
    }
}
#endif

public class SpaceshipRingLights : MonoBehaviour
{
    public GameObject BaseLight;

    public int NumLights = 16;
    public Renderer[] Lights;
    public Renderer[] Shafts;

    [Range(0, 1)] public float Brightness = 1f;
    [Range(0, 1)] public float MaxShaftBrightness = 0.2f;
    public Color[] Colors = new Color[] { Color.red };

    public float OnDamp = 0.5f;
    public float OffDamp = 0.1f;
    [Range(0, 1)] public float OnThreshold = 0.9f;

    public enum RingLightMode { Off, On, Blink, Spin, Random, Dormant }

    public RingLightMode Mode = RingLightMode.On;
    public float Speed = 10f;
    public float SpinFrequency = 1f;

    private MaterialPropertyBlock[] m_matBlocks;

    private int m_colInd;
    private int m_brightInd;
    private int m_shaftColInd;
    private int m_shaftBrightInd;

    private bool m_blink;
    private float m_spin;
    private float m_random;
    private int[] m_randomOrder;
    private float[] m_randomVals;

    private bool[] m_states;
    private float[] m_vels;
    private float[] m_brightness;
    private float[] m_prevBrightness;
    private float m_prevBrightnessSlider;

    private RingLightMode m_prevMode;
    private float m_time;

    private void OnEnable()
    {
        m_colInd = Shader.PropertyToID("_EmissionColor");
        m_brightInd = Shader.PropertyToID("_Emission");
        m_shaftColInd = Shader.PropertyToID("_Color");
        m_shaftBrightInd = Shader.PropertyToID("_Brightness");

        m_states = new bool[NumLights];
        m_vels = new float[NumLights];
        m_brightness = new float[NumLights];
        m_prevBrightness = new float[NumLights];

        // mat blocks
        m_matBlocks = new MaterialPropertyBlock[Lights.Length];
        for (int i = 0; i < Lights.Length; i++)
        {
            m_matBlocks[i] = new MaterialPropertyBlock();
        }

        // random vals (evenly spaced)
        m_randomOrder = new int[Lights.Length];
        for (int i = 0; i < Lights.Length; i++)
        {
            m_randomOrder[i] = i;
        }
        m_randomOrder.Shuffle();
        m_randomVals = new float[Lights.Length];
        for (int i = 0; i < m_randomOrder.Length; i++)
        {
            m_randomVals[i] = ((float)m_randomOrder[i]) / Lights.Length;
        }

        ApplyToMaterials(true);
    }

    private void Update()
    {
        if (WorldBeyondManager.Instance._currentChapter < WorldBeyondManager.GameChapter.Ending)
        {
            return;
        }

        // on mode change
        if (m_prevMode != Mode)
        {
            m_time = 0f;

            // force lights when switching to dormant
            if (Mode == RingLightMode.Dormant)
                ApplyToMaterials(true);
        }

        m_time += Time.deltaTime * Speed;

        switch (Mode)
        {
            case RingLightMode.Off:
                TickOff();
                break;
            case RingLightMode.On:
                TickOn();
                break;
            case RingLightMode.Blink:
                TickBlink();
                break;
            case RingLightMode.Spin:
                TickSpin();
                break;
            case RingLightMode.Random:
                TickRandom();
                break;
        }
        m_prevMode = Mode;

        ApplyToMaterials();

        m_prevBrightnessSlider = Brightness;
    }

    private void TickOff()
    {
        for (int i = 0; i < Lights.Length; i++)
        {
            m_states[i] = false;
        }
    }

    private void TickOn()
    {
        for (int i = 0; i < Lights.Length; i++)
        {
            m_states[i] = true;
        }
    }

    private void TickBlink()
    {
        m_blink = 0f < Mathf.Sin(m_time);
        for (int i = 0; i < Lights.Length; i++)
        {
            m_states[i] = m_blink;
        }
    }

    private void TickSpin()
    {
        for (int i = 0; i < Lights.Length; i++)
        {
            m_spin = Mathf.Sin((m_time + i) * SpinFrequency) * 0.5f + 0.5f;
            m_spin = Mathf.Clamp01(m_spin);
            m_states[i] = m_spin > OnThreshold;
        }
    }

    private void TickRandom()
    {
        for (int i = 0; i < Lights.Length; i++)
        {
            m_random = Mathf.Sin(m_time + (m_randomVals[i] * Speed)) * 0.5f + 0.5f;
            m_random = Mathf.Clamp01(m_random);
            m_states[i] = m_random > OnThreshold;
        }
    }

    private void ApplyToMaterials(bool force = false)
    {
        if (Mode == RingLightMode.Dormant && !force)
            return;

        int m = 0;
        for (int i = 0; i < Lights.Length; i++)
        {
            m_brightness[i] = Mathf.SmoothDamp(m_brightness[i], m_states[i] ? 1f : 0, ref m_vels[i], m_states[i] ? OnDamp : OffDamp);

            if (Brightness != m_prevBrightnessSlider || m_prevBrightness[i] != m_brightness[i] || force)
            {
                m_matBlocks[i].Clear();

                // light
                m_matBlocks[i].SetColor(m_colInd, Colors[(m + 1) % Colors.Length]);
                m_matBlocks[i].SetFloat(m_brightInd, Brightness * m_brightness[i]);
                Lights[i].SetPropertyBlock(m_matBlocks[i]);

                // shaft
                m_matBlocks[i].SetColor(m_shaftColInd, Colors[(m + 1) % Colors.Length]);
                m_matBlocks[i].SetFloat(m_shaftBrightInd, Brightness * m_brightness[i] * MaxShaftBrightness);
                Shafts[i].SetPropertyBlock(m_matBlocks[i]);
            }

            m_prevBrightness[i] = m_brightness[i];
            m++;
        }
    }
}

public static class RingLightExtensions
{
    /// <summary>
    /// Shuffle the order of an IList of items
    /// </summary>
    private static System.Random rng = new System.Random();
    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}
