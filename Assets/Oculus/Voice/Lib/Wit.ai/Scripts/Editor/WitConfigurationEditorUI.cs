/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEditor;
using UnityEngine;
using Facebook.WitAi.Data.Configuration;

namespace Facebook.WitAi
{
    public static class WitConfigurationEditorUI
    {
        // Configuration select
        public static void LayoutConfigurationSelect(ref int configIndex)
        {
            // Refresh configurations if needed
            WitConfiguration[] witConfigs = WitConfigurationUtility.WitConfigs;

            if (witConfigs == null || witConfigs.Length == 0)
            {
                // If no configuration exists, provide a means for the user to create a new one.
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                    
                if (WitEditorUI.LayoutTextButton("New Config"))
                {
                    WitConfigurationUtility.CreateConfiguration("");

                    EditorUtility.FocusProjectWindow();
                }
                    
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                
                return;
            }

            // Clamp Config Index
            bool configUpdated = false;
            if (configIndex < 0 || configIndex >= witConfigs.Length)
            {
                configUpdated = true;
                configIndex = Mathf.Clamp(configIndex, 0, witConfigs.Length);
            }

            GUILayout.BeginHorizontal();
            
            // Layout popup
            WitEditorUI.LayoutPopup(WitTexts.Texts.ConfigurationSelectLabel, WitConfigurationUtility.WitConfigNames, ref configIndex, ref configUpdated);

            if (GUILayout.Button("", GUI.skin.GetStyle("IN ObjectField"), GUILayout.Width(15)))
            {
                EditorUtility.FocusProjectWindow();
                EditorGUIUtility.PingObject(witConfigs[configIndex]);
            }
            
            GUILayout.EndHorizontal();
        }
    }
}
