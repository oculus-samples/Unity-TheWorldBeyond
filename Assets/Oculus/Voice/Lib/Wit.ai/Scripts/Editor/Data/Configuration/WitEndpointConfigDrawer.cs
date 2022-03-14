/*
 * Copyright (c) Facebook, Inc. and its affiliates.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEditor;
using UnityEngine;
using Facebook.WitAi.Configuration;
using System.Reflection;

namespace Facebook.WitAi.Windows
{
    public class WitEndpointConfigDrawer : WitPropertyDrawer
    {
        // Allow edit with lock
        protected override WitPropertyEditType EditType => WitPropertyEditType.LockEdit;
        // Determine if should layout field
        protected override bool ShouldLayoutField(SerializedProperty property, FieldInfo subfield)
        {
            switch (subfield.Name)
            {
                case "message":
                    return false;
            }
            return base.ShouldLayoutField(property, subfield);
        }
        // Get default fields
        protected override string GetDefaultFieldValue(SerializedProperty property, FieldInfo subfield)
        {
            // Iterate options
            switch (subfield.Name)
            {
                case "uriScheme":
                    return WitRequest.URI_SCHEME;
                case "authority":
                    return WitRequest.URI_AUTHORITY;
                case "port":
                    return WitRequest.URI_DEFAULT_PORT.ToString();
                case "witApiVersion":
                    return WitRequest.WIT_API_VERSION;
                case "speech":
                    return WitRequest.WIT_ENDPOINT_SPEECH;
            }

            // Return base
            return base.GetDefaultFieldValue(property, subfield);
        }
        // Use name value for title if possible
        protected override string GetLocalizedText(SerializedProperty property, string key)
        {
            // Iterate options
            switch (key)
            {
                case LocalizedTitleKey:
                    return WitStyles.Texts.ConfigurationEndpointTitleLabel;
                case "uriScheme":
                    return WitStyles.Texts.ConfigurationEndpointUriLabel;
                case "authority":
                    return WitStyles.Texts.ConfigurationEndpointAuthLabel;
                case "port":
                    return WitStyles.Texts.ConfigurationEndpointPortLabel;
                case "witApiVersion":
                    return WitStyles.Texts.ConfigurationEndpointApiLabel;
                case "speech":
                    return WitStyles.Texts.ConfigurationEndpointSpeechLabel;
            }
            // Default to base
            return base.GetLocalizedText(property, key);
        }
    }
}
