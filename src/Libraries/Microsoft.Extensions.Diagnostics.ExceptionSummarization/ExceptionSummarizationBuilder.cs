// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.Diagnostics.ExceptionSummarization;

internal sealed class ExceptionSummarizationBuilder : IExceptionSummarizationBuilder
{
    public ExceptionSummarizationBuilder(IServiceCollection services)
    {
        Services = services;
    }

    public IServiceCollection Services { get; }

    public IExceptionSummarizationBuilder AddProvider<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
        where T : class, IExceptionSummaryProvider
    {
        Services.TryAddEnumerable(ServiceDescriptor.Singleton<IExceptionSummaryProvider, T>());
        return this;
    }
}
