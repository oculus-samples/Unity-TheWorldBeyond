/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

#if OVR_INTERNAL_CODE
#if OVRPLUGIN_TESTING

using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

using Bool = OVRPlugin.Bool;
using Result = OVRPlugin.Result;
using Quatf = OVRPlugin.Quatf;
using Vector3f = OVRPlugin.Vector3f;

public class FakeOVRPlugin_9 : OVRPlugin.OVRP_1_9_0_TEST
{
	// Pretend hands are connected although they are not really not
	public override OVRPlugin.Controller ovrp_GetConnectedControllers()
	{
		return OVRPlugin.Controller.Hands;
	}
}

public class FakeOVRPlugin_12 : OVRPlugin.OVRP_1_12_0_TEST
{
	public Vector3 LeftHandPosition = Vector3.zero;
	public Vector3 RightHandPosition = Vector3.zero;

	private OVRPlugin.OVRP_1_12_0_TEST innerMockObj_ = new OVRPlugin.OVRP_1_12_0_TEST();

	public override OVRPlugin.PoseStatef ovrp_GetNodePoseState(OVRPlugin.Step stepId, OVRPlugin.Node nodeId)
	{
		if (nodeId == OVRPlugin.Node.HandLeft || nodeId == OVRPlugin.Node.HandRight)
		{
			OVRPlugin.PoseStatef pose = OVRPlugin.PoseStatef.identity;
			OVRPose ovrPose = new OVRPose
			{
				position = nodeId == OVRPlugin.Node.HandLeft ? LeftHandPosition :  RightHandPosition,
				orientation = Quaternion.identity
			};
			return new OVRPlugin.PoseStatef()
			{
				Pose = ovrPose.ToPosef(),
				Velocity = Vector3f.zero,
				Acceleration = Vector3f.zero,
				AngularVelocity = Vector3f.zero,
				AngularAcceleration = Vector3f.zero,
				Time = 0.0f,
			};
		}

		return innerMockObj_.ovrp_GetNodePoseState(stepId, nodeId);
	}
}

public class FakeOVRPlugin_44 : OVRPlugin.OVRP_1_44_0_TEST
{
	public override Result ovrp_GetHandTrackingEnabled(ref Bool handTrackingEnabled)
	{
		handTrackingEnabled = Bool.True;
		return Result.Success;
	}

	public override Result ovrp_GetHandState(OVRPlugin.Step stepId, OVRPlugin.Hand hand, out OVRPlugin.HandStateInternal handState)
	{
		// The only thing we actually need to override this call for is to set Status, the rest we leave blank.
		// Otherwise OVRHand.IsTracked will return true in OVRTrackedKeyboardHands.
		handState.Status = OVRPlugin.HandStatus.HandTracked | OVRPlugin.HandStatus.InputStateValid;
		if (hand == OVRPlugin.Hand.HandRight)
		{
			handState.Status |= OVRPlugin.HandStatus.DominantHand;
		}

		handState.RootPose = OVRPlugin.Posef.identity;
		handState.BoneRotations_0 = Quatf.identity;
		handState.BoneRotations_1 = Quatf.identity;
		handState.BoneRotations_2 = Quatf.identity;
		handState.BoneRotations_3 = Quatf.identity;
		handState.BoneRotations_4 = Quatf.identity;
		handState.BoneRotations_5 = Quatf.identity;
		handState.BoneRotations_6 = Quatf.identity;
		handState.BoneRotations_7 = Quatf.identity;
		handState.BoneRotations_8 = Quatf.identity;
		handState.BoneRotations_9 = Quatf.identity;
		handState.BoneRotations_10 = Quatf.identity;
		handState.BoneRotations_11 = Quatf.identity;
		handState.BoneRotations_12 = Quatf.identity;
		handState.BoneRotations_13 = Quatf.identity;
		handState.BoneRotations_14 = Quatf.identity;
		handState.BoneRotations_15 = Quatf.identity;
		handState.BoneRotations_16 = Quatf.identity;
		handState.BoneRotations_17 = Quatf.identity;
		handState.BoneRotations_18 = Quatf.identity;
		handState.BoneRotations_19 = Quatf.identity;
		handState.BoneRotations_20 = Quatf.identity;
		handState.BoneRotations_21 = Quatf.identity;
		handState.BoneRotations_22 = Quatf.identity;
		handState.BoneRotations_23 = Quatf.identity;

		handState.Pinches = 0;
		handState.PinchStrength_0 = 0.0f;
		handState.PinchStrength_1 = 0.0f;
		handState.PinchStrength_2 = 0.0f;
		handState.PinchStrength_3 = 0.0f;
		handState.PinchStrength_4 = 0.0f;

		handState.PointerPose = OVRPlugin.Posef.identity;
		handState.HandScale = 1.0f;

		handState.HandConfidence = OVRPlugin.TrackingConfidence.High;
		handState.FingerConfidences_0 = OVRPlugin.TrackingConfidence.High;
		handState.FingerConfidences_1 = OVRPlugin.TrackingConfidence.High;
		handState.FingerConfidences_2 = OVRPlugin.TrackingConfidence.High;
		handState.FingerConfidences_3 = OVRPlugin.TrackingConfidence.High;
		handState.FingerConfidences_4 = OVRPlugin.TrackingConfidence.High;

		handState.RequestedTimeStamp = 0.0;
		handState.SampleTimeStamp = 0.0;

		return Result.Success;
	}
}

public class FakeOVRPlugin_55 : OVRPlugin.OVRP_1_55_0_TEST
{
	// Provide OVRSkeleton with a fake bone list. (OVRTrackedKeyboardHands directly references specific bones
	// but OVRPlugin is not available in the test environment to provide them.)
	public override Result ovrp_GetSkeleton2(OVRPlugin.SkeletonType skeletonType, out OVRPlugin.Skeleton2Internal skeleton)
	{
		skeleton = new OVRPlugin.Skeleton2Internal();

		skeleton.Type = skeletonType;

		skeleton.NumBones = (uint)OVRPlugin.SkeletonConstants.MaxHandBones;
		skeleton.Bones_0 = MakeDefaultBone(0);
		skeleton.Bones_1 = MakeDefaultBone(1);
		skeleton.Bones_2 = MakeDefaultBone(2);
		skeleton.Bones_3 = MakeDefaultBone(3);
		skeleton.Bones_4 = MakeDefaultBone(4);
		skeleton.Bones_5 = MakeDefaultBone(5);
		skeleton.Bones_6 = MakeDefaultBone(6);
		skeleton.Bones_7 = MakeDefaultBone(7);
		skeleton.Bones_8 = MakeDefaultBone(8);
		skeleton.Bones_9 = MakeDefaultBone(9);
		skeleton.Bones_10 = MakeDefaultBone(10);
		skeleton.Bones_11 = MakeDefaultBone(11);
		skeleton.Bones_12 = MakeDefaultBone(12);
		skeleton.Bones_13 = MakeDefaultBone(13);
		skeleton.Bones_14 = MakeDefaultBone(14);
		skeleton.Bones_15 = MakeDefaultBone(15);
		skeleton.Bones_16 = MakeDefaultBone(16);
		skeleton.Bones_17 = MakeDefaultBone(17);
		skeleton.Bones_18 = MakeDefaultBone(18);
		skeleton.Bones_19 = MakeDefaultBone(19);
		skeleton.Bones_20 = MakeDefaultBone(20);
		skeleton.Bones_21 = MakeDefaultBone(21);
		skeleton.Bones_22 = MakeDefaultBone(22);
		skeleton.Bones_23 = MakeDefaultBone(23);

		skeleton.NumBoneCapsules = (uint)OVRPlugin.SkeletonConstants.MaxBoneCapsules;
		skeleton.BoneCapsules_0 = MakeDefaultBoneCapsule(0);
		skeleton.BoneCapsules_1 = MakeDefaultBoneCapsule(1);
		skeleton.BoneCapsules_2 = MakeDefaultBoneCapsule(2);
		skeleton.BoneCapsules_3 = MakeDefaultBoneCapsule(3);
		skeleton.BoneCapsules_4 = MakeDefaultBoneCapsule(4);
		skeleton.BoneCapsules_5 = MakeDefaultBoneCapsule(5);
		skeleton.BoneCapsules_6 = MakeDefaultBoneCapsule(6);
		skeleton.BoneCapsules_7 = MakeDefaultBoneCapsule(7);
		skeleton.BoneCapsules_8 = MakeDefaultBoneCapsule(8);
		skeleton.BoneCapsules_9 = MakeDefaultBoneCapsule(9);
		skeleton.BoneCapsules_10 = MakeDefaultBoneCapsule(10);
		skeleton.BoneCapsules_11 = MakeDefaultBoneCapsule(11);
		skeleton.BoneCapsules_12 = MakeDefaultBoneCapsule(12);
		skeleton.BoneCapsules_13 = MakeDefaultBoneCapsule(13);
		skeleton.BoneCapsules_14 = MakeDefaultBoneCapsule(14);
		skeleton.BoneCapsules_15 = MakeDefaultBoneCapsule(15);
		skeleton.BoneCapsules_16 = MakeDefaultBoneCapsule(16);
		skeleton.BoneCapsules_17 = MakeDefaultBoneCapsule(17);
		skeleton.BoneCapsules_18 = MakeDefaultBoneCapsule(18);

		return Result.Success;
	}

	private static OVRPlugin.Bone MakeDefaultBone(int id)
	{
		return new OVRPlugin.Bone
		{
			Id = (OVRPlugin.BoneId)id,
			ParentBoneIndex = -1,
			Pose = OVRPlugin.Posef.identity
		};
	}

	private static OVRPlugin.BoneCapsule MakeDefaultBoneCapsule(int id)
	{
		return new OVRPlugin.BoneCapsule
		{
			BoneIndex = (short)id,
			StartPoint = Vector3f.zero,
			EndPoint = Vector3f.zero,
			Radius = 0.1f
		};
	}
}

public class FakeOVRPlugin_68 : OVRPlugin.OVRP_1_68_0_TEST
{
	public Vector3 KeyboardPosition = Vector3.zero;
	public Quaternion KeyboardRotation = Quaternion.identity;
	public bool KeyboardIsTracked = true;

	public override Result ovrp_StartKeyboardTracking(UInt64 trackedKeyboardId)
	{
		return Result.Success;
	}

	public override Result ovrp_StopKeyboardTracking()
	{
		return Result.Success;
	}

	public override Result ovrp_GetKeyboardState(OVRPlugin.Step stepId, int frameIndex, out OVRPlugin.KeyboardState keyboardState)
	{
		keyboardState.IsActive = Bool.True;
		keyboardState.OrientationValid = KeyboardIsTracked ? Bool.True : Bool.False;
		keyboardState.PositionValid = KeyboardIsTracked ? Bool.True : Bool.False;
		keyboardState.OrientationTracked = KeyboardIsTracked ? Bool.True : Bool.False;
		keyboardState.PositionTracked = KeyboardIsTracked ? Bool.True : Bool.False;

		OVRPlugin.PoseStatef pose = OVRPlugin.PoseStatef.identity;
		OVRPose ovrPose = new OVRPose { position = KeyboardPosition, orientation = KeyboardRotation };
		pose.Pose = ovrPose.ToPosef(); // Convert pose to Posef, OVRPlugin layer uses different representation
		keyboardState.PoseState = pose;

		keyboardState.ContrastParameters = OVRPlugin.Vector4f.zero;
		return Result.Success;
	}

	public override Result ovrp_GetSystemKeyboardDescription(OVRPlugin.TrackedKeyboardQueryFlags keyboardQueryFlags, out OVRPlugin.KeyboardDescription keyboardDescription)
	{
		keyboardDescription.Name = Encoding.UTF8.GetBytes("TestKeyboard");
		keyboardDescription.SupportedPresentationStyles = OVRPlugin.TrackedKeyboardPresentationStyles.Opaque | OVRPlugin.TrackedKeyboardPresentationStyles.KeyLabel;
		keyboardDescription.Dimensions = Vector3f.zero;
		keyboardDescription.Dimensions.x = 0.5f;
		keyboardDescription.Dimensions.y = 0.5f;
		keyboardDescription.Dimensions.z = 0.5f;
		keyboardDescription.KeyboardFlags = OVRPlugin.TrackedKeyboardFlags.Exists | OVRPlugin.TrackedKeyboardFlags.Local | OVRPlugin.TrackedKeyboardFlags.Connected;
		keyboardDescription.TrackedKeyboardId = 0;
		return Result.Success;
	}

	public override Result ovrp_LoadRenderModel(UInt64 modelKey, uint bufferInputCapacity, ref uint bufferCountOuput, IntPtr buffer)
	{
		TextAsset textAsset = Resources.Load("logitech_k830_float_color_babylon_glb") as TextAsset;
		Debug.Log("Loading test resource " + textAsset.name + " of size " + textAsset.bytes.Length);
		long copySize = Math.Min(textAsset.bytes.Length, bufferInputCapacity);
		if (copySize > 0)
		{
			Marshal.Copy(textAsset.bytes, 0, buffer, (int)copySize);
		}
		bufferCountOuput = (uint)textAsset.bytes.Length;
		return Result.Success;
	}

	public override Result ovrp_GetRenderModelPaths(uint index, IntPtr path)
	{
		if (index == 0)
		{
			byte[] pathBytes = Encoding.ASCII.GetBytes("/model_fb/keyboard/local" + "\0");
			Marshal.Copy(pathBytes, 0, path, pathBytes.Length);
			return Result.Success;
		}
		return Result.Failure;
	}

	public override Result ovrp_GetRenderModelProperties(string path, out OVRPlugin.RenderModelPropertiesInternal properties)
	{
		properties = new OVRPlugin.RenderModelPropertiesInternal
		{
			ModelName = Encoding.ASCII.GetBytes("Test keyboard"),
			ModelKey = OVRPlugin.RENDER_MODEL_NULL_KEY + 1, // Any non-null-key value
			VendorId = 0,
			ModelVersion = 0
		};
		return Result.Success;
	}
}

public class TrackedKeyboardTest : OVRPluginPlayModeTest
{
	private FakeOVRPlugin_9 fakeOVRPlugin_9_;
	private FakeOVRPlugin_12 fakeOVRPlugin_12_;
	private FakeOVRPlugin_44 fakeOVRPlugin_44_;
	private FakeOVRPlugin_55 fakeOVRPlugin_55_;
	private FakeOVRPlugin_68 fakeOVRPlugin_68_;

	private OVRTrackedKeyboard ovrTrackedKeyboard_;
	private OVRHand leftHand_;
	private OVRHand rightHand_;
	private GameObject keyboardModel_;
	private const float MAX_DELTA = 0.001f;

	[UnitySetUp]
	public override IEnumerator UnitySetUp()
	{
		yield return base.UnitySetUp();

		fakeOVRPlugin_9_ = new FakeOVRPlugin_9();
		OVRPlugin.OVRP_1_9_0.mockObj = fakeOVRPlugin_9_;
		fakeOVRPlugin_12_ = new FakeOVRPlugin_12();
		OVRPlugin.OVRP_1_12_0.mockObj = fakeOVRPlugin_12_;
		fakeOVRPlugin_44_ = new FakeOVRPlugin_44();
		OVRPlugin.OVRP_1_44_0.mockObj = fakeOVRPlugin_44_;
		fakeOVRPlugin_55_ = new FakeOVRPlugin_55();
		OVRPlugin.OVRP_1_55_0.mockObj = fakeOVRPlugin_55_;
		fakeOVRPlugin_68_ = new FakeOVRPlugin_68();
		OVRPlugin.OVRP_1_68_0.mockObj = fakeOVRPlugin_68_;

		var asyncOperation = SceneManager.LoadSceneAsync("Oculus/VR/Scenes/TrackedKeyboard", LoadSceneMode.Additive);
		yield return new WaitUntil(() => asyncOperation.isDone);
	}

	[UnityTearDown]
	public override IEnumerator UnityTearDown()
	{
		GameObject.FindObjectOfType<OVRTrackedKeyboard>().TrackingEnabled = false;
		yield return null;

		OVRKeyboard.TrackedKeyboardInfo info;
		OVRKeyboard.GetSystemKeyboardInfo(OVRPlugin.TrackedKeyboardQueryFlags.Local, out info);
		OVRKeyboard.StopKeyboardTracking(info);
		yield return null;

		var asyncOperation = SceneManager.UnloadSceneAsync("Oculus/VR/Scenes/TrackedKeyboard");
		yield return new WaitUntil(() => asyncOperation.isDone);

		// Point mockObjs back at original production version of code
		OVRPlugin.OVRP_1_9_0.mockObj = new OVRPlugin.OVRP_1_9_0_TEST();
		OVRPlugin.OVRP_1_12_0.mockObj = new OVRPlugin.OVRP_1_12_0_TEST();
		OVRPlugin.OVRP_1_44_0.mockObj = new OVRPlugin.OVRP_1_44_0_TEST();
		OVRPlugin.OVRP_1_55_0.mockObj = new OVRPlugin.OVRP_1_55_0_TEST();
		OVRPlugin.OVRP_1_68_0.mockObj = new OVRPlugin.OVRP_1_68_0_TEST();

		yield return base.UnityTearDown();
	}

	private bool HandIsVisible(OVRHand hand)
	{
		return hand.gameObject.activeInHierarchy &&
		       hand.enabled &&
		       hand.GetComponent<SkinnedMeshRenderer>().enabled;
	}

	private IEnumerator StartTracking()
	{
		ovrTrackedKeyboard_ = GameObject.FindObjectOfType<OVRTrackedKeyboard>();
		Assert.IsNotNull(ovrTrackedKeyboard_);

		ovrTrackedKeyboard_.TrackingEnabled = true;
		yield return new WaitUntil(() =>
			ovrTrackedKeyboard_.TrackingState == OVRTrackedKeyboard.TrackedKeyboardState.Valid);

		keyboardModel_ = GameObject.Find("k830_keyboard");
		Assert.IsNotNull(keyboardModel_);
		OVRCameraRig cameraRig = GameObject.FindObjectOfType<OVRCameraRig>();
		leftHand_ = cameraRig.leftHandAnchor.GetComponentInChildren<OVRHand>();
		Assert.IsNotNull(leftHand_);
		rightHand_ = cameraRig.rightHandAnchor.GetComponentInChildren<OVRHand>();
		Assert.IsNotNull(rightHand_);
	}

	private IEnumerator StopTracking()
	{
		ovrTrackedKeyboard_.TrackingEnabled = false;
		yield return new WaitUntil(() =>
			ovrTrackedKeyboard_.TrackingState == OVRTrackedKeyboard.TrackedKeyboardState.Offline);

		keyboardModel_ = GameObject.Find("k830_keyboard");
		Assert.IsNull(keyboardModel_);
	}

	// Tests that keyboard goes into Valid state and model appears when TrackingEnabled is set to true.
	[UnityTest]
	public IEnumerator TestStartsTracking()
	{
		yield return StartTracking();
	}

	// Tests that keyboard goes offline and model disappears when TrackingEnabled is set to false.
	[UnityTest]
	public IEnumerator TestStopsTracking()
	{
		yield return StartTracking();
		yield return StopTracking();
	}

	// Tests that starting then stopping then starting tracking again works.
	[UnityTest]
	public IEnumerator TestRestartsTracking()
	{
		yield return StartTracking();
		yield return StopTracking();
		yield return StartTracking();
	}

	// Checks that keyboard model exists and has bounding box with expected size (verifies model loading)
	[UnityTest]
	public IEnumerator TestKeyboardSize()
	{
		yield return StartTracking();

		MeshRenderer renderer = keyboardModel_.GetComponent<MeshRenderer>();
		Assert.IsNotNull(renderer);

		// Test K830 keyboard should be larger than 35cm / 13 in and smaller than 40cm / 16 in
		// This mainly tests model loading from the glb.
		Assert.Greater(renderer.bounds.size.magnitude, 0.35f);
		Assert.Less   (renderer.bounds.size.magnitude, 0.40f);
	}

	// Verifies that keyboard position/rotation updates correctly when OVRPlugin reports
	// a new keyboard position/rotation.
	[UnityTest]
	public IEnumerator TestTrackingPose()
	{
		yield return StartTracking();

		Transform keyboardTransform = keyboardModel_.transform;

		Vector3 initialPosition = keyboardTransform.position;
		Quaternion initialRotation = keyboardTransform.rotation;

		Vector3 positionChange = new Vector3(1.0f, 1.0f, 1.0f);
		fakeOVRPlugin_68_.KeyboardPosition += positionChange;

		// This angle must be small otherwise the keyboard angle filter will stop tracking
		Quaternion rotationChange = Quaternion.Euler(2.0f, 2.0f, 2.0f);
		float rotationChangeAngle;
		Vector3 rotationChangeAxis;
		rotationChange.ToAngleAxis(out rotationChangeAngle, out rotationChangeAxis);
		fakeOVRPlugin_68_.KeyboardRotation *= rotationChange;

		// Wait for motion smoothing to stabilize
		Vector3 oldPosition, newPosition;
		do
		{
			oldPosition = keyboardTransform.position;
			yield return null;
			newPosition = keyboardTransform.position;
		} while (newPosition != oldPosition);

		Assert.Less(((keyboardTransform.position - initialPosition) - positionChange).magnitude, MAX_DELTA);
		Assert.Less(Quaternion.Angle(initialRotation, keyboardTransform.rotation) - rotationChangeAngle, MAX_DELTA);
	}

	// If keyboard rotates to a large angle from "up", should stop updating its tracking pose
	[UnityTest]
	public IEnumerator TestKeyboardAngleFilter()
	{
		yield return StartTracking();

		Transform keyboardTransform = keyboardModel_.transform;

		Vector3 initialPosition = keyboardTransform.position;
		Quaternion initialRotation = keyboardTransform.rotation;

		fakeOVRPlugin_68_.KeyboardPosition += new Vector3(1.0f, 1.0f, 1.0f);
		fakeOVRPlugin_68_.KeyboardRotation *= Quaternion.Euler(90.0f, 90.0f, 90.0f); // big angle

		yield return null;

		// Should see no change
		Assert.Less((keyboardTransform.position - initialPosition).magnitude, MAX_DELTA);
		Assert.Less(Quaternion.Angle(initialRotation, keyboardTransform.rotation), MAX_DELTA);
	}

	// Tests that the passthrough hands layer does not appear when hands are far from keyboard,
	// and does appear when hands are right over keyboard.
	[UnityTest]
	public IEnumerator TestPassthroughHandsAppear()
	{
		yield return StartTracking();
		Transform keyboardTransform = keyboardModel_.transform;
		OVROverlay[] overlays = GameObject.FindObjectsOfType<OVROverlay>();
		OVRTrackedKeyboard trackedKeyboard = GameObject.FindObjectOfType<OVRTrackedKeyboard>();

		// Put hands far from keyboard
		fakeOVRPlugin_12_.LeftHandPosition = keyboardTransform.position + new Vector3(0.0f, 100.0f, 0.0f);
		fakeOVRPlugin_12_.RightHandPosition = keyboardTransform.position + new Vector3(0.0f, 100.0f, 0.0f);

		yield return new WaitUntil(() =>
			!trackedKeyboard.HandsOverKeyboard &&
			HandIsVisible(leftHand_) && HandIsVisible(rightHand_) &&
			Array.TrueForAll(overlays, o => o.hidden));

		// Put hands right over keyboard
		fakeOVRPlugin_12_.LeftHandPosition = keyboardTransform.position + new Vector3(0.0f, 0.02f, 0.0f);
		fakeOVRPlugin_12_.RightHandPosition = keyboardTransform.position + new Vector3(0.0f, 0.02f, 0.0f);

		yield return new WaitUntil(() =>
			trackedKeyboard.HandsOverKeyboard &&
			!HandIsVisible(leftHand_) && !HandIsVisible(rightHand_) &&
			Array.Exists(overlays, o => !o.hidden));
	}

	// Tests that if passthrough hands layer is currently being shown, and keyboard
	// tracking is lost, passthrough hands will disappear (and be replaced by regular hand model).
	// (Regression test for T106692304 fixed in D32771725).
	[UnityTest]
	public IEnumerator TestPassthroughHandsDisappearWhenKeyboardTrackingLost()
	{
		yield return StartTracking();
		Transform keyboardTransform = keyboardModel_.transform;
		OVROverlay[] overlays = GameObject.FindObjectsOfType<OVROverlay>();
		OVRTrackedKeyboard trackedKeyboard = GameObject.FindObjectOfType<OVRTrackedKeyboard>();

		// Put hands right over keyboard
		fakeOVRPlugin_12_.LeftHandPosition = keyboardTransform.position + new Vector3(0.0f, 0.02f, 0.0f);
		fakeOVRPlugin_12_.RightHandPosition = keyboardTransform.position + new Vector3(0.0f, 0.02f, 0.0f);

		yield return new WaitUntil(() =>
			!HandIsVisible(leftHand_) && !HandIsVisible(rightHand_) &&
			Array.Exists(overlays, o => !o.hidden));

		fakeOVRPlugin_68_.KeyboardIsTracked = false;

		yield return new WaitUntil(() =>
			HandIsVisible(leftHand_) && HandIsVisible(rightHand_) &&
			Array.TrueForAll(overlays, o => o.hidden));
	}

	// Verifies keyboard will appear even when not currently tracked if ShowUntracked option is true.
	[UnityTest]
	public IEnumerator TestShowUntracked()
	{
		yield return StartTracking();

		MeshRenderer renderer = keyboardModel_.GetComponent<MeshRenderer>();
		Assert.IsNotNull(renderer);
		yield return new WaitUntil(() => renderer.enabled);

		fakeOVRPlugin_68_.KeyboardIsTracked = false;
		yield return new WaitUntil(() => !renderer.enabled);

		ovrTrackedKeyboard_.ShowUntracked = true;
		yield return new WaitUntil(() => renderer.enabled);
		// Should not be rendering at origin
		Assert.Greater(Vector3.Distance(Vector3.zero, renderer.transform.position), 0.1f);

		ovrTrackedKeyboard_.ShowUntracked = false;
		yield return new WaitUntil(() => !renderer.enabled);
	}
}

#endif
#endif
