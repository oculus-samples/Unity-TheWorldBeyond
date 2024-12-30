// Copyright (c) Meta Platforms, Inc. and affiliates.

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
