/*
 * Copyright (c) Facebook, Inc. and its affiliates.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using Facebook.WitAi.CallbackHandlers;
using Facebook.WitAi.Configuration;
using Facebook.WitAi.Data;
using Facebook.WitAi.Data.Configuration;
using Facebook.WitAi.Lib;
using Facebook.WitAi.Utilities;
using UnityEditor;
using UnityEngine;

namespace Facebook.WitAi.Windows
{
    public class WitUnderstandingViewer : WitConfigurationWindow
    {
        [SerializeField] private Texture2D witHeader;
        [SerializeField] private string responseText;
        private string utterance;
        private WitResponseNode response;
        private Dictionary<string, bool> foldouts;

        private DateTime submitStart;
        private TimeSpan requestLength;
        private string status;
        private VoiceService wit;
        private int responseCode;
        private WitRequest request;

        public bool HasWit => null != wit;

        class Content
        {
            public static GUIContent copyPath;
            public static GUIContent copyCode;
            public static GUIContent createStringValue;
            public static GUIContent createIntValue;
            public static GUIContent createFloatValue;

            static Content()
            {
                createStringValue = new GUIContent("Create Value Reference/Create String");
                createIntValue = new GUIContent("Create Value Reference/Create Int");
                createFloatValue = new GUIContent("Create Value Reference/Create Float");

                copyPath = new GUIContent("Copy Path to Clipboard");
                copyCode = new GUIContent("Copy Code to Clipboard");
            }
        }

        protected override GUIContent Title => WitStyles.UnderstandingTitleContent;
        protected override WitStyles.WitAppEndpointType HeaderEndpointType => WitStyles.WitAppEndpointType.Understanding;

        protected override void OnEnable()
        {
            base.OnEnable();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            SetWit(GameObject.FindObjectOfType<VoiceService>());
            if (!string.IsNullOrEmpty(responseText))
            {
                response = WitResponseNode.Parse(responseText);
            }
            status = WitStyles.Texts.UnderstandingViewerPromptLabel;
        }

        protected override void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && !HasWit)
            {
                SetWit(FindObjectOfType<VoiceService>());
            }
        }

        private void OnSelectionChange()
        {
            if (Selection.activeGameObject)
            {
                wit = Selection.activeGameObject.GetComponent<VoiceService>();
                SetWit(wit);
            }
        }

        private void SetWit(VoiceService wit)
        {
            if (HasWit)
            {
                wit.events.OnRequestCreated.RemoveListener(OnRequestCreated);
                wit.events.OnError.RemoveListener(OnError);
                wit.events.OnResponse.RemoveListener(ShowResponse);
                wit.events.OnFullTranscription.RemoveListener(ShowTranscription);
                wit.events.OnPartialTranscription.RemoveListener(ShowTranscription);
            }
            if (null != wit)
            {
                this.wit = wit;
                wit.events.OnRequestCreated.AddListener(OnRequestCreated);
                wit.events.OnError.AddListener(OnError);
                wit.events.OnResponse.AddListener(ShowResponse);
                wit.events.OnFullTranscription.AddListener(ShowTranscription);
                wit.events.OnPartialTranscription.AddListener(ShowTranscription);
                // We will be measuring perceived request time since the actual request starts
                // as soon as the mic goes active and the user says something.
                wit.events.OnStoppedListening.AddListener(ResetStartTime);
                Repaint();
            }
        }

        private void ResetStartTime()
        {
            submitStart = System.DateTime.Now;
        }

        private void OnError(string title, string message)
        {
            status = message;
        }

        private void OnRequestCreated(WitRequest request)
        {
            this.request = request;
            ResetStartTime();
        }

        private void ShowTranscription(string transcription)
        {
            utterance = transcription;
            Repaint();
        }

        // On gui
        protected override void OnGUI()
        {
            base.OnGUI();
            WitEditorUI.LayoutStatusLabel(status);
        }

        protected override void LayoutContent()
        {
            // Layout wit select
            base.LayoutContent();

            // Need configuration
            if (!witConfiguration)
            {
                WitEditorUI.LayoutErrorLabel(WitStyles.Texts.UnderstandingViewerMissingConfigLabel);
                return;
            }
            // Need app id
            string appID = WitConfigurationUtility.GetAppID(witConfiguration);
            if (string.IsNullOrEmpty(appID))
            {
                WitEditorUI.LayoutErrorLabel(WitStyles.Texts.UnderstandingViewerNoAppLabel);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (WitEditorUI.LayoutTextButton(WitStyles.Texts.UnderstandingViewerSettingsButtonLabel))
                {
                    Selection.activeObject = witConfiguration;
                }
                GUILayout.EndHorizontal();
                return;
            }
            bool updated = false;
            WitEditorUI.LayoutTextField(new GUIContent(WitStyles.Texts.UnderstandingViewerUtteranceLabel), ref utterance, ref updated);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (WitEditorUI.LayoutTextButton(WitStyles.Texts.UnderstandingViewerSubmitButtonLabel) && (null == request || !request.IsActive))
            {
                responseText = "";
                if (!string.IsNullOrEmpty(utterance))
                {
                    SubmitUtterance();
                }
                else
                {
                    response = null;
                }
            }

            if (EditorApplication.isPlaying && wit)
            {
                if (!wit.Active && WitEditorUI.LayoutTextButton(WitStyles.Texts.UnderstandingViewerActivateButtonLabel))
                {
                    wit.Activate();
                }

                if (wit.Active && WitEditorUI.LayoutTextButton(WitStyles.Texts.UnderstandingViewerDeactivateButtonLabel))
                {
                    wit.Deactivate();
                }

                if (wit.Active && WitEditorUI.LayoutTextButton(WitStyles.Texts.UnderstandingViewerAbortButtonLabel))
                {
                    wit.DeactivateAndAbortRequest();
                }
            }
            GUILayout.EndHorizontal();

            // Results
            GUILayout.BeginVertical(EditorStyles.helpBox);
            if (wit && wit.MicActive)
            {
                WitEditorUI.LayoutWrapLabel(WitStyles.Texts.UnderstandingViewerListeningLabel);
            }
            else if (wit && wit.IsRequestActive)
            {
                WitEditorUI.LayoutWrapLabel(WitStyles.Texts.UnderstandingViewerLoadingLabel);
            }
            else if (response != null)
            {
                DrawResponse();
            }
            else if (string.IsNullOrEmpty(responseText))
            {
                WitEditorUI.LayoutWrapLabel(WitStyles.Texts.UnderstandingViewerPromptLabel);
            }
            else
            {
                WitEditorUI.LayoutWrapLabel(responseText);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }

        private void SubmitUtterance()
        {
            if (Application.isPlaying && !HasWit)
            {
                SetDefaultWit();
            }

            // Remove response
            response = null;

            if (wit && Application.isPlaying)
            {
                status = WitStyles.Texts.UnderstandingViewerListeningLabel;
                responseText = status;
                wit.Activate(utterance);
                // Hack to watch for loading to complete. Response does not
                // come back on the main thread so Repaint in onResponse in
                // the editor does nothing.
                EditorApplication.update += WatchForWitResponse;
            }
            else
            {
                status = WitStyles.Texts.UnderstandingViewerLoadingLabel;
                responseText = status;
                submitStart = System.DateTime.Now;
                request = witConfiguration.MessageRequest(utterance, new WitRequestOptions());
                request.onResponse = OnResponse;
                request.Request();
            }
        }

        private void WatchForWitResponse()
        {
            if (wit && !wit.Active)
            {
                Repaint();
                EditorApplication.update -= WatchForWitResponse;
            }
        }

        private void SetDefaultWit()
        {
            SetWit(FindObjectOfType<VoiceService>());
        }

        private void OnResponse(WitRequest request)
        {
            responseCode = request.StatusCode;
            if (null != request.ResponseData)
            {
                ShowResponse(request.ResponseData);
            }
            else if (!string.IsNullOrEmpty(request.StatusDescription))
            {
                responseText = request.StatusDescription;
            }
            else
            {
                responseText = "No response. Status: " + request.StatusCode;
            }
        }

        private void ShowResponse(WitResponseNode r)
        {
            response = r;
            responseText = response.ToString();
            requestLength = DateTime.Now - submitStart;
            status = $"Response time: {requestLength}";
        }

        private void DrawResponse()
        {
            DrawResponseNode(response);
        }

        private void DrawResponseNode(WitResponseNode witResponseNode, string path = "")
        {
            if (null == witResponseNode?.AsObject) return;

            if(string.IsNullOrEmpty(path)) DrawNode(witResponseNode["text"], "text", path);

            var names = witResponseNode.AsObject.ChildNodeNames;
            Array.Sort(names);
            foreach (string child in names)
            {
                if (!(string.IsNullOrEmpty(path) && child == "text"))
                {
                    var childNode = witResponseNode[child];
                    DrawNode(childNode, child, path);
                }
            }
        }

        private void DrawNode(WitResponseNode childNode, string child, string path, bool isArrayElement = false)
        {
            if (childNode == null)
            {
                return;
            }
            string childPath;

            if (path.Length > 0)
            {
                childPath = isArrayElement ? $"{path}[{child}]" : $"{path}.{child}";
            }
            else
            {
                childPath = child;
            }

            if (!string.IsNullOrEmpty(childNode.Value))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(15 * EditorGUI.indentLevel);
                if (GUILayout.Button($"{child} = {childNode.Value}", "Label"))
                {
                    ShowNodeMenu(childNode, childPath);
                }

                GUILayout.EndHorizontal();
            }
            else
            {
                var childObject = childNode.AsObject;
                var childArray = childNode.AsArray;

                if ((null != childObject || null != childArray) && Foldout(childPath, child))
                {
                    EditorGUI.indentLevel++;
                    if (null != childObject)
                    {
                        DrawResponseNode(childNode, childPath);
                    }

                    if (null != childArray)
                    {
                        DrawArray(childArray, childPath);
                    }

                    EditorGUI.indentLevel--;
                }
            }
        }

        private void ShowNodeMenu(WitResponseNode node, string path)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(Content.createStringValue, false, () => WitDataCreation.CreateStringValue(path));
            menu.AddItem(Content.createIntValue, false, () => WitDataCreation.CreateIntValue(path));
            menu.AddItem(Content.createFloatValue, false, () => WitDataCreation.CreateFloatValue(path));
            menu.AddSeparator("");
            menu.AddItem(Content.copyPath, false, () =>
            {
                EditorGUIUtility.systemCopyBuffer = path;
            });
            menu.AddItem(Content.copyCode, false, () =>
            {
                EditorGUIUtility.systemCopyBuffer = WitResultUtilities.GetCodeFromPath(path);
            });

            if (Selection.activeGameObject)
            {
                menu.AddSeparator("");

                var label =
                    new GUIContent($"Add response matcher to {Selection.activeObject.name}");

                menu.AddItem(label, false, () =>
                {
                    var valueHandler = Selection.activeGameObject.AddComponent<WitResponseMatcher>();
                    valueHandler.intent = response.GetIntentName();
                    valueHandler.valueMatchers = new ValuePathMatcher[]
                    {
                        new ValuePathMatcher() { path = path }
                    };
                });

                AddMultiValueUpdateItems(path, menu);
            }

            menu.ShowAsContext();
        }

        private void AddMultiValueUpdateItems(string path, GenericMenu menu)
        {

            string name = path;
            int index = path.LastIndexOf('.');
            if (index > 0)
            {
                name = name.Substring(index + 1);
            }

            var mvhs = Selection.activeGameObject.GetComponents<WitResponseMatcher>();
            if (mvhs.Length > 1)
            {
                for (int i = 0; i < mvhs.Length; i++)
                {
                    var handler = mvhs[i];
                    menu.AddItem(
                        new GUIContent($"Add {name} matcher to {Selection.activeGameObject.name}/Handler {(i + 1)}"),
                        false, (h) => AddNewEventHandlerPath((WitResponseMatcher) h, path), handler);
                }
            }
            else if (mvhs.Length == 1)
            {
                var handler = mvhs[0];
                menu.AddItem(
                    new GUIContent($"Add {name} matcher to {Selection.activeGameObject.name}'s Response Matcher"),
                    false, (h) => AddNewEventHandlerPath((WitResponseMatcher) h, path), handler);
            }
        }

        private void AddNewEventHandlerPath(WitResponseMatcher handler, string path)
        {
            Array.Resize(ref handler.valueMatchers, handler.valueMatchers.Length + 1);
            handler.valueMatchers[handler.valueMatchers.Length - 1] = new ValuePathMatcher()
            {
                path = path
            };
        }

        private void DrawArray(WitResponseArray childArray, string childPath)
        {
            for (int i = 0; i < childArray.Count; i++)
            {
                DrawNode(childArray[i], i.ToString(), childPath, true);
            }
        }

        private bool Foldout(string path, string label)
        {
            if (null == foldouts) foldouts = new Dictionary<string, bool>();
            if (!foldouts.TryGetValue(path, out var state))
            {
                state = false;
                foldouts[path] = state;
            }

            var newState = EditorGUILayout.Foldout(state, label);
            if (newState != state)
            {
                foldouts[path] = newState;
            }

            return newState;
        }
    }
}
