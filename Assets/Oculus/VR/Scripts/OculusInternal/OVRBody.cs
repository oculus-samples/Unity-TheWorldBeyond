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
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-90)]
public class OVRBody : MonoBehaviour,
	OVRSkeleton.IOVRSkeletonDataProvider,
	OVRSkeletonRenderer.IOVRSkeletonRendererDataProvider
{
	[Flags]
	public enum BodyConfigFlags
	{
		Disabled = OVRPlugin.BodyConfigFlags.Disabled,
		ThreePointTracking = OVRPlugin.BodyConfigFlags.ThreePointTracking,
	}

	public enum BodyModality
	{
		Unknown = OVRPlugin.BodyModality.Unknown,
		Sitting = OVRPlugin.BodyModality.Sitting,
		Standing = OVRPlugin.BodyModality.Standing
	}

	[SerializeField]
	private BodyConfigFlags _bodyConfig = BodyConfigFlags.ThreePointTracking;
	private OVRPlugin.BodyState _bodyState = new OVRPlugin.BodyState();

	public bool IsDataValid { get; private set; }
	public bool IsDataHighConfidence { get; private set; }
	public int SkeletonChangedCount { get; private set; }
	public BodyModality Modality { get; private set; }
	public Vector3 HipsBoneTranslation { get; private set; }

	void Awake()
	{
		SetBodyConfig(_bodyConfig);

		GetBodyState(OVRPlugin.Step.Render);
	}

	void Update()
	{
		GetBodyState(OVRPlugin.Step.Render);
	}

	private void FixedUpdate()
	{
		if (OVRPlugin.nativeXrApi != OVRPlugin.XrApi.OpenXR)
		{
			GetBodyState(OVRPlugin.Step.Physics);
		}
	}

	private void GetBodyState(OVRPlugin.Step step)
	{
		if (OVRPlugin.GetBodyState(step, ref _bodyState))
		{
			SkeletonChangedCount = _bodyState.SkeletonChangedCount;
			Modality = (BodyModality)_bodyState.BodyModality;
			HipsBoneTranslation = _bodyState.HipsBoneTranslation.FromFlippedZVector3f();

			IsDataValid = true;
			IsDataHighConfidence = true;
		}
		else
		{
			SkeletonChangedCount = 0;
			Modality = BodyModality.Unknown;
			HipsBoneTranslation = Vector3.zero;

			IsDataValid = false;
			IsDataHighConfidence = false;
		}
	}

	public bool SetBodyConfig(BodyConfigFlags flags)
	{
		OVRPlugin.BodyConfig config = new OVRPlugin.BodyConfig();

		if ((flags & OVRBody.BodyConfigFlags.ThreePointTracking) != 0)
			config.Flags |= (uint)OVRPlugin.BodyConfigFlags.ThreePointTracking;

		return OVRPlugin.SetBodyConfig(config);
	}

	OVRSkeleton.SkeletonType OVRSkeleton.IOVRSkeletonDataProvider.GetSkeletonType()
	{
		return OVRSkeleton.SkeletonType.Body;
	}

	OVRSkeleton.SkeletonPoseData OVRSkeleton.IOVRSkeletonDataProvider.GetSkeletonPoseData()
	{
		var data = new OVRSkeleton.SkeletonPoseData();

		data.IsDataValid = IsDataValid;
		if (IsDataValid)
		{
			data.RootPose = _bodyState.RootPose;
			data.RootScale = 1.0f;
			data.BoneRotations = _bodyState.BoneRotations;
			data.IsDataHighConfidence = IsDataHighConfidence;
			data.HipsBoneTranslation = HipsBoneTranslation;
			data.SkeletonChangedCount = SkeletonChangedCount;
		}

		return data;
	}

	OVRSkeletonRenderer.SkeletonRendererData OVRSkeletonRenderer.IOVRSkeletonRendererDataProvider.GetSkeletonRendererData()
	{
		var data = new OVRSkeletonRenderer.SkeletonRendererData();

		data.IsDataValid = IsDataValid;
		if (IsDataValid)
		{
			data.RootScale = 1.0f;
			data.IsDataHighConfidence = IsDataHighConfidence;
			data.ShouldUseSystemGestureMaterial = false;
		}

		return data;
	}
}
#endif
