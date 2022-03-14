/*
 * Copyright (c) Facebook, Inc. and its affiliates.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Facebook.WitAi.Data.Configuration;

namespace Facebook.WitAi.Windows
{
    public static class WitWindowUtility
    {
        // Default types
        private readonly static Type defaultSetupWindowType = typeof(WitWelcomeWizard);
        private readonly static Type defaultConfigurationWindowType = typeof(WitWindow);
        private readonly static Type defaultUnderstandingWindowType = typeof(WitUnderstandingViewer);

        // Window types
        public static Type setupWindowType = defaultSetupWindowType;
        public static Type configurationWindowType = defaultConfigurationWindowType;
        public static Type understandingWindowType = defaultUnderstandingWindowType;

        // Opens Setup Window
        public static void OpenSetupWindow(Action<WitConfiguration> onSetupComplete)
        {
            // Init
            WitStyles.Init();
            // Get setup type
            Type type = GetSafeType(setupWindowType, defaultSetupWindowType);
            // Get wizard (Title is overwritten)
            WitWelcomeWizard wizard = (WitWelcomeWizard)ScriptableWizard.DisplayWizard(WitStyles.Texts.SetupTitleLabel, type, WitStyles.Texts.SetupSubmitButtonLabel);
            // Set success callback
            wizard.successAction = onSetupComplete;
        }
        // Opens Configuration Window
        public static void OpenConfigurationWindow(WitConfiguration configuration = null)
        {
            // Init
            WitStyles.Init();
            // Setup if needed
            if (configuration == null && !WitConfigurationUtility.HasValidCustomConfig())
            {
                OpenSetupWindow(OpenConfigurationWindow);
                return;
            }

            // Get confuration type
            Type type = GetSafeType(configurationWindowType, defaultConfigurationWindowType);
            // Get window & show
            WitConfigurationWindow window = (WitConfigurationWindow)EditorWindow.GetWindow(type);
            window.autoRepaintOnSceneChange = true;
            window.SetConfiguration(configuration);
            window.Show();
        }
        // Opens Understanding Window to specific configuration
        public static void OpenUnderstandingWindow(WitConfiguration configuration = null)
        {
            // Init
            WitStyles.Init();
            // Setup if needed
            if (configuration == null && !WitConfigurationUtility.HasValidCustomConfig())
            {
                OpenSetupWindow(OpenUnderstandingWindow);
                return;
            }

            // Get understanding type
            Type type = GetSafeType(understandingWindowType, defaultUnderstandingWindowType);
            // Get window & show
            WitConfigurationWindow window = (WitConfigurationWindow)EditorWindow.GetWindow(type);
            window.autoRepaintOnSceneChange = true;
            window.SetConfiguration(configuration);
            window.Show();
        }
        // Get safe type
        private static Type GetSafeType(Type desiredType, Type defaultType)
        {
            if (desiredType == null || (desiredType != defaultType && !desiredType.IsSubclassOf(defaultType)))
            {
                Debug.LogError("Wit Editor Utility - Invalid Window Type: " + (desiredType == null ? "NULL" : desiredType.ToString()) + "\nUsing: " + defaultType.ToString());
                return defaultType;
            }
            return desiredType;
        }
    }
}
