/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using Oculus.Interaction.HandPosing.Visuals;
using Oculus.Interaction.Input;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Oculus.Interaction.HandPosing.Editor
{
    public class HandGrabPoseWizard : ScriptableWizard
    {
        [Header("Reference interactor used for Hand Grab:")]
        [SerializeField]
        private HandGrabInteractor _handGrabInteractor;
        [Header("Root of the object to record the pose to:")]
        [SerializeField]
        private GameObject _recordable;

        /// <summary>
        /// References the hand prototypes used to represent the HandGrabInteractables. These are the
        /// static hands placed around the interactable to visualize the different holding hand-poses.
        /// Not mandatory.
        /// </summary>
        [SerializeField, Optional]
        [Tooltip("Prototypes of the static hands (ghosts) that visualize holding poses")]
        private HandGhostProvider _ghostProvider;

        /// <summary>
        /// This ScriptableObject stores the HandGrabInteractables generated at Play-Mode so it survives
        /// the Play-Edit cycle.
        /// Create a collection and assign it even in Play Mode and make sure to store here the
        /// interactables, then restore it in Edit-Mode to be serialized.
        /// </summary>
        [SerializeField, Optional]
        [Tooltip("Collection for storing generated HandGrabInteractables during Play-Mode, so they can be restored in Edit-Mode")]
        private HandGrabInteractableDataCollection _posesCollection = null;

        [Header("1- Go to Play mode and record your poses: ")]
        [SerializeField]
        private KeyCode _recordKey = KeyCode.Space;
        [InspectorButton("RecordPose", 80)]
        [SerializeField]
        private string _recordPose;

        [Header("2- Store your poses before exiting Play mode: ")]
        /// <summary>
        /// Creates an Inspector button to store the current HandGrabInteractables to the posesCollection.
        /// Use it during Play-Mode.
        /// </summary>
        [InspectorButton("SaveToAsset")]
        [SerializeField]
        private string _storePoses;

        [Header("3-Now load the poses in Edit mode to tweak and persist them: ")]
        /// <summary>
        /// Creates an Inspector button that restores the saved HandGrabInteractables inn posesCollection.
        /// Use it in Edit-Mode.
        /// </summary>
        [InspectorButton("LoadFromAsset")]
        [SerializeField]
        private string _loadPoses;

        [MenuItem("Oculus/Interaction/Hand Pose Recorder")]
        private static void CreateWizard()
        {
            HandGrabPoseWizard wizard = ScriptableWizard.DisplayWizard<HandGrabPoseWizard>("Hand Pose Recorder", "Close");
            wizard.helpString = "Auto-Generate HandGrabInteractables with HandPoses in PlayMode, then Store and retrieve them in EditMode to persist and tweak them.";
        }

        private void Awake()
        {
            if (_ghostProvider == null)
            {
                HandGhostProvider.TryGetDefault(out _ghostProvider);
            }
        }

        protected override bool DrawWizardGUI()
        {
            if (_handGrabInteractor == null)
            {
                //TODO unity might lose this reference (But still present it in the inspector)
                //during Play-Edit mode changes
                _handGrabInteractor = null;
            }

            Event e = Event.current;
            if (e.type == EventType.KeyDown
                && e.keyCode == _recordKey)
            {
                RecordPose();
            }

            return base.DrawWizardGUI();
        }

        /// <summary>
        /// Finds the nearest object that can be snapped to and adds a new HandGrabInteractable to
        /// it with the user hand representation.
        /// </summary>
        public void RecordPose()
        {
            if (_handGrabInteractor == null
                || _handGrabInteractor.Hand == null)
            {
                Debug.LogError("Missing HandGrabInteractor. Ensure you are in PLAY mode!", this);
                return;
            }

            if (_recordable == null)
            {
                Debug.LogError("Missing Recordable", this);
                return;
            }

            HandPose trackedHandPose = TrackedPose();
            if (trackedHandPose == null)
            {
                Debug.LogError("Tracked Pose could not be retrieved", this);
                return;
            }
            Pose gripPoint = _recordable.transform.RelativeOffset(_handGrabInteractor.transform);
            HandGrabPoint point = AddHandGrabPoint(trackedHandPose, gripPoint);
            AttachGhost(point);
        }

        private HandPose TrackedPose()
        {
            if (!_handGrabInteractor.Hand.GetJointPosesLocal(out ReadOnlyHandJointPoses localJoints))
            {
                return null;
            }
            HandPose result = new HandPose(_handGrabInteractor.Hand.Handedness);
            for (int i = 0; i < FingersMetadata.HAND_JOINT_IDS.Length; ++i)
            {
                HandJointId jointID = FingersMetadata.HAND_JOINT_IDS[i];
                result.JointRotations[i] = localJoints[jointID].rotation;
            }
            return result;
        }

        private void AttachGhost(HandGrabPoint point)
        {
            if (_ghostProvider != null)
            {
                HandGhost ghostPrefab = _ghostProvider.GetHand(_handGrabInteractor.Hand.Handedness);
                HandGhost ghost = GameObject.Instantiate(ghostPrefab, point.transform);
                ghost.SetPose(point);
            }
        }

        /// <summary>
        /// Creates a new HandGrabInteractable at the exact pose of a given hand.
        /// Mostly used with Hand-Tracking at Play-Mode
        /// </summary>
        /// <param name="rawPose">The user controlled hand pose.</param>
        /// <param name="snapPoint">The user controlled hand pose.</param>
        /// <returns>The generated HandGrabPoint.</returns>
        public HandGrabPoint AddHandGrabPoint(HandPose rawPose, Pose snapPoint)
        {
            HandGrabInteractable interactable = HandGrabInteractable.Create(_recordable.transform);
            HandGrabPointData pointData = new HandGrabPointData()
            {
                handPose = rawPose,
                scale = 1f,
                gripPose = snapPoint,
            };
            return interactable.LoadPoint(pointData);
        }

        /// <summary>
        /// Creates a new HandGrabInteractable from the stored data.
        /// Mostly used to restore a HandGrabInteractable that was stored during Play-Mode.
        /// </summary>
        /// <param name="data">The data of the HandGrabInteractable.</param>
        /// <returns>The generated HandGrabInteractable.</returns>
        private HandGrabInteractable LoadHandGrabInteractable(HandGrabInteractableData data)
        {
            HandGrabInteractable interactable = HandGrabInteractable.Create(_recordable.transform);
            interactable.LoadData(data);
            return interactable;
        }

        /// <summary>
        /// Load the HandGrabInteractable from a Collection.
        /// This method is called from a button in the Inspector and will load the posesCollection.
        /// </summary>
        private void LoadFromAsset()
        {
            if (_posesCollection != null)
            {
                foreach (HandGrabInteractableData handPose in _posesCollection.InteractablesData)
                {
                    LoadHandGrabInteractable(handPose);
                }
            }
        }

        /// <summary>
        /// Stores the interactables to a SerializedObject (the empty object must be
        /// provided in the inspector or one will be auto-generated). First it translates the HandGrabInteractable to a serialized
        /// form HandGrabbableData).
        /// This method is called from a button in the Inspector.
        /// </summary>
        private void SaveToAsset()
        {
            List<HandGrabInteractableData> savedPoses = new List<HandGrabInteractableData>();
            foreach (HandGrabInteractable snap in _recordable.GetComponentsInChildren<HandGrabInteractable>(false))
            {
                savedPoses.Add(snap.SaveData());
            }
            if (_posesCollection == null)
            {
                GenerateCollectionAsset();
            }
            _posesCollection?.StoreInteractables(savedPoses);
        }

        private void GenerateCollectionAsset()
        {
            _posesCollection = ScriptableObject.CreateInstance<HandGrabInteractableDataCollection>();
            string parentDir = Path.Combine("Assets", "HandGrabInteractableDataCollection");
            if (!Directory.Exists(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }
            string name = _recordable != null ? _recordable.name : "Auto";
            AssetDatabase.CreateAsset(_posesCollection, Path.Combine(parentDir, $"{name}_HandGrabCollection.asset"));
            AssetDatabase.SaveAssets();
        }
    }
}
