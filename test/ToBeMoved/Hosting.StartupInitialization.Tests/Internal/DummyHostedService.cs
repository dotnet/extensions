// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.Hosting.Testing.StartupInitialization.Test;

#pragma warning disable SA1402 // File may only contain a single type

[SuppressMessage("Minor Code Smell", "S3717:Track use of \"NotImplementedException\"", Justification = "Not applicable.")]
public class DummyHostedService : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task StopAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
}

[SuppressMessage("Minor Code Smell", "S3717:Track use of \"NotImplementedException\"", Justification = "Not applicable.")]
public class DummyHostedService2 : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task StopAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
}

[SuppressMessage("Minor Code Smell", "S3717:Track use of \"NotImplementedException\"", Justification = "Not applicable.")]
public class DummyHostedService3 : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task StopAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
}
