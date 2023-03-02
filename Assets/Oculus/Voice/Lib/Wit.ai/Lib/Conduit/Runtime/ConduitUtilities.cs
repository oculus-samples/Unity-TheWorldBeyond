/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Text.RegularExpressions;

namespace Meta.Conduit
{
    /// <summary>
    /// Utility class for Conduit.
    /// </summary>
    internal class ConduitUtilities
    {
        private static readonly Regex UnderscoreSplitter = new Regex("(\\B[A-Z])", RegexOptions.Compiled);
        
        /// <summary>
        /// Splits a string at word boundaries and delimits it with underscores. 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string DelimitWithUnderscores(string input)
        {
            return UnderscoreSplitter.Replace(input, "_$1");
        }
    }
}
