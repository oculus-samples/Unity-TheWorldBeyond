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
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class OVRBodyTest : MonoBehaviour
{
	public class BoolMonitor
	{
		public delegate bool BoolGenerator();

		private string m_name = "";
		private BoolGenerator m_generator;
		private bool m_prevValue = false;
		private bool m_currentValue = false;
		private bool m_currentValueRecentlyChanged = false;
		private float m_displayTimeout = 0.0f;
		private float m_displayTimer = 0.0f;

		public BoolMonitor(string name, BoolGenerator generator, float displayTimeout = 0.5f)
		{
			m_name = name;
			m_generator = generator;
			m_displayTimeout = displayTimeout;
		}

		public void Update()
		{
			m_prevValue = m_currentValue;
			m_currentValue = m_generator();

			if (m_currentValue != m_prevValue)
			{
				m_currentValueRecentlyChanged = true;
				m_displayTimer = m_displayTimeout;
			}

			if (m_displayTimer > 0.0f)
			{
				m_displayTimer -= Time.deltaTime;

				if (m_displayTimer <= 0.0f)
				{
					m_currentValueRecentlyChanged = false;
					m_displayTimer = 0.0f;
				}
			}
		}

		public void AppendToStringBuilder(ref StringBuilder sb)
		{
			sb.Append(m_name);

			if (m_currentValue && m_currentValueRecentlyChanged)
				sb.Append(": *True*\n");
			else if (m_currentValue)
				sb.Append(":  True \n");
			else if (!m_currentValue && m_currentValueRecentlyChanged)
				sb.Append(": *False*\n");
			else if (!m_currentValue)
				sb.Append(":  False \n");
		}
	}

	public Text uiText;
	private List<BoolMonitor> monitors;
	private StringBuilder data;

	private OVRPlugin.BodyState bodyState = new OVRPlugin.BodyState();
	private OVRPlugin.Skeleton2 skeleton = new OVRPlugin.Skeleton2();

	private bool result_skeleton = false;

	void Start()
	{
		if (uiText != null)
		{
			uiText.supportRichText = false;
		}

		data = new StringBuilder(2048);

		monitors = new List<BoolMonitor>()
		{
			new BoolMonitor("One",                              () => OVRInput.Get(OVRInput.Button.One)),
			new BoolMonitor("Two",                              () => OVRInput.Get(OVRInput.Button.Two)),
			new BoolMonitor("Three",                              () => OVRInput.Get(OVRInput.Button.Three)),
			new BoolMonitor("Four",                              () => OVRInput.Get(OVRInput.Button.Four)),
		};

		result_skeleton = OVRPlugin.GetSkeleton2(OVRPlugin.SkeletonType.Body, ref skeleton);
	}

	static string prevConnected = "";
	static BoolMonitor controllers = new BoolMonitor("Controllers Changed", () => { return OVRInput.GetConnectedControllers().ToString() != prevConnected; });

	void Update()
	{
		data.Length = 0;

		OVRInput.Controller activeController = OVRInput.GetActiveController();

		string activeControllerName = activeController.ToString();
		data.AppendFormat("Active: {0}\n", activeControllerName);

		string connectedControllerNames = OVRInput.GetConnectedControllers().ToString();
		data.AppendFormat("Connected: {0}\n", connectedControllerNames);

		data.AppendFormat("PrevConnected: {0}\n", prevConnected);

		controllers.Update();
		controllers.AppendToStringBuilder(ref data);
		prevConnected = connectedControllerNames;

		data.AppendFormat("HandTrackingEnabled: {0}\n", OVRPlugin.GetHandTrackingEnabled());

		data.AppendFormat("BodyReady: {0}\n", OVRPlugin.IsBodyReady());
		data.AppendFormat("BodyConfig: {0}\n", OVRPlugin.GetBodyConfig().Flags);
		bool result_bodyState = OVRPlugin.GetBodyState(OVRPlugin.Step.Render, ref bodyState);
		data.AppendFormat("BodyState Query Res: {0}\n", result_bodyState);
		data.AppendFormat("BodyState SkeletonChangedCount: {0}\n", bodyState.SkeletonChangedCount);
		data.AppendFormat("BodyState Modality: {0}\n", bodyState.BodyModality);
		data.AppendFormat("BodyState RootPose: {0}\n", bodyState.RootPose);
		data.AppendFormat("BodyState HipsTrans: {0}\n", bodyState.HipsBoneTranslation);

		data.AppendFormat("Skeleton Query Res: {0}\n", result_skeleton);
		data.AppendFormat("Skeleton Type: {0}\n", skeleton.Type);
		data.AppendFormat("Skeleton NumBones: {0}\n", skeleton.NumBones);
		data.AppendFormat("Skeleton NumBoneCapsules: {0}\n", skeleton.NumBoneCapsules);

		for (int i = 0; i < monitors.Count; i++)
		{
			monitors[i].Update();
			monitors[i].AppendToStringBuilder(ref data);
		}

		if (uiText != null)
		{
			uiText.text = data.ToString();
		}
	}
}
#endif
