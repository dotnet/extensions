// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection;

internal sealed class NamedServiceProviderOptions<TService>
    where TService : class
{
    public List<NamedServiceDescriptor<TService>> Services { get; set; } = new();
}
