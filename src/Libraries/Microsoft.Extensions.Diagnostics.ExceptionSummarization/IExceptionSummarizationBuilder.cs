﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Diagnostics.ExceptionSummarization;

/// <summary>
/// Abstraction to register new exception summary providers.
/// </summary>
public interface IExceptionSummarizationBuilder
{
    /// <summary>
    /// Gets the service collection into which the summary provider instances are registered.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Adds a summary provider to the builder.
    /// </summary>
    /// <typeparam name="T">The type of the provider.</typeparam>
    /// <returns>The current instance.</returns>
    IExceptionSummarizationBuilder AddProvider<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
        where T : class, IExceptionSummaryProvider;
}
