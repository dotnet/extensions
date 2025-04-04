// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Telemetry.Metrics;

/// <summary>
/// Provides a way to add common tags to the all the metrics in this class.
/// </summary>
/// <example>
/// <code language="csharp">
/// [CommonTag("Category", "Http")]
/// [CommonTag("MetricVersion", "v1")]
/// static partial class Metric
/// {
///     [Counter("RequestName", "RequestStatusCode")]
///     static partial RequestCounter CreateRequestCounter(Meter meter);
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class CommonTagAttribute : Attribute
{
    public CommonTagAttribute(string tagName, string tagValue)
    {
        if (string.IsNullOrEmpty(tagName))
        {
            throw new ArgumentException("tagName name cannot be null or empty.", nameof(tagName));
        }

        this.TagName = tagName;
        this.TagValue = tagValue;
    }

    public string TagName { get; }
    public string TagValue { get; }
}
