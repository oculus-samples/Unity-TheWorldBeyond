// Copyright(c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

[System.Serializable]
public class BallShooter : SanctuaryToy
{
    public override void ActionDown()
    {
        if (SanctuaryExperience.Instance._ballCount > 0)
        {
            ShootBall(transform.forward);
            SanctuaryExperience.Instance._ballCount--;
        }
        else
        {
            // play "no balls" sound
            if (SanctuaryExperience.Instance._currentChapter == SanctuaryExperience.SanctuaryChapter.TheGreatBeyond)
            {
                SanctuaryTutorial.Instance.DisplayMessage(SanctuaryTutorial.TutorialMessage.NoBalls);
            }
        }
    }

    public void ShootBall(Vector3 ballDirection)
    {
        if (SanctuaryExperience.Instance && SanctuaryExperience.Instance._ballPrefab == null)
        {
            Debug.Log("Sanctuary: no ball prefab found");
            return;
        }

        Vector3 ballPos = transform.position + transform.forward * 0.1f;
        GameObject newBall = Instantiate(SanctuaryExperience.Instance._ballPrefab, ballPos, Quaternion.identity);
        BallCollectable nbc = newBall.GetComponent<BallCollectable>();
        SanctuaryExperience.Instance.AddBallToWorld(nbc);
        nbc.Shoot(ballPos, ballDirection);
    }
}
