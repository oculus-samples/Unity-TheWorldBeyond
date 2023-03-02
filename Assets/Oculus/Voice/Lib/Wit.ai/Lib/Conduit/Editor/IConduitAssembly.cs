/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Meta.Conduit.Editor
{
    /// <summary>
    /// Wrapper for assemblies to provide convenience methods and abstract from CLR.
    /// </summary>
    internal interface IConduitAssembly
    {
        string FullName { get; }

        IEnumerable<Type> GetEnumTypes();

        IEnumerable<MethodInfo> GetMethods();
    }
}
