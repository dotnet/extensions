// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DataIngestion;

#pragma warning disable SA1500 // Braces for multi-line statements should not share line
#pragma warning disable SA1513 // Closing brace should be followed by blank line

/// <summary>
/// Options for configuring the ingestion pipeline.
/// </summary>
public sealed class IngestionPipelineOptions
{
    /// <summary>
    /// Gets or sets the name of the <see cref="ActivitySource"/> used for diagnostics.
    /// </summary>
    public string ActivitySourceName
    {
        get;
        set => field = Throw.IfNullOrEmpty(value);
    } = DiagnosticsConstants.ActivitySourceName;

    /// <summary>
    /// Gets or sets the maximum number of ingestions that are allowed to fail with an error.
    /// </summary>
    /// <value>
    /// The maximum number of ingestions that are allowed to fail with an error.
    /// The default value is 3.
    /// </value>
    /// <remarks>
    /// <para>
    /// When document processing fails with an exception, the <see cref="IngestionPipeline{T}"/>
    /// continues to process files, optionally logging the exceptions.
    /// </para>
    /// <para>
    /// However, in case document processing continues to produce exceptions, this property can be used to
    /// limit the number of ingestions that are allowed to fail with an error. When the limit is reached, the exceptions will be
    /// rethrown to the caller as <see cref="AggregateException"/>.
    /// </para>
    /// <para>
    /// If the value is set to zero, all exceptions immediately terminate the processing.
    /// </para>
    /// <para>
    /// When all ingestions fail, the processing will always terminate with an <see cref="AggregateException"/>.
    /// </para>
    /// </remarks>
    public int MaximumErrorsPerProcessing
    {
        get;
        set => field = Throw.IfLessThan(value, 0);
    } = 3;

    internal IngestionPipelineOptions Clone() => new()
    {
        ActivitySourceName = ActivitySourceName,
        MaximumErrorsPerProcessing = MaximumErrorsPerProcessing
    };
}
