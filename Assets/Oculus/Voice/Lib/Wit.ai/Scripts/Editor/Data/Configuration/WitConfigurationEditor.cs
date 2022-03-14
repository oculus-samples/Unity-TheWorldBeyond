/*
 * Copyright (c) Facebook, Inc. and its affiliates.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Facebook.WitAi.Configuration;
using Facebook.WitAi.Data.Configuration;
using UnityEditor;
using UnityEngine;

namespace Facebook.WitAi.Windows
{
    public class WitConfigurationEditor : Editor
    {
        public WitConfiguration configuration { get; private set; }
        private string serverToken;
        private bool initialized = false;
        public bool drawHeader = true;
        private bool foldout = true;
        private int requestTab = -1;
        private string[] tabNames;

        protected virtual Texture2D HeaderIcon => WitStyles.HeaderIcon;
        protected virtual string HeaderUrl => WitStyles.GetAppURL(WitConfigurationUtility.GetAppID(configuration), WitStyles.WitAppEndpointType.Settings);

        private const int TAB_APPLICATION = 0;
        private const int TAB_INTENTS = 1;
        private const int TAB_ENTITIES = 2;
        private const int TAB_TRAITS = 3;
        public void Initialize()
        {
            // Refresh configuration & auth tokens
            configuration = target as WitConfiguration;
            // Get app server token
            serverToken = WitAuthUtility.GetAppServerToken(configuration);
            if (WitConfigurationUtility.IsServerTokenValid(serverToken))
            {
                // Get client token if needed
                string appID = WitConfigurationUtility.GetAppID(configuration);
                if (string.IsNullOrEmpty(appID))
                {
                    configuration.SetServerToken(serverToken);
                }
                // Refresh additional data
                else
                {
                    SafeRefresh();
                }
            }
        }

        public override void OnInspectorGUI()
        {
            // Init styles
            WitStyles.Init();
            // Init if needed
            if (!initialized || configuration != target)
            {
                Initialize();
                initialized = true;
            }

            // Draw header
            if (drawHeader)
            {
                WitEditorUI.LayoutHeaderButton(HeaderIcon, HeaderUrl);
                GUILayout.Space(WitStyles.HeaderPaddingBottom);
                EditorGUI.indentLevel++;
            }

            // Layout content
            LayoutContent();

            // Undent
            if (drawHeader)
            {
                EditorGUI.indentLevel--;
            }
        }

        protected virtual void LayoutContent()
        {
            // Begin vertical box
            GUILayout.BeginVertical(EditorStyles.helpBox);

            // Title Foldout
            GUILayout.BeginHorizontal();
            string foldoutText = WitStyles.Texts.ConfigurationHeaderLabel;
            string appName = configuration?.application?.name;
            if (!string.IsNullOrEmpty(appName))
            {
                foldoutText = foldoutText + " - " + appName;
            }
            foldout = WitEditorUI.LayoutFoldout(new GUIContent(foldoutText), foldout);
            // Refresh button
            if (configuration)
            {
                if (string.IsNullOrEmpty(appName))
                {
                    bool isValid =  WitConfigurationUtility.IsServerTokenValid(serverToken);
                    GUI.enabled = isValid;
                    if (WitEditorUI.LayoutTextButton(WitStyles.Texts.ConfigurationRefreshButtonLabel))
                    {
                        configuration.SetServerToken(serverToken);
                    }
                }
                else
                {
                    bool isRefreshing = configuration.IsRefreshingData();
                    GUI.enabled = !isRefreshing;
                    if (WitEditorUI.LayoutTextButton(isRefreshing ? WitStyles.Texts.ConfigurationRefreshingButtonLabel : WitStyles.Texts.ConfigurationRefreshButtonLabel))
                    {
                        SafeRefresh();
                    }
                }
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.Space(WitStyles.ButtonMargin);

            // Show configuration app data
            if (foldout)
            {
                // Indent
                EditorGUI.indentLevel++;

                // Server access token
                bool updated = false;
                WitEditorUI.LayoutPasswordField(WitStyles.ConfigurationServerTokenContent, ref serverToken, ref updated);
                if (updated)
                {
                    configuration.SetServerToken(serverToken);
                }

                // Additional data
                if (configuration)
                {
                    LayoutConfigurationData();
                }

                // Undent
                EditorGUI.indentLevel--;
            }

            // End vertical box layout
            GUILayout.EndVertical();

            // Layout configuration request tabs
            LayoutConfigurationRequestTabs();

            // Additional open wit button
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(WitStyles.Texts.WitOpenButtonLabel, WitStyles.TextButton))
            {
                Application.OpenURL(HeaderUrl);
            }
        }
        // Layout configuration data
        protected virtual void LayoutConfigurationData()
        {
            // Reset update
            bool updated = false;
            // Client access field
            WitEditorUI.LayoutPasswordField(WitStyles.ConfigurationClientTokenContent, ref configuration.clientAccessToken, ref updated);
            // Timeout field
            WitEditorUI.LayoutIntField(WitStyles.ConfigurationRequestTimeoutContent, ref configuration.timeoutMS, ref updated);
            // Updated
            if (updated)
            {
                EditorUtility.SetDirty(configuration);
            }

            // Show configuration app data
            LayoutConfigurationEndpoint();
        }
        // Layout endpoint data
        protected virtual void LayoutConfigurationEndpoint()
        {
            // Generate if needed
            if (configuration.endpointConfiguration == null)
            {
                configuration.endpointConfiguration = new WitEndpointConfig();
                EditorUtility.SetDirty(configuration);
            }

            // Handle via serialized object
            var serializedObj = new SerializedObject(configuration);
            var serializedProp = serializedObj.FindProperty("endpointConfiguration");
            EditorGUILayout.PropertyField(serializedProp);
            serializedObj.ApplyModifiedProperties();
        }
        // Tabs
        protected virtual void LayoutConfigurationRequestTabs()
        {
            // Indent
            EditorGUI.indentLevel++;

            // Generate tab names
            if (tabNames == null)
            {
                tabNames = new string[4];
                tabNames[TAB_APPLICATION] = WitStyles.Texts.ConfigurationApplicationTabLabel;
                tabNames[TAB_INTENTS] = WitStyles.Texts.ConfigurationIntentsTabLabel;
                tabNames[TAB_ENTITIES] = WitStyles.Texts.ConfigurationEntitiesTabLabel;
                tabNames[TAB_TRAITS] = WitStyles.Texts.ConfigurationTraitsTabLabel;
            }
            // Application tabs
            WitEditorUI.LayoutTabButtons(tabNames, ref requestTab);

            // Use response tab key as property id
            string propertyID = "";
            string missingText = "";
            switch (requestTab)
            {
                case TAB_APPLICATION:
                    propertyID = "application";
                    missingText = WitStyles.Texts.ConfigurationApplicationMissingLabel;
                    break;
                case TAB_INTENTS:
                    propertyID = "intents";
                    missingText = WitStyles.Texts.ConfigurationIntentsMissingLabel;
                    break;
                case TAB_ENTITIES:
                    propertyID = "entities";
                    missingText = WitStyles.Texts.ConfigurationEntitiesMissingLabel;
                    break;
                case TAB_TRAITS:
                    propertyID = "traits";
                    missingText = WitStyles.Texts.ConfigurationTraitsMissingLabel;
                    break;
            }

            // Layout selected tab using property id
            if (!string.IsNullOrEmpty(propertyID))
            {
                SerializedObject serializedObj = new SerializedObject(configuration);
                SerializedProperty serializedProp = serializedObj.FindProperty(propertyID);
                if (serializedProp == null)
                {
                    WitEditorUI.LayoutErrorLabel(missingText);
                }
                else if (!serializedProp.isArray)
                {
                    EditorGUILayout.PropertyField(serializedProp);
                }
                else if (serializedProp.arraySize == 0)
                {
                    WitEditorUI.LayoutErrorLabel(missingText);
                }
                else
                {
                    for (int i = 0; i < serializedProp.arraySize; i++)
                    {
                        SerializedProperty serializedPropChild = serializedProp.GetArrayElementAtIndex(i);
                        EditorGUILayout.PropertyField(serializedPropChild);
                    }
                }
                serializedObj.ApplyModifiedProperties();
            }

            // Undent
            EditorGUI.indentLevel--;
        }

        // Safe refresh
        protected virtual void SafeRefresh()
        {
            if (!WitConfigurationUtility.IsClientTokenValid(configuration.clientAccessToken))
            {
                if (!WitConfigurationUtility.IsServerTokenValid(serverToken))
                {
                    configuration.SetServerToken(serverToken);
                }
            }
            else
            {
                configuration.RefreshData();
            }
        }
    }
}
