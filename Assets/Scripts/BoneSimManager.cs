/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

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

    private void OnEnable ()
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

    private void LateUpdate ()
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
