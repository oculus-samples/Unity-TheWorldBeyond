/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using System.Reflection;

namespace Meta.Conduit
{
    /// <summary>
    /// Resolves parameters for invoking callbacks.
    /// </summary>
    internal interface IParameterProvider
    {
        /// <summary>
        /// Must be called after all parameters have been obtained and mapped but before any are read.
        /// </summary>
        void Populate(Dictionary<string, object> actualParameters, Dictionary<string, string> parameterToRoleMap);

        /// <summary>
        /// Returns true if a parameter with the specified name can be provided. 
        /// </summary>
        /// <param name="parameter">The name of the parameter.</param>
        /// <returns>True if a parameter with the specified name can be provided.</returns>
        bool ContainsParameter(ParameterInfo parameter);

        /// <summary>
        /// Returns the actual value for a formal parameter.
        /// </summary>
        /// <param name="formalParameter">The parameter info.</param>
        /// <returns>The actual parameter value.</returns>
        object GetParameterValue(ParameterInfo formalParameter);
    }
}
