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

public class SampleBullet : MonoBehaviour
{
    public GameObject _debrisPrefab;
    Rigidbody _rigidBody;
    AudioSource _bounceSound;

    private void Start()
    {
        _rigidBody = GetComponent<Rigidbody>();
        _bounceSound = GetComponent<AudioSource>();
    }

    public void OnCollisionEnter(Collision collision)
    {
        // make sure this was an intentional shot, as opposed to a light graze
        Vector3 impactNormal = collision.GetContact(0).normal;

        SpawnImpactDebris(collision.GetContact(0).point + impactNormal * 0.005f, Quaternion.LookRotation(-impactNormal));

        float impactDot = Mathf.Abs(Vector3.Dot(impactNormal, _rigidBody.velocity.normalized));
        if (impactDot > 0.7f)
        {
            _bounceSound.time = 0.0f;
            _bounceSound.Play();
        }
    }

    /// <summary>
    /// When colliding with something, spawn little impact gems that play with the world.
    /// </summary>
    void SpawnImpactDebris(Vector3 position, Quaternion impactSpace)
    {
        // spawn debris in a cone formation
        int debrisCount = Random.Range(2, 4);
        for (int i = 0; i < debrisCount; i++)
        {
            float angle = Mathf.Deg2Rad * Random.Range(35.0f, 55.0f);
            Vector3 localEjectDirection = new Vector3(Mathf.Cos(angle), 0, -Mathf.Sin(angle));
            localEjectDirection = Quaternion.Euler(0, 0, Random.Range(0.0f, 360.0f)) * localEjectDirection;
            Vector3 localEjectPosition = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), 0).normalized * 0.03f;
            localEjectPosition = impactSpace * localEjectPosition;
            GameObject debrisInstance = Instantiate(_debrisPrefab, position + localEjectPosition, Quaternion.identity);
            debrisInstance.transform.localScale = Random.Range(0.2f, 0.5f) * Vector3.one;
            debrisInstance.GetComponent<Rigidbody>().AddForce(impactSpace * localEjectDirection * Random.Range(0.5f, 1.5f), ForceMode.Impulse);
            Vector3 randomTorque = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f));
            debrisInstance.GetComponent<Rigidbody>().AddTorque(randomTorque * Random.Range(1.0f, 2.0f), ForceMode.Impulse);
            SelfDestruct selfDestruct = debrisInstance.AddComponent<SelfDestruct>();
            selfDestruct._selfDestructionTimer = 3.0f;
        }
    }
}
