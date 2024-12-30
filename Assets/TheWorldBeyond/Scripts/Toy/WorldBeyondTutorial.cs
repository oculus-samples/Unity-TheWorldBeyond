// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using TheWorldBeyond.GameManagement;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheWorldBeyond.Toy
{
    public class WorldBeyondTutorial : MonoBehaviour
    {
        public static WorldBeyondTutorial Instance = null;
        public Canvas CanvasObject;
        public Image LabelBackground;
        public TextMeshProUGUI TutorialText;
        // used for full-screen passthrough, only when player walks out of the room
        public MeshRenderer PassthroughSphere;
        private bool m_attachToView = false;

        // don't allow any other messages after this one, because the app is quitting
        private bool m_hitCriticalError = false;

        // when hitting a game-breaking error, quit the app after displaying the message
        private const float ERROR_MESSAGE_DISPLAY_TIME = 5.0f;

        public enum TutorialMessage
        {
            EnableFlashlight, // hands only
            BallSearch,
            ShootBall,
            AimWall, // hands only
            ShootWall,
            SwitchToy,
            NoBalls,
            None,
            // Only Scene Error messages after this entry
            ERROR_USER_WALKED_OUTSIDE_OF_ROOM,    // runs every frame, to m_force on Passthrough. The only error that doesn't quit the app.
            ERROR_NO_SCENE_DATA,                  // by default, Room Setup is launched; if the user cancels, this message displays
            ERROR_NO_SCENE_DATA_LINK,             // same as above, but on PC, Room Setup doesn't launch; so error message is slightly different
            ERROR_USER_STARTED_OUTSIDE_OF_ROOM,   // user is outside of the room volume, likely from starting in a different guardian/room
            ERROR_NOT_ENOUGH_WALLS,               // fewer than 3 walls, or only non-walls discovered (e.g. user has only set up a desk)
            ERROR_TOO_MANY_WALLS,                 // a closed loop of walls was found, but there are other rooms/walls
            ERROR_ROOM_IS_OPEN,                   // walls don't form a closed loop
            ERROR_INTERSECTING_WALLS              // walls are intersecting
        };
        public TutorialMessage CurrentMessage { private set; get; } = TutorialMessage.BallSearch;

        private void Awake()
        {
            Instance = this;
            DisplayMessage(TutorialMessage.None);

            // ensure the UI renders above Passthrough and hands
            PassthroughSphere.material.renderQueue = 4499;
            LabelBackground.material.renderQueue = 4500;
            TutorialText.fontMaterial.renderQueue = 4501;

            PassthroughSphere.gameObject.SetActive(false);
        }

        private void Update()
        {
            UpdatePosition();
        }

        public void UpdateMessageTextForInput()
        {
            DisplayMessage(CurrentMessage);
        }

        public void DisplayMessage(TutorialMessage message)
        {
            if (m_hitCriticalError)
            {
                return;
            }

            CanvasObject.gameObject.SetActive(message != TutorialMessage.None);

            PassthroughSphere.gameObject.SetActive(message == TutorialMessage.ERROR_USER_WALKED_OUTSIDE_OF_ROOM);

            AttachToView(message >= TutorialMessage.ERROR_USER_WALKED_OUTSIDE_OF_ROOM);

            m_hitCriticalError = message >= TutorialMessage.ERROR_NO_SCENE_DATA;
            if (m_hitCriticalError)
            {
                _ = StartCoroutine(KillApp());
            }

            switch (message)
            {
                case TutorialMessage.EnableFlashlight:
                    TutorialText.text = "Open palm outward to enable flashlight";
                    break;
                case TutorialMessage.BallSearch:
                    TutorialText.text = WorldBeyondManager.Instance.UsingHands ?
                        "Search around your room for energy balls. Make a fist to grab them from afar." :
                        "Search around your room for energy balls. Aim and hold Index Trigger to absorb them.";
                    break;
                case TutorialMessage.NoBalls:
                    TutorialText.text = "You're out of energy balls... switch back to the flashlight to find more";
                    break;
                case TutorialMessage.ShootBall:
                    TutorialText.text = WorldBeyondManager.Instance.UsingHands ?
                        "Shoot balls by opening fist" :
                        "Shoot balls with index trigger";
                    break;
                case TutorialMessage.AimWall: // only used for hands
                    TutorialText.text = "With your palm facing up, aim at a wall";
                    break;
                case TutorialMessage.ShootWall:
                    TutorialText.text = WorldBeyondManager.Instance.UsingHands ?
                        "Close hand to open/close wall" :
                        "Shoot walls to open/close them";
                    break;
                case TutorialMessage.SwitchToy:
                    TutorialText.text = "Use thumbstick left/right to switch toys";
                    break;
                case TutorialMessage.None:
                    break;
                case TutorialMessage.ERROR_USER_WALKED_OUTSIDE_OF_ROOM:
                    TutorialText.text = "Out of bounds. Please return to your room.";
                    break;
                case TutorialMessage.ERROR_NO_SCENE_DATA:
                    TutorialText.text = "The World Beyond requires Scene data. Please run Space Setup in Settings.";
                    break;
                case TutorialMessage.ERROR_NO_SCENE_DATA_LINK:
                    TutorialText.text = "The World Beyond requires Scene data. Please disable Link, then run Space Setup in Settings. Then enable Link and try again.";
                    break;
                case TutorialMessage.ERROR_USER_STARTED_OUTSIDE_OF_ROOM:
                    TutorialText.text = "It appears you're outside of your room. Please enter your room and restart.";
                    break;
                case TutorialMessage.ERROR_NOT_ENOUGH_WALLS:
                    TutorialText.text = "You haven't set up enough walls. Please run Room Setup in Settings.";
                    break;
                case TutorialMessage.ERROR_TOO_MANY_WALLS:
                    TutorialText.text = "Somehow, you have more walls than you should. Please re-create your walls in Space Setup in Settings.";
                    break;
                case TutorialMessage.ERROR_ROOM_IS_OPEN:
                    TutorialText.text = "The World Beyond requires a closed space. Please re-create your walls in Space Setup in Settings.";
                    break;
                case TutorialMessage.ERROR_INTERSECTING_WALLS:
                    TutorialText.text = "It appears that some of your walls overlap. Please re-create your walls in Room Setup in Settings.";
                    break;
            }
            CurrentMessage = message;
        }

        public void ForceInvisible()
        {
            // Don't hide the popup if it's attached to your view
            if (m_attachToView) return;
            CanvasObject.gameObject.SetActive(false);
        }

        public void ForceVisible()
        {
            if (CurrentMessage != TutorialMessage.None)
            {
                CanvasObject.gameObject.SetActive(true);
            }
        }

        public void HideMessage(TutorialMessage message)
        {
            if (CurrentMessage == message)
            {
                CanvasObject.gameObject.SetActive(false);
                CurrentMessage = TutorialMessage.None;
            }
        }

        private void AttachToView(bool doAttach)
        {
            m_attachToView = doAttach;
            // snap it to the view
            if (doAttach)
            {
                UpdatePosition(false);
                ForceVisible();
            }

            // for now, attaching to the view is a special case and only used when there is no Scene detected
            // the black fade sphere needs to render before the UI
            if (WorldBeyondManager.Instance)
            {
                WorldBeyondManager.Instance.FadeSphere.sharedMaterial.renderQueue = doAttach ? 4497 : 4999;
            }
        }

        private void UpdatePosition(bool useSmoothing = true)
        {
            var centerEye = WorldBeyondManager.Instance.MainCamera.transform;

            var smoothing = useSmoothing ? Mathf.SmoothStep(0.3f, 0.9f, Time.deltaTime / 50.0f) : 1.0f;

            var targetPosition = m_attachToView ? centerEye.position + centerEye.forward * 0.7f : WorldBeyondManager.Instance.GetControllingHand(19).position;
            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothing);

            var lookDir = transform.position - centerEye.position;
            if (lookDir.magnitude > Mathf.Epsilon && lookDir != Vector3.zero)
            {
                var targetRotation = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothing);
            }
        }

        private IEnumerator KillApp()
        {
            yield return new WaitForSeconds(ERROR_MESSAGE_DISPLAY_TIME);
            Application.Quit();
        }
    }
}
