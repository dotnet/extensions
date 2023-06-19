// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;

namespace Microsoft.Extensions.Telemetry.Testing.Metering;

/// <summary>
/// The helper class to automatically capture metering information that has been recorded
/// by instruments created by <see cref="Meter"/>.
/// </summary>
/// <remarks>
/// This type has been designed to be used only for testing purposes.
/// </remarks>
/// <typeparam name="TMeterName">The type whose name is used as the <see cref="Meter"/> instance name.</typeparam>
[Experimental(diagnosticId: "NETEXT0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
#pragma warning disable SA1649 // File name should match first type name
public sealed class MetricCollector<TMeterName> : MetricCollector
#pragma warning restore SA1649 // File name should match first type name
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MetricCollector{TMeterName}"/> class.
    /// </summary>
    public MetricCollector()
        : base(new[] { typeof(TMeterName).FullName! })
    {
    }
}
