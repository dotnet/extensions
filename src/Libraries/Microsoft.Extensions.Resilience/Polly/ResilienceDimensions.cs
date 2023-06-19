// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Resilience;

/// <summary>
/// Constants used for enrichment dimensions.
/// </summary>
/// <remarks>
/// Constants are standardized in <see href="https://aka.ms/commonschema">MS Common Schema</see>.
/// </remarks>
// Avoid changing const values in this class by all means. Such a breaking change would break customer's monitoring.
[Experimental(diagnosticId: "NETEXT0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ResilienceDimensions
{
    /// <summary>
    /// Pipeline name.
    /// </summary>
    [Experimental(diagnosticId: "NETEXT0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public const string PipelineName = "pipeline_name";

    /// <summary>
    /// Pipeline key.
    /// </summary>
    [Experimental(diagnosticId: "NETEXT0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public const string PipelineKey = "pipeline_key";

    /// <summary>
    /// Result type.
    /// </summary>
    [Experimental(diagnosticId: "NETEXT0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public const string ResultType = "result_type";

    /// <summary>
    /// Policy name.
    /// </summary>
    [Experimental(diagnosticId: "NETEXT0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public const string PolicyName = "policy_name";

    /// <summary>
    /// Event name.
    /// </summary>
    [Experimental(diagnosticId: "NETEXT0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public const string EventName = "event_name";

    /// <summary>
    /// Failure source.
    /// </summary>
    [Experimental(diagnosticId: "NETEXT0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public const string FailureSource = "failure_source";

    /// <summary>
    /// Failure reason.
    /// </summary>
    [Experimental(diagnosticId: "NETEXT0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public const string FailureReason = "failure_reason";

    /// <summary>
    /// Failure summary.
    /// </summary>
    [Experimental(diagnosticId: "NETEXT0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public const string FailureSummary = "failure_summary";

    /// <summary>
    /// Dependency name.
    /// </summary>
    [Experimental(diagnosticId: "NETEXT0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public const string DependencyName = "dep_name";

    /// <summary>
    /// Request name.
    /// </summary>
    [Experimental(diagnosticId: "NETEXT0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public const string RequestName = "req_name";

    /// <summary>
    /// Gets a list of all dimension names.
    /// </summary>
    /// <returns>A read-only <see cref="IReadOnlyList{String}"/> of all dimension names.</returns>
    [Experimental(diagnosticId: "NETEXT0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IReadOnlyList<string> DimensionNames { get; } =
        Array.AsReadOnly(new[]
        {
            PipelineName,
            PipelineKey,
            ResultType,
            PolicyName,
            EventName,
            FailureSource,
            FailureReason,
            FailureSummary,
            DependencyName,
            RequestName,
        });
}
