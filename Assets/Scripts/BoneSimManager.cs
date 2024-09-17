// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

public class BoneSimManager : MonoBehaviour
{
    public int EditorFrameRate = 72;

    public BoneSim[] BoneSims;

    private void Awake()
    {
        for (int i = 0; i < BoneSims.Length; i++)
        {
            BoneSims[i].OrderedEvaluation = true;
        }
    }

    private void Start()
    {
        if (Application.isEditor)
        {
            Application.targetFrameRate = EditorFrameRate;
        }
    }

    private void OnEnable()
    {
        for (int i = 0; i < BoneSims.Length; i++)
        {
            if (BoneSims[i].isActiveAndEnabled)
            {
                BoneSims[i].OrderedEvaluation = true;
                BoneSims[i].Init();
            }
        }
    }

    private void OnDisable()
    {
        for (int i = 0; i < BoneSims.Length; i++)
        {
            if (BoneSims[i].isActiveAndEnabled)
            {
                BoneSims[i].OrderedEvaluation = false;
            }
        }
    }

    private void LateUpdate()
    {
        for (int i = 0; i < BoneSims.Length; i++)
        {
            if (BoneSims[i].isActiveAndEnabled)
            {
                BoneSims[i].Tick();
            }
        }
    }
}
