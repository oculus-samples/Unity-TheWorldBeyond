/*
 * Copyright (c) Facebook, Inc. and its affiliates.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using UnityEngine;
using Facebook.WitAi.Data.Configuration;

namespace Facebook.WitAi.Windows
{
    public abstract class WitConfigurationWindow : BaseWitWindow
    {
        protected int witConfigIndex = -1;
        protected WitConfiguration witConfiguration;

        protected override string HeaderUrl
        {
            get
            {
                string appID = WitConfigurationUtility.GetAppID(witConfiguration);
                if (!string.IsNullOrEmpty(appID))
                {
                    return WitStyles.GetAppURL(appID, HeaderEndpointType);
                }
                return base.HeaderUrl;
            }
        }
        protected virtual WitStyles.WitAppEndpointType HeaderEndpointType => WitStyles.WitAppEndpointType.Settings;
        protected virtual void SetConfiguration(int newConfigIndex)
        {
            witConfigIndex = newConfigIndex;
            WitConfiguration[] witConfigs = WitConfigurationUtility.WitConfigs;
            witConfiguration = witConfigs != null && witConfigIndex >= 0 && witConfigIndex < witConfigs.Length ? witConfigs[witConfigIndex] : null;
        }
        public virtual void SetConfiguration(WitConfiguration newConfiguration)
        {
            int newConfigIndex = newConfiguration == null ? -1 : Array.IndexOf(WitConfigurationUtility.WitConfigs, newConfiguration);
            if (newConfigIndex != -1)
            {
                SetConfiguration(newConfigIndex);
            }
        }
        protected override void OnEnable()
        {
            base.OnEnable();
            WitAuthUtility.InitEditorTokens();
        }
        protected override void LayoutContent()
        {
            // Layout popup
            int index = witConfigIndex;
            WitConfigurationEditorUI.LayoutConfigurationSelect(ref index);
            GUILayout.Space(WitStyles.ButtonMargin);
            // Selection changed
            if (index != witConfigIndex)
            {
                SetConfiguration(index);
            }
        }
    }
}
