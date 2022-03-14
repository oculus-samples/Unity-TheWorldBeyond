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
using Facebook.WitAi.Configuration;
using Facebook.WitAi.Data.Entities;
using Facebook.WitAi.Data.Intents;
using Facebook.WitAi.Data.Traits;
using Facebook.WitAi.Utilities;
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
            if (witConfigs == null)
            {
                WitConfigurationUtility.ReloadConfigurationData();
                witConfigs = WitConfigurationUtility.WitConfigs;
            }

            // Error if none found
            if (witConfigs.Length == 0)
            {
                WitEditorUI.LayoutErrorLabel(WitStyles.Texts.ConfigurationSelectMissingLabel);
                return;
            }

            // Clamp Config Index
            configIndex = Mathf.Clamp(configIndex, 0, witConfigs.Length);

            // Layout popup
            bool configUpdated = false;
            WitEditorUI.LayoutPopup(WitStyles.Texts.ConfigurationSelectLabel, WitConfigurationUtility.WitConfigNames, ref configIndex, ref configUpdated);
        }
    }
}
