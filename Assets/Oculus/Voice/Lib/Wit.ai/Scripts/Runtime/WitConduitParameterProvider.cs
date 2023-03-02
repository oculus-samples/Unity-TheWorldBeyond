/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Reflection;
using Facebook.WitAi.Data;
using Facebook.WitAi.Lib;
using Meta.Conduit;

namespace Facebook.WitAi
{
    internal class WitConduitParameterProvider : ParameterProvider
    {
        public const string WitResponseNodeReservedName = "@WitResponseNode";
        public const string VoiceSessionReservedName = "@VoiceSession";
        protected override object GetSpecializedParameter(ParameterInfo formalParameter)
        {
            if (formalParameter.ParameterType == typeof(WitResponseNode) && ActualParameters.ContainsKey(WitResponseNodeReservedName))
            {
                return ActualParameters[WitResponseNodeReservedName];
            }
            else if (formalParameter.ParameterType == typeof(VoiceSession) && ActualParameters.ContainsKey(VoiceSessionReservedName))
            {
                return ActualParameters[VoiceSessionReservedName];
            }
            return null;
        }

        protected override bool SupportedSpecializedParameter(ParameterInfo formalParameter)
        {
            return formalParameter.ParameterType == typeof(WitResponseNode) || formalParameter.ParameterType == typeof(VoiceSession);
        }
    }
}
