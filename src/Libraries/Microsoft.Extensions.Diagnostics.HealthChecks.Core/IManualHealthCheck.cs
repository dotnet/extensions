// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Lets you manually set the health status of the application.
/// </summary>
public interface IManualHealthCheck : IDisposable
{
    /// <summary>
    /// Gets or sets the health status.
    /// </summary>
    public HealthCheckResult Result { get; set; }
}

/// <summary>
/// Lets you manually set the application's health status.
/// </summary>
/// <typeparam name="T">The type of <see cref="IManualHealthCheck"/>.</typeparam>
public interface IManualHealthCheck<T> : IManualHealthCheck
{
}
