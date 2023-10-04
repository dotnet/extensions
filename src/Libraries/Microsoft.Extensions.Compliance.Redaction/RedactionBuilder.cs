// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Compliance.Redaction;

/// <summary>
/// Configures redaction library.
/// </summary>
internal sealed class RedactionBuilder : IRedactionBuilder
{
    /// <inheritdoc/>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RedactionBuilder"/> class.
    /// </summary>
    /// <param name="services">Host services that will be used to register redactors.</param>
    public RedactionBuilder(IServiceCollection services)
    {
        Services = Throw.IfNull(services);

        Services.TryAddEnumerable(ServiceDescriptor.Singleton<Redactor>(ErasingRedactor.Instance));
        Services.TryAddEnumerable(ServiceDescriptor.Singleton<Redactor>(NullRedactor.Instance));
    }

    public IRedactionBuilder SetRedactor<T>(params DataClassification[] classifications)
        where T : Redactor
    {
        _ = Throw.IfNull(classifications);

        foreach (var c in classifications)
        {
            var redactorType = typeof(T);
            Services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(Redactor), redactorType));
            _ = Services.Configure<RedactorProviderOptions>(options => options.Redactors[c] = redactorType);
        }

        return this;
    }

    public IRedactionBuilder SetRedactor<T>(params IReadOnlyList<DataClassification>[] classifications)
        where T : Redactor
    {
        _ = Throw.IfNull(classifications);

        foreach (var c in classifications)
        {
            var redactorType = typeof(T);
            Services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(Redactor), redactorType));
            _ = Services.Configure<RedactorProviderOptions>(options => options.SetRedactors[c.ToFrozenSet()] = redactorType);
        }

        return this;
    }

    public IRedactionBuilder SetFallbackRedactor<T>()
        where T : Redactor
    {
        Services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(Redactor), typeof(T)));
        _ = Services.Configure<RedactorProviderOptions>(options => options.FallbackRedactor = typeof(T));
        return this;
    }
}
