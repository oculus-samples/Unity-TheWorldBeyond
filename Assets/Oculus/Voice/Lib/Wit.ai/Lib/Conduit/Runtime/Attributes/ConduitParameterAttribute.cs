/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace Meta.Conduit
{
    /// <summary>
    /// Marks a parameter as a Conduit parameter to be supplied when the callback method is called.
    /// This is not required, but allows the addition of more information about parameters to improve the quality of
    /// intent recognition and entity resolution. 
    /// </summary>
    [AttributeUsage(System.AttributeTargets.Parameter)]
    public class ConduitParameterAttribute : Attribute
    {
        public ConduitParameterAttribute(params string[] aliases)
        {
            this.Aliases = aliases.ToList();
        }

        /// <summary>
        /// The names that refer to this parameter.
        /// </summary>
        public List<string> Aliases { get; }
    }
}
