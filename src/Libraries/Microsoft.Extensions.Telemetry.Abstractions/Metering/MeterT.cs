﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;

namespace Microsoft.Extensions.Telemetry.Metering;

#pragma warning disable SA1649 // File name should match first type name

/// <summary>
/// A meter class where the meter name is derived from the specified <typeparamref name="TMeterName"/> type name.
/// </summary>
/// <typeparam name="TMeterName">The type whose name is used as the meter name.</typeparam>
[Experimental(diagnosticId: "TBD", UrlFormat = "TBD")]
public class Meter<TMeterName> : Meter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Meter{TMeterName}"/> class.
    /// </summary>
    [Experimental(diagnosticId: "TBD", UrlFormat = "TBD")]
    public Meter()
        : base(typeof(TMeterName).FullName!)
    {
    }
}
