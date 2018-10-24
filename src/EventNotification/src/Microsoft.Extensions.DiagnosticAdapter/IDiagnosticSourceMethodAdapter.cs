// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Microsoft.Extensions.DiagnosticAdapter
{
    public interface IDiagnosticSourceMethodAdapter
    {
        Func<object, object, bool> Adapt(MethodInfo method, Type inputType);
    }
}
