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
using UnityEngine;
using Marshal = System.Runtime.InteropServices.Marshal;

// The lowest-level interface we have with OVRPlugin is the P/Invoke (extern) methods in OVRPlugin.cs.
// This file replaces them with new methods that allow test methods to be injected by overriding them
// in a subclass. This allows end-to-end testing of the C# layer to be done. Currently I only mock the
// ones I need but there's no reason not to mock any/all of them.

// TODO: Auto-generate this code file in the future, it is perfectly mechanical and uniform.

/* In the meantime, here are manual directions on how to import an OVRP class into this file:

1. Copy the static OVRP class with all its members from OVRPlugin.cs to MockOVRPlugin.cs,
put it at the correct position in numerical order, and make it public.

2. In OVRPlugin.cs, use this ifdef structure to rename it:

#if OVR_INTERNAL_CODE
#if OVRPLUGIN_TESTING
	private static class OVRP_1_999_0_PROD
#else
	private static class OVRP_1_999_0
#endif
#else
  private static class OVRP_1_999_0
#endif

3. In MockOVRPlugin.cs, add this at top of the class (and fix version number):

		public static OVRP_1_999_0_TEST mockObj = new OVRP_1_999_0_TEST();

		public static readonly System.Version version = OVRP_1_999_0_PROD.version;

4. Delete all DllImport and other interop attributes

5. Make all extern methods public. Any non-extern methods should delegate to their PROD versions, which should
be modified to make all sub-calls using “OVRP_1_999_0.” syntax (to call back here). Any constants should delegate
to the PROD constant.

6. Remove "extern" keywords (replace “extern “ by "").

7. Use this template to create member bodies that delegate to mockObj (same method name, pass on all arguments):
		{
			return mockObj.
		}

8. Make a copy of the class, put it above it, add _TEST to the name, change “private static” to public,
remove the mockObj and version stuff from top, and remove any constants or non-delegating methods.

9. replace static by virtual and mockObj by OVRP_1_999_0_PROD (fix version number).

10. If any private types or private methods in OVRPlugin.cs need to be referenced to build correctly,
expose them with the same ifdef structure as in step #1 above.
 */

public partial class OVRPlugin
{
	public class OVRP_1_1_0_TEST
	{
		public virtual Bool ovrp_GetInitialized()
		{
			return OVRP_1_1_0_PROD.ovrp_GetInitialized();
		}

		public virtual IntPtr _ovrp_GetVersion()
		{
			return OVRP_1_1_0_PROD._ovrp_GetVersion();
		}

		public virtual IntPtr _ovrp_GetNativeSDKVersion()
		{
			return OVRP_1_1_0_PROD._ovrp_GetNativeSDKVersion();
		}

		public virtual IntPtr ovrp_GetAudioOutId()
		{
			return OVRP_1_1_0_PROD.ovrp_GetAudioOutId();
		}

		public virtual IntPtr ovrp_GetAudioInId()
		{
			return OVRP_1_1_0_PROD.ovrp_GetAudioInId();
		}

		public virtual float ovrp_GetEyeTextureScale()
		{
			return OVRP_1_1_0_PROD.ovrp_GetEyeTextureScale();
		}

		public virtual Bool ovrp_SetEyeTextureScale(float value)
		{
			return OVRP_1_1_0_PROD.ovrp_SetEyeTextureScale(value);
		}

		public virtual Bool ovrp_GetTrackingOrientationSupported()
		{
			return OVRP_1_1_0_PROD.ovrp_GetTrackingOrientationSupported();
		}

		public virtual Bool ovrp_GetTrackingOrientationEnabled()
		{
			return OVRP_1_1_0_PROD.ovrp_GetTrackingOrientationEnabled();
		}

		public virtual Bool ovrp_SetTrackingOrientationEnabled(Bool value)
		{
			return OVRP_1_1_0_PROD.ovrp_SetTrackingOrientationEnabled(value);
		}

		public virtual Bool ovrp_GetTrackingPositionSupported()
		{
			return OVRP_1_1_0_PROD.ovrp_GetTrackingPositionSupported();
		}

		public virtual Bool ovrp_GetTrackingPositionEnabled()
		{
			return OVRP_1_1_0_PROD.ovrp_GetTrackingPositionEnabled();
		}

		public virtual Bool ovrp_SetTrackingPositionEnabled(Bool value)
		{
			return OVRP_1_1_0_PROD.ovrp_SetTrackingPositionEnabled(value);
		}

		public virtual Bool ovrp_GetNodePresent(Node nodeId)
		{
			return OVRP_1_1_0_PROD.ovrp_GetNodePresent(nodeId);
		}

		public virtual Bool ovrp_GetNodeOrientationTracked(Node nodeId)
		{
			return OVRP_1_1_0_PROD.ovrp_GetNodeOrientationTracked(nodeId);
		}

		public virtual Bool ovrp_GetNodePositionTracked(Node nodeId)
		{
			return OVRP_1_1_0_PROD.ovrp_GetNodePositionTracked(nodeId);
		}

		public virtual Frustumf ovrp_GetNodeFrustum(Node nodeId)
		{
			return OVRP_1_1_0_PROD.ovrp_GetNodeFrustum(nodeId);
		}

		public virtual ControllerState ovrp_GetControllerState(uint controllerMask)
		{
			return OVRP_1_1_0_PROD.ovrp_GetControllerState(controllerMask);
		}

		public virtual int ovrp_GetSystemCpuLevel()
		{
			return OVRP_1_1_0_PROD.ovrp_GetSystemCpuLevel();
		}

		public virtual Bool ovrp_SetSystemCpuLevel(int value)
		{
			return OVRP_1_1_0_PROD.ovrp_SetSystemCpuLevel(value);
		}

		public virtual int ovrp_GetSystemGpuLevel()
		{
			return OVRP_1_1_0_PROD.ovrp_GetSystemGpuLevel();
		}

		public virtual Bool ovrp_SetSystemGpuLevel(int value)
		{
			return OVRP_1_1_0_PROD.ovrp_SetSystemGpuLevel(value);
		}

		public virtual Bool ovrp_GetSystemPowerSavingMode()
		{
			return OVRP_1_1_0_PROD.ovrp_GetSystemPowerSavingMode();
		}

		public virtual float ovrp_GetSystemDisplayFrequency()
		{
			return OVRP_1_1_0_PROD.ovrp_GetSystemDisplayFrequency();
		}

		public virtual int ovrp_GetSystemVSyncCount()
		{
			return OVRP_1_1_0_PROD.ovrp_GetSystemVSyncCount();
		}

		public virtual float ovrp_GetSystemVolume()
		{
			return OVRP_1_1_0_PROD.ovrp_GetSystemVolume();
		}

		public virtual BatteryStatus ovrp_GetSystemBatteryStatus()
		{
			return OVRP_1_1_0_PROD.ovrp_GetSystemBatteryStatus();
		}

		public virtual float ovrp_GetSystemBatteryLevel()
		{
			return OVRP_1_1_0_PROD.ovrp_GetSystemBatteryLevel();
		}

		public virtual float ovrp_GetSystemBatteryTemperature()
		{
			return OVRP_1_1_0_PROD.ovrp_GetSystemBatteryTemperature();
		}

		public virtual IntPtr _ovrp_GetSystemProductName()
		{
			return OVRP_1_1_0_PROD._ovrp_GetSystemProductName();
		}

		public virtual Bool ovrp_ShowSystemUI(PlatformUI ui)
		{
			return OVRP_1_1_0_PROD.ovrp_ShowSystemUI(ui);
		}

		public virtual Bool ovrp_GetAppMonoscopic()
		{
			return OVRP_1_1_0_PROD.ovrp_GetAppMonoscopic();
		}

		public virtual Bool ovrp_SetAppMonoscopic(Bool value)
		{
			return OVRP_1_1_0_PROD.ovrp_SetAppMonoscopic(value);
		}

		public virtual Bool ovrp_GetAppHasVrFocus()
		{
			return OVRP_1_1_0_PROD.ovrp_GetAppHasVrFocus();
		}

		public virtual Bool ovrp_GetAppShouldQuit()
		{
			return OVRP_1_1_0_PROD.ovrp_GetAppShouldQuit();
		}

		public virtual Bool ovrp_GetAppShouldRecenter()
		{
			return OVRP_1_1_0_PROD.ovrp_GetAppShouldRecenter();
		}

		public virtual IntPtr _ovrp_GetAppLatencyTimings()
		{
			return OVRP_1_1_0_PROD._ovrp_GetAppLatencyTimings();
		}

		public virtual Bool ovrp_GetUserPresent()
		{
			return OVRP_1_1_0_PROD.ovrp_GetUserPresent();
		}

		public virtual float ovrp_GetUserIPD()
		{
			return OVRP_1_1_0_PROD.ovrp_GetUserIPD();
		}

		public virtual Bool ovrp_SetUserIPD(float value)
		{
			return OVRP_1_1_0_PROD.ovrp_SetUserIPD(value);
		}

		public virtual float ovrp_GetUserEyeDepth()
		{
			return OVRP_1_1_0_PROD.ovrp_GetUserEyeDepth();
		}

		public virtual Bool ovrp_SetUserEyeDepth(float value)
		{
			return OVRP_1_1_0_PROD.ovrp_SetUserEyeDepth(value);
		}

		public virtual float ovrp_GetUserEyeHeight()
		{
			return OVRP_1_1_0_PROD.ovrp_GetUserEyeHeight();
		}

		public virtual Bool ovrp_SetUserEyeHeight(float value)
		{
			return OVRP_1_1_0_PROD.ovrp_SetUserEyeHeight(value);
		}
	}

	public static class OVRP_1_1_0
	{
		public static OVRP_1_1_0_TEST mockObj = new OVRP_1_1_0_TEST();

		public static readonly System.Version version = OVRP_1_1_0_PROD.version;

		public static Bool ovrp_GetInitialized()
		{
			return mockObj.ovrp_GetInitialized();
		}

		public static IntPtr _ovrp_GetVersion()
		{
			return mockObj._ovrp_GetVersion();
		}

		public static string ovrp_GetVersion() { return OVRP_1_1_0_PROD.ovrp_GetVersion(); }

		public static IntPtr _ovrp_GetNativeSDKVersion()
		{
			return mockObj._ovrp_GetNativeSDKVersion();
		}

		public static string ovrp_GetNativeSDKVersion() { return OVRP_1_1_0_PROD.ovrp_GetNativeSDKVersion(); }

		public static IntPtr ovrp_GetAudioOutId()
		{
			return mockObj.ovrp_GetAudioOutId();
		}

		public static IntPtr ovrp_GetAudioInId()
		{
			return mockObj.ovrp_GetAudioInId();
		}

		public static float ovrp_GetEyeTextureScale()
		{
			return mockObj.ovrp_GetEyeTextureScale();
		}

		public static Bool ovrp_SetEyeTextureScale(float value)
		{
			return mockObj.ovrp_SetEyeTextureScale(value);
		}

		public static Bool ovrp_GetTrackingOrientationSupported()
		{
			return mockObj.ovrp_GetTrackingOrientationSupported();
		}

		public static Bool ovrp_GetTrackingOrientationEnabled()
		{
			return mockObj.ovrp_GetTrackingOrientationEnabled();
		}

		public static Bool ovrp_SetTrackingOrientationEnabled(Bool value)
		{
			return mockObj.ovrp_SetTrackingOrientationEnabled(value);
		}

		public static Bool ovrp_GetTrackingPositionSupported()
		{
			return mockObj.ovrp_GetTrackingPositionSupported();
		}

		public static Bool ovrp_GetTrackingPositionEnabled()
		{
			return mockObj.ovrp_GetTrackingPositionEnabled();
		}

		public static Bool ovrp_SetTrackingPositionEnabled(Bool value)
		{
			return mockObj.ovrp_SetTrackingPositionEnabled(value);
		}

		public static Bool ovrp_GetNodePresent(Node nodeId)
		{
			return mockObj.ovrp_GetNodePresent(nodeId);
		}

		public static Bool ovrp_GetNodeOrientationTracked(Node nodeId)
		{
			return mockObj.ovrp_GetNodeOrientationTracked(nodeId);
		}

		public static Bool ovrp_GetNodePositionTracked(Node nodeId)
		{
			return mockObj.ovrp_GetNodePositionTracked(nodeId);
		}

		public static Frustumf ovrp_GetNodeFrustum(Node nodeId)
		{
			return mockObj.ovrp_GetNodeFrustum(nodeId);
		}

		public static ControllerState ovrp_GetControllerState(uint controllerMask)
		{
			return mockObj.ovrp_GetControllerState(controllerMask);
		}

		public static int ovrp_GetSystemCpuLevel()
		{
			return mockObj.ovrp_GetSystemCpuLevel();
		}

		public static Bool ovrp_SetSystemCpuLevel(int value)
		{
			return mockObj.ovrp_SetSystemCpuLevel(value);
		}

		public static int ovrp_GetSystemGpuLevel()
		{
			return mockObj.ovrp_GetSystemGpuLevel();
		}

		public static Bool ovrp_SetSystemGpuLevel(int value)
		{
			return mockObj.ovrp_SetSystemGpuLevel(value);
		}

		public static Bool ovrp_GetSystemPowerSavingMode()
		{
			return mockObj.ovrp_GetSystemPowerSavingMode();
		}

		public static float ovrp_GetSystemDisplayFrequency()
		{
			return mockObj.ovrp_GetSystemDisplayFrequency();
		}

		public static int ovrp_GetSystemVSyncCount()
		{
			return mockObj.ovrp_GetSystemVSyncCount();
		}

		public static float ovrp_GetSystemVolume()
		{
			return mockObj.ovrp_GetSystemVolume();
		}

		public static BatteryStatus ovrp_GetSystemBatteryStatus()
		{
			return mockObj.ovrp_GetSystemBatteryStatus();
		}

		public static float ovrp_GetSystemBatteryLevel()
		{
			return mockObj.ovrp_GetSystemBatteryLevel();
		}

		public static float ovrp_GetSystemBatteryTemperature()
		{
			return mockObj.ovrp_GetSystemBatteryTemperature();
		}

		public static IntPtr _ovrp_GetSystemProductName()
		{
			return mockObj._ovrp_GetSystemProductName();
		}

		public static string ovrp_GetSystemProductName() { return OVRP_1_1_0_PROD.ovrp_GetSystemProductName(); }

		public static Bool ovrp_ShowSystemUI(PlatformUI ui)
		{
			return mockObj.ovrp_ShowSystemUI(ui);
		}

		public static Bool ovrp_GetAppMonoscopic()
		{
			return mockObj.ovrp_GetAppMonoscopic();
		}

		public static Bool ovrp_SetAppMonoscopic(Bool value)
		{
			return mockObj.ovrp_SetAppMonoscopic(value);
		}

		public static Bool ovrp_GetAppHasVrFocus()
		{
			return mockObj.ovrp_GetAppHasVrFocus();
		}

		public static Bool ovrp_GetAppShouldQuit()
		{
			return mockObj.ovrp_GetAppShouldQuit();
		}

		public static Bool ovrp_GetAppShouldRecenter()
		{
			return mockObj.ovrp_GetAppShouldRecenter();
		}

		public static IntPtr _ovrp_GetAppLatencyTimings()
		{
			return mockObj._ovrp_GetAppLatencyTimings();
		}

		public static string ovrp_GetAppLatencyTimings() { return OVRP_1_1_0_PROD.ovrp_GetAppLatencyTimings(); }

		public static Bool ovrp_GetUserPresent()
		{
			return mockObj.ovrp_GetUserPresent();
		}

		public static float ovrp_GetUserIPD()
		{
			return mockObj.ovrp_GetUserIPD();
		}

		public static Bool ovrp_SetUserIPD(float value)
		{
			return mockObj.ovrp_SetUserIPD(value);
		}

		public static float ovrp_GetUserEyeDepth()
		{
			return mockObj.ovrp_GetUserEyeDepth();
		}

		public static Bool ovrp_SetUserEyeDepth(float value)
		{
			return mockObj.ovrp_SetUserEyeDepth(value);
		}

		public static float ovrp_GetUserEyeHeight()
		{
			return mockObj.ovrp_GetUserEyeHeight();
		}

		public static Bool ovrp_SetUserEyeHeight(float value)
		{
			return mockObj.ovrp_SetUserEyeHeight(value);
		}
	}

	public class OVRP_1_3_0_TEST
	{
		public virtual Bool ovrp_GetEyeOcclusionMeshEnabled()
		{
			return OVRP_1_3_0_PROD.ovrp_GetEyeOcclusionMeshEnabled();
		}
		public virtual Bool ovrp_SetEyeOcclusionMeshEnabled(Bool value)
		{
			return OVRP_1_3_0_PROD.ovrp_SetEyeOcclusionMeshEnabled(value);
		}
		public virtual Bool ovrp_GetSystemHeadphonesPresent()
		{
			return OVRP_1_3_0_PROD.ovrp_GetSystemHeadphonesPresent();
		}
	}

	public static class OVRP_1_3_0
	{
		public static OVRP_1_3_0_TEST mockObj = new OVRP_1_3_0_TEST();

		public static readonly System.Version version = OVRP_1_3_0_PROD.version;

		public static Bool ovrp_GetEyeOcclusionMeshEnabled()
		{
			return mockObj.ovrp_GetEyeOcclusionMeshEnabled();
		}
		public static Bool ovrp_SetEyeOcclusionMeshEnabled(Bool value)
		{
			return mockObj.ovrp_SetEyeOcclusionMeshEnabled(value);
		}
		public static Bool ovrp_GetSystemHeadphonesPresent()
		{
			return mockObj.ovrp_GetSystemHeadphonesPresent();
		}
	}


	public class OVRP_1_9_0_TEST
	{
		public virtual SystemHeadset ovrp_GetSystemHeadsetType()
		{
			return OVRP_1_9_0_PROD.ovrp_GetSystemHeadsetType();
		}

		public virtual Controller ovrp_GetActiveController()
		{
			return OVRP_1_9_0_PROD.ovrp_GetActiveController();
		}

		public virtual Controller ovrp_GetConnectedControllers()
		{
			return OVRP_1_9_0_PROD.ovrp_GetConnectedControllers();
		}

		public virtual Bool ovrp_GetBoundaryGeometry2(BoundaryType boundaryType, IntPtr points, ref int pointsCount)
		{
			return OVRP_1_9_0_PROD.ovrp_GetBoundaryGeometry2(boundaryType, points, ref pointsCount);
		}

		public virtual AppPerfStats ovrp_GetAppPerfStats()
		{
			return OVRP_1_9_0_PROD.ovrp_GetAppPerfStats();
		}

		public virtual Bool ovrp_ResetAppPerfStats()
		{
			return OVRP_1_9_0_PROD.ovrp_ResetAppPerfStats();
		}
	}

	public static class OVRP_1_9_0
	{
		public static OVRP_1_9_0_TEST mockObj = new OVRP_1_9_0_TEST();

		public static readonly System.Version version = OVRP_1_9_0_PROD.version;

		public static SystemHeadset ovrp_GetSystemHeadsetType()
		{
			return mockObj.ovrp_GetSystemHeadsetType();
		}

		public static Controller ovrp_GetActiveController()
		{
			return mockObj.ovrp_GetActiveController();
		}

		public static Controller ovrp_GetConnectedControllers()
		{
			return mockObj.ovrp_GetConnectedControllers();
		}

		public static Bool ovrp_GetBoundaryGeometry2(BoundaryType boundaryType, IntPtr points, ref int pointsCount)
		{
			return mockObj.ovrp_GetBoundaryGeometry2(boundaryType, points, ref pointsCount);
		}

		public static AppPerfStats ovrp_GetAppPerfStats()
		{
			return mockObj.ovrp_GetAppPerfStats();
		}

		public static Bool ovrp_ResetAppPerfStats()
		{
			return mockObj.ovrp_ResetAppPerfStats();
		}
	}

	public class OVRP_1_12_0_TEST
	{
		public virtual float ovrp_GetAppFramerate()
		{
			return OVRP_1_12_0_PROD.ovrp_GetAppFramerate();
		}

		public virtual PoseStatef ovrp_GetNodePoseState(Step stepId, Node nodeId)
		{
			return OVRP_1_12_0_PROD.ovrp_GetNodePoseState(stepId, nodeId);
		}

		public virtual ControllerState2 ovrp_GetControllerState2(uint controllerMask)
		{
			return OVRP_1_12_0_PROD.ovrp_GetControllerState2(controllerMask);
		}
	}

	public static class OVRP_1_12_0
	{
		public static OVRP_1_12_0_TEST mockObj = new OVRP_1_12_0_TEST();

		public static readonly System.Version version = OVRP_1_12_0_PROD.version;

		public static float ovrp_GetAppFramerate()
		{
			return mockObj.ovrp_GetAppFramerate();
		}

		public static PoseStatef ovrp_GetNodePoseState(Step stepId, Node nodeId)
		{
			return mockObj.ovrp_GetNodePoseState(stepId, nodeId);
		}

		public static ControllerState2 ovrp_GetControllerState2(uint controllerMask)
		{
			return mockObj.ovrp_GetControllerState2(controllerMask);
		}
	}

	public class OVRP_1_44_0_TEST
	{
		public virtual Result ovrp_GetHandTrackingEnabled(ref Bool handTrackingEnabled)
		{
			return OVRP_1_44_0_PROD.ovrp_GetHandTrackingEnabled(ref handTrackingEnabled);
		}

		public virtual Result ovrp_GetHandState(Step stepId, Hand hand, out HandStateInternal handState)
		{
			return OVRP_1_44_0_PROD.ovrp_GetHandState(stepId, hand, out handState);
		}

		public virtual Result ovrp_GetSkeleton(SkeletonType skeletonType, out Skeleton skeleton)
		{
			return OVRP_1_44_0_PROD.ovrp_GetSkeleton(skeletonType, out skeleton);
		}

		public virtual Result ovrp_GetMesh(MeshType meshType, System.IntPtr meshPtr)
		{
			return OVRP_1_44_0_PROD.ovrp_GetMesh(meshType, meshPtr);
		}

		public virtual Result ovrp_OverrideExternalCameraFov(int cameraId, Bool useOverriddenFov, ref Fovf fov)
		{
			return OVRP_1_44_0_PROD.ovrp_OverrideExternalCameraFov(cameraId, useOverriddenFov, ref fov);
		}

		public virtual Result ovrp_GetUseOverriddenExternalCameraFov(int cameraId, out Bool useOverriddenFov)
		{
			return OVRP_1_44_0_PROD.ovrp_GetUseOverriddenExternalCameraFov(cameraId, out useOverriddenFov);
		}

		public virtual Result ovrp_OverrideExternalCameraStaticPose(int cameraId, Bool useOverriddenPose, ref Posef poseInStageOrigin)
		{
			return OVRP_1_44_0_PROD.ovrp_OverrideExternalCameraStaticPose(cameraId, useOverriddenPose, ref poseInStageOrigin);
		}

		public virtual Result ovrp_GetUseOverriddenExternalCameraStaticPose(int cameraId, out Bool useOverriddenStaticPose)
		{
			return OVRP_1_44_0_PROD.ovrp_GetUseOverriddenExternalCameraStaticPose(cameraId, out useOverriddenStaticPose);
		}

		public virtual Result ovrp_ResetDefaultExternalCamera()
		{
			return OVRP_1_44_0_PROD.ovrp_ResetDefaultExternalCamera();
		}

		public virtual Result ovrp_SetDefaultExternalCamera(string cameraName, ref CameraIntrinsics cameraIntrinsics, ref CameraExtrinsics cameraExtrinsics)
		{
			return OVRP_1_44_0_PROD.ovrp_SetDefaultExternalCamera(cameraName, ref cameraIntrinsics, ref cameraExtrinsics);
		}

		public virtual Result ovrp_GetLocalTrackingSpaceRecenterCount(ref int recenterCount)
		{
			return OVRP_1_44_0_PROD.ovrp_GetLocalTrackingSpaceRecenterCount(ref recenterCount);
		}
	}

	public static class OVRP_1_44_0
	{
		public static OVRP_1_44_0_TEST mockObj = new OVRP_1_44_0_TEST();

		public static readonly System.Version version = OVRP_1_44_0_PROD.version;

		public static Result ovrp_GetHandTrackingEnabled(ref Bool handTrackingEnabled)
		{
			return mockObj.ovrp_GetHandTrackingEnabled(ref handTrackingEnabled);
		}

		public static Result ovrp_GetHandState(Step stepId, Hand hand, out HandStateInternal handState)
		{
			return mockObj.ovrp_GetHandState(stepId, hand, out handState);
		}

		public static Result ovrp_GetSkeleton(SkeletonType skeletonType, out Skeleton skeleton)
		{
			return mockObj.ovrp_GetSkeleton(skeletonType, out skeleton);
		}

		public static Result ovrp_GetMesh(MeshType meshType, System.IntPtr meshPtr)
		{
			return mockObj.ovrp_GetMesh(meshType, meshPtr);
		}

		public static Result ovrp_OverrideExternalCameraFov(int cameraId, Bool useOverriddenFov, ref Fovf fov)
		{
			return mockObj.ovrp_OverrideExternalCameraFov(cameraId, useOverriddenFov, ref fov);
		}

		public static Result ovrp_GetUseOverriddenExternalCameraFov(int cameraId, out Bool useOverriddenFov)
		{
			return mockObj.ovrp_GetUseOverriddenExternalCameraFov(cameraId, out useOverriddenFov);
		}

		public static Result ovrp_OverrideExternalCameraStaticPose(int cameraId, Bool useOverriddenPose, ref Posef poseInStageOrigin)
		{
			return mockObj.ovrp_OverrideExternalCameraStaticPose(cameraId, useOverriddenPose, ref poseInStageOrigin);
		}

		public static Result ovrp_GetUseOverriddenExternalCameraStaticPose(int cameraId, out Bool useOverriddenStaticPose)
		{
			return mockObj.ovrp_GetUseOverriddenExternalCameraStaticPose(cameraId, out useOverriddenStaticPose);
		}

		public static Result ovrp_ResetDefaultExternalCamera()
		{
			return mockObj.ovrp_ResetDefaultExternalCamera();
		}

		public static Result ovrp_SetDefaultExternalCamera(string cameraName, ref CameraIntrinsics cameraIntrinsics, ref CameraExtrinsics cameraExtrinsics)
		{
			return mockObj.ovrp_SetDefaultExternalCamera(cameraName, ref cameraIntrinsics, ref cameraExtrinsics);
		}

		public static Result ovrp_GetLocalTrackingSpaceRecenterCount(ref int recenterCount)
		{
			return mockObj.ovrp_GetLocalTrackingSpaceRecenterCount(ref recenterCount);
		}
	}

	public class OVRP_1_55_0_TEST
	{
		public virtual Result ovrp_GetBodyConfig(out BodyConfig bodyConfig)
		{
			return OVRP_1_55_0_PROD.ovrp_GetBodyConfig(out bodyConfig);
		}

		public virtual Result ovrp_SetBodyConfig(ref BodyConfig bodyConfig)
		{
			return OVRP_1_55_0_PROD.ovrp_SetBodyConfig(ref bodyConfig);
		}

		public virtual Result ovrp_IsBodyReady(ref Bool status)
		{
			return OVRP_1_55_0_PROD.ovrp_IsBodyReady(ref status);
		}

		public virtual Result ovrp_GetBodyState(Step stepId, int frameIndex, out BodyStateInternal bodyState)
		{
			return OVRP_1_55_0_PROD.ovrp_GetBodyState(stepId, frameIndex, out bodyState);
		}

		public virtual Result ovrp_GetSkeleton2(SkeletonType skeletonType, out Skeleton2Internal skeleton)
		{
			return OVRP_1_55_0_PROD.ovrp_GetSkeleton2(skeletonType, out skeleton);
		}

		public virtual Result ovrp_PollEvent(ref EventDataBuffer eventDataBuffer)
		{
			return OVRP_1_55_0_PROD.ovrp_PollEvent(ref eventDataBuffer);
		}

		public virtual Result ovrp_GetNativeXrApiType(out XrApi xrApi)
		{
			return OVRP_1_55_0_PROD.ovrp_GetNativeXrApiType(out xrApi);
		}

		public virtual Result ovrp_GetNativeOpenXRHandles(out UInt64 xrInstance, out UInt64 xrSession)
		{
			return OVRP_1_55_0_PROD.ovrp_GetNativeOpenXRHandles(out xrInstance, out xrSession);
		}
	}

	public static class OVRP_1_55_0
	{
		public static OVRP_1_55_0_TEST mockObj = new OVRP_1_55_0_TEST();

		public static readonly System.Version version = OVRP_1_55_0_PROD.version;

		public static Result ovrp_GetBodyConfig(out BodyConfig bodyConfig)
		{
			return mockObj.ovrp_GetBodyConfig(out bodyConfig);
		}

		public static Result ovrp_SetBodyConfig(ref BodyConfig bodyConfig)
		{
			return mockObj.ovrp_SetBodyConfig(ref bodyConfig);
		}

		public static Result ovrp_IsBodyReady(ref Bool status)
		{
			return mockObj.ovrp_IsBodyReady(ref status);
		}

		public static Result ovrp_GetBodyState(Step stepId, int frameIndex, out BodyStateInternal bodyState)
		{
			return mockObj.ovrp_GetBodyState(stepId, frameIndex, out bodyState);
		}

		public static Result ovrp_GetSkeleton2(SkeletonType skeletonType, out Skeleton2Internal skeleton)
		{
			return mockObj.ovrp_GetSkeleton2(skeletonType, out skeleton);
		}

		public static Result ovrp_PollEvent(ref EventDataBuffer eventDataBuffer)
		{
			return mockObj.ovrp_PollEvent(ref eventDataBuffer);
		}

		public static Result ovrp_GetNativeXrApiType(out XrApi xrApi)
		{
			return mockObj.ovrp_GetNativeXrApiType(out xrApi);
		}

		public static Result ovrp_GetNativeOpenXRHandles(out UInt64 xrInstance, out UInt64 xrSession)
		{
			return mockObj.ovrp_GetNativeOpenXRHandles(out xrInstance, out xrSession);
		}
	}

	public class OVRP_1_64_0_TEST
	{
		public virtual Result ovrp_LocateSpace(ref Posef location, ref UInt64 space, TrackingOrigin trackingOrigin)
		{
			return OVRP_1_64_0_PROD.ovrp_LocateSpace(ref location, ref space, trackingOrigin);
		}
	}

	public static class OVRP_1_64_0
	{
		public static OVRP_1_64_0_TEST mockObj = new OVRP_1_64_0_TEST();

		public static readonly System.Version version = OVRP_1_64_0_PROD.version;

		public static Result ovrp_LocateSpace(ref Posef location, ref UInt64 space, TrackingOrigin trackingOrigin)
		{
			return mockObj.ovrp_LocateSpace(ref location, ref space, trackingOrigin);
		}
	}

	public class OVRP_1_65_0_TEST
	{
		public virtual Result ovrp_KtxLoadFromMemory(ref IntPtr data, uint length, ref System.IntPtr texture)
		{
			return OVRP_1_65_0_PROD.ovrp_KtxLoadFromMemory(ref data, length, ref texture);
		}

		public virtual Result ovrp_KtxTextureWidth(IntPtr texture, ref uint width)
		{
			return OVRP_1_65_0_PROD.ovrp_KtxTextureWidth(texture, ref width);
		}

		public virtual Result ovrp_KtxTextureHeight(IntPtr texture, ref uint height)
		{
			return OVRP_1_65_0_PROD.ovrp_KtxTextureHeight(texture, ref height);
		}

		public virtual Result ovrp_KtxTranscode(IntPtr texture, uint format)
		{
			return OVRP_1_65_0_PROD.ovrp_KtxTranscode(texture, format);
		}

		public virtual Result ovrp_KtxGetTextureData(IntPtr texture, IntPtr data, uint bufferSize)
		{
			return OVRP_1_65_0_PROD.ovrp_KtxGetTextureData(texture, data, bufferSize);
		}

		public virtual Result ovrp_KtxTextureSize(IntPtr texture, ref uint size)
		{
			return OVRP_1_65_0_PROD.ovrp_KtxTextureSize(texture, ref size);
		}

		public virtual Result ovrp_KtxDestroy(IntPtr texture)
		{
			return OVRP_1_65_0_PROD.ovrp_KtxDestroy(texture);
		}

		public virtual Result ovrp_DestroySpace(ref UInt64 space)
		{
			return OVRP_1_65_0_PROD.ovrp_DestroySpace(ref space);
		}
	}

	public static class OVRP_1_65_0
	{
		public static OVRP_1_65_0_TEST mockObj = new OVRP_1_65_0_TEST();

		public static readonly System.Version version = OVRP_1_65_0_PROD.version;

		public static Result ovrp_KtxLoadFromMemory(ref IntPtr data, uint length, ref System.IntPtr texture)
		{
			return mockObj.ovrp_KtxLoadFromMemory(ref data, length, ref texture);
		}

		public static Result ovrp_KtxTextureWidth(IntPtr texture, ref uint width)
		{
			return mockObj.ovrp_KtxTextureWidth(texture, ref width);
		}

		public static Result ovrp_KtxTextureHeight(IntPtr texture, ref uint height)
		{
			return mockObj.ovrp_KtxTextureHeight(texture, ref height);
		}

		public static Result ovrp_KtxTranscode(IntPtr texture, uint format)
		{
			return mockObj.ovrp_KtxTranscode(texture, format);
		}

		public static Result ovrp_KtxGetTextureData(IntPtr texture, IntPtr data, uint bufferSize)
		{
			return mockObj.ovrp_KtxGetTextureData(texture, data, bufferSize);
		}

		public static Result ovrp_KtxTextureSize(IntPtr texture, ref uint size)
		{
			return mockObj.ovrp_KtxTextureSize(texture, ref size);
		}

		public static Result ovrp_KtxDestroy(IntPtr texture)
		{
			return mockObj.ovrp_KtxDestroy(texture);
		}

		public static Result ovrp_DestroySpace(ref UInt64 space)
		{
			return mockObj.ovrp_DestroySpace(ref space);
		}
	}

	public class OVRP_1_66_0_TEST
	{
		public virtual Result ovrp_GetInsightPassthroughInitializationState()
		{
			return OVRP_1_66_0_PROD.ovrp_GetInsightPassthroughInitializationState();
		}

		public virtual Result ovrp_Media_IsCastingToRemoteClient(out Bool isCasting)
		{
			return OVRP_1_66_0_PROD.ovrp_Media_IsCastingToRemoteClient(out isCasting);
		}
	}

	public static class OVRP_1_66_0
	{
		public static OVRP_1_66_0_TEST mockObj = new OVRP_1_66_0_TEST();

		public static readonly System.Version version = OVRP_1_66_0_PROD.version;

		public static Result ovrp_GetInsightPassthroughInitializationState()
		{
			return mockObj.ovrp_GetInsightPassthroughInitializationState();
		}

		public static Result ovrp_Media_IsCastingToRemoteClient(out Bool isCasting)
		{
			return mockObj.ovrp_Media_IsCastingToRemoteClient(out isCasting);
		}
	}

	public class OVRP_1_67_0_TEST
	{
		public virtual Result ovrp_QuerySpatialEntity(ref SpatialEntityQueryInfo queryInfo, out UInt64 requestId)
		{
			return OVRP_1_67_0_PROD.ovrp_QuerySpatialEntity(ref queryInfo, out requestId);
		}

		public virtual Result ovrp_GetControllerState5(uint controllerMask, ref ControllerState5 controllerState)
		{
			return OVRP_1_67_0_PROD.ovrp_GetControllerState5(controllerMask, ref controllerState);
		}

		public virtual Result ovrp_SetControllerLocalizedVibration(Controller controllerMask, HapticsLocation hapticsLocationMask, float frequency, float amplitude)
		{
			return OVRP_1_67_0_PROD.ovrp_SetControllerLocalizedVibration(controllerMask, hapticsLocationMask, frequency, amplitude);
		}
	}

	public static class OVRP_1_67_0
	{
		public static OVRP_1_67_0_TEST mockObj = new OVRP_1_67_0_TEST();

		public static readonly System.Version version = OVRP_1_67_0_PROD.version;

		public static Result ovrp_QuerySpatialEntity(ref SpatialEntityQueryInfo queryInfo, out UInt64 requestId)
		{
			return mockObj.ovrp_QuerySpatialEntity(ref queryInfo, out requestId);
		}

		public static Result ovrp_GetControllerState5(uint controllerMask, ref ControllerState5 controllerState)
		{
			return mockObj.ovrp_GetControllerState5(controllerMask, ref controllerState);
		}

		public static Result ovrp_SetControllerLocalizedVibration(Controller controllerMask, HapticsLocation hapticsLocationMask, float frequency, float amplitude)
		{
			return mockObj.ovrp_SetControllerLocalizedVibration(controllerMask, hapticsLocationMask, frequency, amplitude);
		}
	}

	public class OVRP_1_68_0_TEST
	{
		public virtual Result ovrp_LoadRenderModel(UInt64 modelKey, uint bufferInputCapacity, ref uint bufferCountOuput, IntPtr buffer)
		{
			return OVRP_1_68_0_PROD.ovrp_LoadRenderModel(modelKey, bufferInputCapacity, ref bufferCountOuput, buffer);
		}

		public virtual Result ovrp_GetRenderModelPaths(uint index, IntPtr path)
		{
			return OVRP_1_68_0_PROD.ovrp_GetRenderModelPaths(index, path);
		}

		public virtual Result ovrp_GetRenderModelProperties(string path, out RenderModelPropertiesInternal properties)
		{
			return OVRP_1_68_0_PROD.ovrp_GetRenderModelProperties(path, out properties);
		}

		public virtual Result ovrp_GetFaceTrackingEnabled(ref Bool faceTrackingEnabled)
		{
			return OVRP_1_68_0_PROD.ovrp_GetFaceTrackingEnabled(ref faceTrackingEnabled);
		}

		public virtual Result ovrp_GetFaceState(Step stepId, int frameIndex, out FaceStateInternal faceState)
		{
			return OVRP_1_68_0_PROD.ovrp_GetFaceState(stepId, frameIndex, out faceState);
		}

		public virtual Result ovrp_GetEyeTrackingEnabled(ref Bool eyeTrackingEnabled)
		{
			return OVRP_1_68_0_PROD.ovrp_GetEyeTrackingEnabled(ref eyeTrackingEnabled);
		}

		public virtual Result ovrp_GetEyeGazesState(Step stepId, int frameIndex,
			out EyeGazesStateInternal eyeGazesState)
		{
			return OVRP_1_68_0_PROD.ovrp_GetEyeGazesState(stepId, frameIndex, out eyeGazesState);
		}

		public virtual Result ovrp_StartKeyboardTracking(UInt64 trackedKeyboardId)
		{
			return OVRP_1_68_0_PROD.ovrp_StartKeyboardTracking(trackedKeyboardId);
		}

		public virtual Result ovrp_StopKeyboardTracking()
		{
			return OVRP_1_68_0_PROD.ovrp_StopKeyboardTracking();
		}

		public virtual Result ovrp_GetKeyboardState(Step stepId, int frameIndex, out KeyboardState keyboardState)
		{
			return OVRP_1_68_0_PROD.ovrp_GetKeyboardState(stepId, frameIndex, out keyboardState);
		}

		public virtual Result ovrp_GetSystemKeyboardDescription(TrackedKeyboardQueryFlags keyboardQueryFlags, out KeyboardDescription keyboardDescription)
		{
			return OVRP_1_68_0_PROD.ovrp_GetSystemKeyboardDescription(keyboardQueryFlags, out keyboardDescription);
		}

		public virtual Result ovrp_GetSpaceBounded2D(ref UInt64 space, out Rectf rect)
		{
			return OVRP_1_68_0_PROD.ovrp_GetSpaceBounded2D(ref space, out rect);
		}

		public virtual Result ovrp_GetSpaceBounded3D(ref UInt64 space, out Boundsf bounds)
		{
			return OVRP_1_68_0_PROD.ovrp_GetSpaceBounded3D(ref space, out bounds);
		}

		public virtual Result ovrp_GetSpaceSemanticLabels(ref UInt64 space, ref SpaceSemanticLabelInternal labelsInternal)
		{
			return OVRP_1_68_0_PROD.ovrp_GetSpaceSemanticLabels(ref space, ref labelsInternal);
		}

		public virtual Result ovrp_GetSpatialEntityContainer(ref UInt64 space, ref SpatialEntityContainerInternal containerInternal)
		{
			return OVRP_1_68_0_PROD.ovrp_GetSpatialEntityContainer(ref space, ref containerInternal);
		}

		public virtual Result ovrp_GetSpaceRoomLayout(ref UInt64 space, ref RoomLayoutInternal roomLayoutInternal)
		{
			return OVRP_1_68_0_PROD.ovrp_GetSpaceRoomLayout(ref space, ref roomLayoutInternal);
		}

		public virtual Result ovrp_RequestSceneCapture(ref SceneCaptureRequestInternal request, out UInt64 requestId)
		{
			return OVRP_1_68_0_PROD.ovrp_RequestSceneCapture(ref request, out requestId);
		}

		public virtual Result ovrp_QuerySpatialEntity2(ref SpatialEntityQueryInfo2 queryInfo, out UInt64 requestId)
		{
			return OVRP_1_68_0_PROD.ovrp_QuerySpatialEntity2(ref queryInfo, out requestId);
		}

		public virtual Result ovrp_SetInsightPassthroughKeyboardHandsIntensity(int layerId, InsightPassthroughKeyboardHandsIntensity intensity)
		{
			return OVRP_1_68_0_PROD.ovrp_SetInsightPassthroughKeyboardHandsIntensity(layerId, intensity);
		}
	}

	public static class OVRP_1_68_0
	{
		public static OVRP_1_68_0_TEST mockObj = new OVRP_1_68_0_TEST();

		public static readonly System.Version version = OVRP_1_68_0_PROD.version;

		public const int OVRP_RENDER_MODEL_MAX_PATH_LENGTH = OVRP_1_68_0_PROD.OVRP_RENDER_MODEL_MAX_PATH_LENGTH;
		public const int OVRP_RENDER_MODEL_MAX_NAME_LENGTH = OVRP_1_68_0_PROD.OVRP_RENDER_MODEL_MAX_NAME_LENGTH;

		public static Result ovrp_LoadRenderModel(UInt64 modelKey, uint bufferInputCapacity, ref uint bufferCountOuput, IntPtr buffer)
		{
			return mockObj.ovrp_LoadRenderModel(modelKey, bufferInputCapacity, ref bufferCountOuput, buffer);
		}

		public static Result ovrp_GetRenderModelPaths(uint index, IntPtr path)
		{
			return mockObj.ovrp_GetRenderModelPaths(index, path);
		}

		public static Result ovrp_GetRenderModelProperties(string path, out RenderModelPropertiesInternal properties)
		{
			return mockObj.ovrp_GetRenderModelProperties(path, out properties);
		}

		public static Result ovrp_GetFaceTrackingEnabled(ref Bool faceTrackingEnabled)
		{
			return mockObj.ovrp_GetFaceTrackingEnabled(ref faceTrackingEnabled);
		}

		public static Result ovrp_GetFaceState(Step stepId, int frameIndex, out FaceStateInternal faceState)
		{
			return mockObj.ovrp_GetFaceState(stepId, frameIndex, out faceState);
		}

		public static Result ovrp_GetEyeTrackingEnabled(ref Bool eyeTrackingEnabled)
		{
			return mockObj.ovrp_GetEyeTrackingEnabled(ref eyeTrackingEnabled);
		}

		public static Result ovrp_GetEyeGazesState(Step stepId, int frameIndex,
			out EyeGazesStateInternal eyeGazesState)
		{
			return mockObj.ovrp_GetEyeGazesState(stepId, frameIndex, out eyeGazesState);
		}

		public static Result ovrp_StartKeyboardTracking(UInt64 trackedKeyboardId)
		{
			return mockObj.ovrp_StartKeyboardTracking(trackedKeyboardId);
		}

		public static Result ovrp_StopKeyboardTracking()
		{
			return mockObj.ovrp_StopKeyboardTracking();
		}

		public static Result ovrp_GetKeyboardState(Step stepId, int frameIndex, out KeyboardState keyboardState)
		{
			return mockObj.ovrp_GetKeyboardState(stepId, frameIndex, out keyboardState);
		}

		public static Result ovrp_GetSystemKeyboardDescription(TrackedKeyboardQueryFlags keyboardQueryFlags, out KeyboardDescription keyboardDescription)
		{
			return mockObj.ovrp_GetSystemKeyboardDescription(keyboardQueryFlags, out keyboardDescription);
		}

		public static Result ovrp_GetSpaceBounded2D(ref UInt64 space, out Rectf rect)
		{
			return mockObj.ovrp_GetSpaceBounded2D(ref space, out rect);
		}

		public static Result ovrp_GetSpaceBounded3D(ref UInt64 space, out Boundsf bounds)
		{
			return mockObj.ovrp_GetSpaceBounded3D(ref space, out bounds);
		}

		public static Result ovrp_GetSpaceSemanticLabels(ref UInt64 space, ref SpaceSemanticLabelInternal labelsInternal)
		{
			return mockObj.ovrp_GetSpaceSemanticLabels(ref space, ref labelsInternal);
		}

		public static Result ovrp_GetSpatialEntityContainer(ref UInt64 space, ref SpatialEntityContainerInternal containerInternal)
		{
			return mockObj.ovrp_GetSpatialEntityContainer(ref space, ref containerInternal);
		}

		public static Result ovrp_GetSpaceRoomLayout(ref UInt64 space, ref RoomLayoutInternal roomLayoutInternal)
		{
			return mockObj.ovrp_GetSpaceRoomLayout(ref space, ref roomLayoutInternal);
		}

		public static Result ovrp_RequestSceneCapture(ref SceneCaptureRequestInternal request, out UInt64 requestId)
		{
			return mockObj.ovrp_RequestSceneCapture(ref request, out requestId);
		}

		public static Result ovrp_QuerySpatialEntity2(ref SpatialEntityQueryInfo2 queryInfo, out UInt64 requestId)
		{
			return mockObj.ovrp_QuerySpatialEntity2(ref queryInfo, out requestId);
		}

		public static Result ovrp_SetInsightPassthroughKeyboardHandsIntensity(int layerId, InsightPassthroughKeyboardHandsIntensity intensity)
		{
			return mockObj.ovrp_SetInsightPassthroughKeyboardHandsIntensity(layerId, intensity);
		}
	}

	public class OVRP_1_69_0_TEST
	{
		public virtual Result ovrp_GetNodePoseStateImmediate(Node nodeId, out PoseStatef nodePoseState)
		{
			return OVRP_1_69_0_PROD.ovrp_GetNodePoseStateImmediate(nodeId, out nodePoseState);
		}

		public virtual Result ovrp_SetNativeSDKPropertyBool(int propertyEnum, Bool enabled)
		{
			return OVRP_1_69_0_PROD.ovrp_SetNativeSDKPropertyBool(propertyEnum, enabled);
		}

		public virtual Result ovrp_GetNativeSDKPropertyBool(int propertyEnum, ref Bool enabled)
		{
			return OVRP_1_69_0_PROD.ovrp_GetNativeSDKPropertyBool(propertyEnum, ref enabled);
		}
	}

	public static class OVRP_1_69_0
	{
		public static OVRP_1_69_0_TEST mockObj = new OVRP_1_69_0_TEST();

		public static readonly System.Version version = OVRP_1_69_0_PROD.version;

		public static Result ovrp_GetNodePoseStateImmediate(Node nodeId, out PoseStatef nodePoseState)
		{
			return mockObj.ovrp_GetNodePoseStateImmediate(nodeId, out nodePoseState);
		}

		public static Result ovrp_SetNativeSDKPropertyBool(int propertyEnum, Bool enabled)
		{
			return mockObj.ovrp_SetNativeSDKPropertyBool(propertyEnum, enabled);
		}

		public static Result ovrp_GetNativeSDKPropertyBool(int propertyEnum, ref Bool enabled)
		{
			return mockObj.ovrp_GetNativeSDKPropertyBool(propertyEnum, ref enabled);
		}
	}

	public class OVRP_1_70_0_TEST
	{
		public virtual Result ovrp_SetLogCallback2(OVRPlugin.LogCallback2DelegateType logCallback)
		{
			return OVRP_1_70_0_PROD.ovrp_SetLogCallback2(logCallback);
		}

		public virtual void ovrp_UnityOpenXR_SetClientVersion(int majorVersion, int minorVersion, int patchVersion)
		{
			OVRP_1_70_0_PROD.ovrp_UnityOpenXR_SetClientVersion(majorVersion, minorVersion, patchVersion);
		}

		public virtual IntPtr ovrp_UnityOpenXR_HookGetInstanceProcAddr(IntPtr func)
		{
			return OVRP_1_70_0_PROD.ovrp_UnityOpenXR_HookGetInstanceProcAddr(func);
		}

		public virtual Result ovrp_UnityOpenXR_OnInstanceCreate(UInt64 xrInstance)
		{
			return OVRP_1_70_0_PROD.ovrp_UnityOpenXR_OnInstanceCreate(xrInstance);
		}

		public virtual void ovrp_UnityOpenXR_OnInstanceDestroy(UInt64 xrInstance)
		{
			OVRP_1_70_0_PROD.ovrp_UnityOpenXR_OnInstanceDestroy(xrInstance);
		}

		public virtual void ovrp_UnityOpenXR_OnSessionCreate(UInt64 xrSession)
		{
			OVRP_1_70_0_PROD.ovrp_UnityOpenXR_OnSessionCreate(xrSession);
		}

		public virtual void ovrp_UnityOpenXR_OnAppSpaceChange(UInt64 xrSpace)
		{
			OVRP_1_70_0_PROD.ovrp_UnityOpenXR_OnAppSpaceChange(xrSpace);
		}

		public virtual void ovrp_UnityOpenXR_OnSessionStateChange(int oldState, int newState)
		{
			OVRP_1_70_0_PROD.ovrp_UnityOpenXR_OnSessionStateChange(oldState, newState);
		}

		public virtual void ovrp_UnityOpenXR_OnSessionBegin(UInt64 xrSession)
		{
			OVRP_1_70_0_PROD.ovrp_UnityOpenXR_OnSessionBegin(xrSession);
		}

		public virtual void ovrp_UnityOpenXR_OnSessionEnd(UInt64 xrSession)
		{
			OVRP_1_70_0_PROD.ovrp_UnityOpenXR_OnSessionEnd(xrSession);
		}

		public virtual void ovrp_UnityOpenXR_OnSessionExiting(UInt64 xrSession)
		{
			OVRP_1_70_0_PROD.ovrp_UnityOpenXR_OnSessionExiting(xrSession);
		}

		public virtual void ovrp_UnityOpenXR_OnSessionDestroy(UInt64 xrSession)
		{
			OVRP_1_70_0_PROD.ovrp_UnityOpenXR_OnSessionDestroy(xrSession);
		}

		public virtual Result ovrp_ShareSpatialEntity(UInt64[] spaces, UInt32 numSpaces, Principal[] principals,
			UInt32 numPrincipals, out UInt64 requestId)
		{
			return OVRP_1_70_0_PROD.ovrp_ShareSpatialEntity(spaces, numSpaces, principals,
				numPrincipals, out requestId);
		}
	}

	public static class OVRP_1_70_0
	{
		public static OVRP_1_70_0_TEST mockObj = new OVRP_1_70_0_TEST();

		public static readonly System.Version version = OVRP_1_70_0_PROD.version;

		public static Result ovrp_SetLogCallback2(OVRPlugin.LogCallback2DelegateType logCallback)
		{
			return mockObj.ovrp_SetLogCallback2(logCallback);
		}

		public static void ovrp_UnityOpenXR_SetClientVersion(int majorVersion, int minorVersion, int patchVersion)
		{
			mockObj.ovrp_UnityOpenXR_SetClientVersion(majorVersion, minorVersion, patchVersion);
		}

		public static IntPtr ovrp_UnityOpenXR_HookGetInstanceProcAddr(IntPtr func)
		{
			return mockObj.ovrp_UnityOpenXR_HookGetInstanceProcAddr(func);
		}

		public static Result ovrp_UnityOpenXR_OnInstanceCreate(UInt64 xrInstance)
		{
			return mockObj.ovrp_UnityOpenXR_OnInstanceCreate(xrInstance);
		}

		public static void ovrp_UnityOpenXR_OnInstanceDestroy(UInt64 xrInstance)
		{
			mockObj.ovrp_UnityOpenXR_OnInstanceDestroy(xrInstance);
		}

		public static void ovrp_UnityOpenXR_OnSessionCreate(UInt64 xrSession)
		{
			mockObj.ovrp_UnityOpenXR_OnSessionCreate(xrSession);
		}

		public static void ovrp_UnityOpenXR_OnAppSpaceChange(UInt64 xrSpace)
		{
			mockObj.ovrp_UnityOpenXR_OnAppSpaceChange(xrSpace);
		}

		public static void ovrp_UnityOpenXR_OnSessionStateChange(int oldState, int newState)
		{
			mockObj.ovrp_UnityOpenXR_OnSessionStateChange(oldState, newState);
		}

		public static void ovrp_UnityOpenXR_OnSessionBegin(UInt64 xrSession)
		{
			mockObj.ovrp_UnityOpenXR_OnSessionBegin(xrSession);
		}

		public static void ovrp_UnityOpenXR_OnSessionEnd(UInt64 xrSession)
		{
			mockObj.ovrp_UnityOpenXR_OnSessionEnd(xrSession);
		}

		public static void ovrp_UnityOpenXR_OnSessionExiting(UInt64 xrSession)
		{
			mockObj.ovrp_UnityOpenXR_OnSessionExiting(xrSession);
		}

		public static void ovrp_UnityOpenXR_OnSessionDestroy(UInt64 xrSession)
		{
			mockObj.ovrp_UnityOpenXR_OnSessionDestroy(xrSession);
		}

		public static Result ovrp_ShareSpatialEntity(UInt64[] spaces, UInt32 numSpaces, Principal[] principals,
			UInt32 numPrincipals, out UInt64 requestId)
		{
			return mockObj.ovrp_ShareSpatialEntity(spaces, numSpaces, principals,
				numPrincipals, out requestId);
		}
	}

	public class OVRP_1_71_0_TEST
	{
		public virtual Result ovrp_IsInsightPassthroughSupported(ref Bool supported)
		{
			return OVRP_1_71_0_PROD.ovrp_IsInsightPassthroughSupported(ref supported);
		}

#if OVR_INTERNAL_CODE
		public virtual Result ovrp_GetKeyboardRawContrastParams(UInt64 trackedKeyboardId, out KeyboardTrackingContrastParams contrastParams)
		{
			return OVRP_1_71_0_PROD.ovrp_GetKeyboardRawContrastParams(trackedKeyboardId, out contrastParams);
		}

		public virtual Result ovrp_GetEyeTrackingSupported(out Bool eyeTrackingSupported)
		{
			return OVRP_1_71_0_PROD.ovrp_GetEyeTrackingSupported(out eyeTrackingSupported);
		}
#endif
	}

	public static class OVRP_1_71_0
	{
		public static OVRP_1_71_0_TEST mockObj = new OVRP_1_71_0_TEST();

		public static readonly System.Version version = OVRP_1_71_0_PROD.version;

		public static Result ovrp_IsInsightPassthroughSupported(ref Bool supported)
		{
			return mockObj.ovrp_IsInsightPassthroughSupported(ref supported);
		}

#if OVR_INTERNAL_CODE
		public static Result ovrp_GetKeyboardRawContrastParams(UInt64 trackedKeyboardId, out KeyboardTrackingContrastParams contrastParams)
		{
			return mockObj.ovrp_GetKeyboardRawContrastParams(trackedKeyboardId, out contrastParams);
		}

		public static Result ovrp_GetEyeTrackingSupported(out Bool eyeTrackingSupported)
		{
			return mockObj.ovrp_GetEyeTrackingSupported(out eyeTrackingSupported);
		}
#endif // OVR_INTERNAL_CODE
	}

}

#endif
#endif
