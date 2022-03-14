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

public class OVRInhibitHomeTest : MonoBehaviour
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

	void Start()
	{
		if (uiText != null)
		{
			uiText.supportRichText = false;
		}

		data = new StringBuilder(2048);

		monitors = new List<BoolMonitor>()
		{
			new BoolMonitor("InhibitHome Property",                   () => OVRPlugin.GetNativeSDKPropertyBool(1000164512)),
		};
	}

    bool shouldInhibitHome = false;

	void Update()
	{
		data.Length = 0;

		OVRInput.Controller activeController = OVRInput.GetActiveController();

		string activeControllerName = activeController.ToString();
		data.AppendFormat("Active: {0}\n", activeControllerName);

		string connectedControllerNames = OVRInput.GetConnectedControllers().ToString();
		data.AppendFormat("Connected: {0}\n", connectedControllerNames);

		if (OVRInput.GetDown(OVRInput.Button.One))
		{
			shouldInhibitHome = !shouldInhibitHome;
			OVRPlugin.SetNativeSDKPropertyBool(1000164512, shouldInhibitHome);
		}

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
