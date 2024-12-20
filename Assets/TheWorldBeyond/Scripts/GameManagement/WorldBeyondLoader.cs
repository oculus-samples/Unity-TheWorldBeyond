// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using TheWorldBeyond.Toy;
using UnityEngine;

namespace TheWorldBeyond.GameManagement
{
    public class WorldBeyondLoader : OVRSceneModelLoader
    {
        // only allow one attempt to load the Scene;
        // if Scene loading has failed a second time, assume the User has cancelled room setup, and implicitly the game
        private bool m_sceneCaptureLaunched = false;
        private bool m_sceneCaptureComplete = false;

        private static void DisplayNoSceneDataError()
        {
            //Scene API is not supported through Oculus Link yet, don't show the "no scene data" error.
#if UNITY_EDITOR
            Debug.LogWarning("SceneAPI is not supported through Oculus Link yet.");
#else
            WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.ERROR_NO_SCENE_DATA);
#endif
        }

        private IEnumerator AwaitSceneModel()
        {
            const int ATTEMPT_COUNT = 5;
            var preamble = $"[{nameof(WorldBeyondLoader)}]: {nameof(SceneManager.LoadSceneModel)}";
            var attemptsRemaining = ATTEMPT_COUNT;
            while (attemptsRemaining > 0)
            {
                if (SceneManager.LoadSceneModel())
                {
                    Debug.Log($"[{nameof(WorldBeyondLoader)}]: Loading scene model...");
                    yield break;
                }

                if (--attemptsRemaining == 0)
                {
                    Debug.LogError($"{preamble} failed after {ATTEMPT_COUNT} attempts. Could not load scene model.");
                    DisplayNoSceneDataError();
                }
                else
                {
                    Debug.LogWarning($"{preamble} failed. Trying again ({attemptsRemaining} attempt{(attemptsRemaining == 1 ? "" : "s")} remaining).");
                    yield return null;
                }
            }
        }

        protected override void OnStart() => StartCoroutine(AwaitSceneModel());

        private IEnumerator AwaitSceneCaptureCompletion()
        {
            m_sceneCaptureLaunched = true;
            Debug.Log($"[{nameof(WorldBeyondLoader)}]: Requesting Scene Capture.");
            if (SceneManager.RequestSceneCapture())
            {
                // The app should be paused while the Guardian completes scene capture. If we don't get a response within a
                // couple of seconds after resuming, then something has gone wrong.
                yield return new WaitForSeconds(2);
                if (m_sceneCaptureComplete)
                {
                    yield break;
                }

                Debug.LogError($"[{nameof(WorldBeyondLoader)}]: Did not get a response following the completion of Scene Capture.");
            }
            else
            {
                // if no Scene data is found when using Link, inform the user of solution:
                // disable Link, run Room Setup on Quest, enable Link, play again
                if (Application.isEditor)
                {
                    WorldBeyondTutorial.Instance.DisplayMessage(WorldBeyondTutorial.TutorialMessage.ERROR_NO_SCENE_DATA_LINK);
                }

                Debug.LogError($"[{nameof(WorldBeyondLoader)}]: {nameof(SceneManager.RequestSceneCapture)} returned false.");
            }

            DisplayNoSceneDataError();
        }

        protected override void OnNoSceneModelToLoad()
        {
            if (!m_sceneCaptureLaunched)
            {
                m_sceneCaptureLaunched = true;
                _ = StartCoroutine(AwaitSceneCaptureCompletion());
            }
            else
            {
                // pop up a message to the user before quitting
                DisplayNoSceneDataError();
            }
        }

        protected override void OnSceneCaptureReturnedWithoutError()
        {
            m_sceneCaptureComplete = true;
            Debug.Log($"[{nameof(WorldBeyondLoader)}]: Scene Capture completed successfully.");
            _ = StartCoroutine(AwaitSceneModel());
        }

        protected override void OnUnexpectedErrorWithSceneCapture()
        {
            m_sceneCaptureComplete = true;
            Debug.LogError($"[{nameof(WorldBeyondLoader)}]: Scene Capture failed with an unexpected error.");
            base.OnUnexpectedErrorWithSceneCapture();
            DisplayNoSceneDataError();
        }
    }
}
