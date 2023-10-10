// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection;

internal sealed class AutoActivatorOptions
{
    public HashSet<Type> AutoActivators { get; } = [];
    public HashSet<(Type serviceType, object? serviceKey)> KeyedAutoActivators { get; } = [];
}
