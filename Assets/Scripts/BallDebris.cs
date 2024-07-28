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

public class BallDebris : MonoBehaviour
{
    bool _dead = false;
    float _deathTimer = 0.5f;
    float _maxAbsorbStrength = 7.0f;
    public Rigidbody _rigidBody;

    void Update()
    {
        if (_dead)
        {
            // shrink into oblivion
            _deathTimer -= Time.deltaTime * 0.5f;
            if (_deathTimer <= 0.0f)
            {
                Destroy(this.gameObject);
            }
            else
            {
                transform.localScale = _deathTimer * Vector3.one;
            }
        }
    }

    public void Kill()
    {
        if (!_dead)
        {
            _deathTimer = transform.localScale.x;
            _dead = true;
        }
    }

    public void AddForce(Vector3 direction, float absorbScale)
    {
        _rigidBody.AddForce(direction * absorbScale * _maxAbsorbStrength, ForceMode.Force);
    }
}
