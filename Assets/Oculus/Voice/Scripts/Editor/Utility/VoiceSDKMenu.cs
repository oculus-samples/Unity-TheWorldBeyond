/**************************************************************************************************
 * Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.
 *
 * Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
 * under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 **************************************************************************************************/

using UnityEngine;
using UnityEditor;
using Facebook.WitAi.Windows;
using Facebook.WitAi.Configuration;
using Facebook.WitAi.Data.Configuration;
using Facebook.WitAi.Data.Intents;
using Facebook.WitAi.Data.Entities;
using Facebook.WitAi.Data.Traits;
using Oculus.Voice.Windows;

namespace Oculus.Voice.Utility
{
    public static class VoiceSDKMenu
    {
        #region WINDOWS
        private static void Init()
        {
            WitWindowUtility.setupWindowType = typeof(WelcomeWizard);
            WitWindowUtility.configurationWindowType = typeof(SettingsWindow);
            WitWindowUtility.understandingWindowType = typeof(UnderstandingViewerWindow);
        }
        [MenuItem("Oculus/Voice SDK/Settings", false, 100)]
        private static void OpenConfigurationWindow()
        {
            Init();
            WitWindowUtility.OpenConfigurationWindow();
        }
        [MenuItem("Oculus/Voice SDK/Understanding Viewer", false, 100)]
        private static void OpenUnderstandingWindow()
        {
            Init();
            WitWindowUtility.OpenUnderstandingWindow();
        }
        [MenuItem("Oculus/Voice SDK/About", false, 200)]
        private static void OpenAboutWindow()
        {
            Init();
            ScriptableWizard.DisplayWizard<AboutWindow>(VoiceSDKStyles.Texts.AboutTitleLabel, VoiceSDKStyles.Texts.AboutCloseLabel);
        }
        #endregion

        #region DRAWERS
        [CustomPropertyDrawer(typeof(WitEndpointConfig))]
        public class VoiceCustomEndpointPropertyDrawer : WitEndpointConfigDrawer
        {
            
        }
        [CustomPropertyDrawer(typeof(WitApplication))]
        public class VoiceCustomApplicationPropertyDrawer : VoiceApplicationDetailProvider
        {
            
        }
        [CustomPropertyDrawer(typeof(WitIntent))]
        public class VoiceCustomIntentPropertyDrawer : WitIntentPropertyDrawer
        {
            
        }
        [CustomPropertyDrawer(typeof(WitEntity))]
        public class VoiceCustomEntityPropertyDrawer : WitEntityPropertyDrawer
        {
            
        }
        [CustomPropertyDrawer(typeof(WitTrait))]
        public class VoiceCustomTraitPropertyDrawer : WitTraitPropertyDrawer
        {
            
        }
        #endregion
    }
}
