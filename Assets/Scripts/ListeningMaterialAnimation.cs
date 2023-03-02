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

public class ListeningMaterialAnimation : MonoBehaviour
{
    public float ScrollSpeed = -0.05F;
    public Color Color = Color.red;
    public float Intensity = 0.7f;
    private float _intensity = 0.7f;
    private Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
        rend.material.color = Color;
    }
    void Update()
    {
        // It might be good to only do this when in listening state
        float offset = Time.time * ScrollSpeed;
        rend.material.SetFloat("_ScrollAmount", offset);

        Vector3 objFwd = transform.position - WorldBeyondManager.Instance._mainCamera.transform.position;
        objFwd.y = 0;
        _intensity = Mathf.Clamp(Intensity * objFwd.magnitude,0.5f,3) ;
        rend.material.SetFloat("_Intensity", _intensity);
    }
}
