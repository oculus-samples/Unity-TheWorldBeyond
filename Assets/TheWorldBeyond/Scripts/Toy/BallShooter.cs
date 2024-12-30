// Copyright (c) Meta Platforms, Inc. and affiliates.

using TheWorldBeyond.GameManagement;
using TheWorldBeyond.VFX;
using UnityEngine;

namespace TheWorldBeyond.Toy
{
    [System.Serializable]
    public class BallShooter : WorldBeyondToy
    {
        public override void ActionDown()
        {
            if (WorldBeyondManager.Instance.BallCount > 0)
            {
                ShootBall(transform.forward);
                WorldBeyondManager.Instance.BallCount--;
            }
            else
            {
                // play "no balls" sound
                if (WorldBeyondManager.Instance.CurrentChapter == WorldBeyondManager.GameChapter.TheGreatBeyond)
                {
                    WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.NoBalls);
                }
            }
        }

        public void ShootBall(Vector3 ballDirection)
        {
            if (WorldBeyondManager.Instance && WorldBeyondManager.Instance.BallPrefab == null)
            {
                Debug.Log("TheWorldBeyond: no ball prefab found");
                return;
            }

            var ballPos = transform.position + transform.forward * 0.1f;
            var newBall = Instantiate(WorldBeyondManager.Instance.BallPrefab, ballPos, Quaternion.identity);
            var nbc = newBall.GetComponent<BallCollectable>();
            WorldBeyondManager.Instance.AddBallToWorld(nbc);
            nbc.Shoot(ballPos, ballDirection);
        }
    }
}
