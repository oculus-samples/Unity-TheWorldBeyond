/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace Meta.Conduit
{
    /// <summary>
    /// This can optionally be used on enum values to provide additional information.
    /// </summary>
    [AttributeUsage(System.AttributeTargets.Field)]
    public class ConduitValueAttribute : Attribute
    {
        public ConduitValueAttribute(params string[] aliases)
        {
            this.Aliases = aliases;
        }

        /// <summary>
        /// Different ways to refer to the same value.
        /// </summary>
        public string[] Aliases { get; }
    }
}
