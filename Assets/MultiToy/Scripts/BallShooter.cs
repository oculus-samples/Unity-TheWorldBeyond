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

[System.Serializable]
public class BallShooter : WorldBeyondToy
{
    public override void ActionDown()
    {
        if (WorldBeyondManager.Instance._ballCount > 0)
        {
            ShootBall(transform.forward);
            WorldBeyondManager.Instance._ballCount--;
        }
        else
        {
            // play "no balls" sound
            if (WorldBeyondManager.Instance._currentChapter == WorldBeyondManager.GameChapter.TheGreatBeyond)
            {
                WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.NoBalls);
            }
        }
    }

    public void ShootBall(Vector3 ballDirection)
    {
        if (WorldBeyondManager.Instance && WorldBeyondManager.Instance._ballPrefab == null)
        {
            Debug.Log("TheWorldBeyond: no ball prefab found");
            return;
        }

        Vector3 ballPos = transform.position + transform.forward * 0.1f;
        GameObject newBall = Instantiate(WorldBeyondManager.Instance._ballPrefab, ballPos, Quaternion.identity);
        BallCollectable nbc = newBall.GetComponent<BallCollectable>();
        WorldBeyondManager.Instance.AddBallToWorld(nbc);
        nbc.Shoot(ballPos, ballDirection);
    }
}
