// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Telemetry;

/// <summary>
/// Interface for creating a builder.
/// </summary>
public class HttpMeteringBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpMeteringBuilder"/> class.
    /// </summary>
    /// <param name="services">Application services.</param>
    public HttpMeteringBuilder(IServiceCollection services)
    {
        Services = services;
    }

    /// <summary>
    /// Gets the application service collection.
    /// </summary>
    public IServiceCollection Services { get; private set; }
}
