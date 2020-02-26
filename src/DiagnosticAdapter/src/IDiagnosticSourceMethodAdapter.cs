// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;

namespace Microsoft.Extensions.DiagnosticAdapter
{
    public interface IDiagnosticSourceMethodAdapter
    {
        Func<object, object, bool> Adapt(MethodInfo method, Type inputType);
    }
}
