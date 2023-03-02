/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;

namespace Meta.Conduit
{
    /// <summary>
    /// The dispatcher is responsible for deciding which method to invoke when a request is received as well as parsing
    /// the parameters and passing them to the handling method.
    /// </summary>
    internal interface IConduitDispatcher
    {
        /// <summary>
        /// Parses the manifest provided and registers its callbacks for dispatching.
        /// </summary>
        /// <param name="manifestFilePath">The path to the manifest file.</param>
        void Initialize(string manifestFilePath);

        /// <summary>
        /// Invokes the method matching the specified action ID.
        /// This should NOT be called before the dispatcher is initialized.
        /// </summary>
        /// <param name="actionId">The action ID (which is also the intent name).</param>
        /// <param name="parameters">Dictionary of parameters mapping parameter name to value.</param>
        /// <param name="confidence">The confidence level (between 0-1) of the intent that's invoking the action.</param>
        bool InvokeAction(string actionId, Dictionary<string, object> parameters, float confidence = 1f, bool partial = false);
    }
}
