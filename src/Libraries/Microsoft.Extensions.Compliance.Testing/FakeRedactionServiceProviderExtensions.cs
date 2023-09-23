// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Shared.Diagnostics;

namespace System;

/// <summary>
/// Extensions that allow registering a fake redactor in the application.
/// </summary>
public static class FakeRedactionServiceProviderExtensions
{
    /// <summary>
    /// Gets the fake redactor collector instance from the dependency injection container.
    /// </summary>
    /// <param name="serviceProvider">The container used to obtain the collector instance.</param>
    /// <returns>The obtained collector.</returns>
    /// <exception cref="InvalidOperationException">The collector is not in the container.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="serviceProvider"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <see cref="FakeRedactionCollector"/> should be registered and used only with fake redaction implementation.
    /// </remarks>
    public static FakeRedactionCollector GetFakeRedactionCollector(this IServiceProvider serviceProvider)
        => Throw.IfNull(serviceProvider).GetRequiredService<FakeRedactionCollector>();
}
