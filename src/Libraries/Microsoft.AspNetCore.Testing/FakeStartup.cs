// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Testing;

[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "convention")]
internal sealed class FakeStartup
{
    public void Configure(IApplicationBuilder _)
    {
        // intentionally empty
    }

    public void ConfigureServices(IServiceCollection _)
    {
        // intentionally empty
    }
}
