// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.Extensions.Telemetry.Logging;

public partial class LoggerMessageState : ITagCollector
{
    private const char Separator = '_';

    /// <inheritdoc />
    void ITagCollector.Add(string tagName, object? tagValue)
    {
        string fullName = TagNamePrefix.Length > 0 ? TagNamePrefix + Separator + tagName : tagName;
        AddTag(fullName, tagValue);
    }

    /// <inheritdoc />
    void ITagCollector.Add(string tagName, object? tagValue, DataClassification classification)
    {
        string fullName = TagNamePrefix.Length > 0 ? TagNamePrefix + Separator + tagName : tagName;
        AddClassifiedTag(fullName, tagValue, classification);
    }

    /// <summary>
    /// Gets or sets the parameter name that is prepended to all tag names added to this instance using the
    /// <see cref="ITagCollector.Add(string, object?)"/> or <see cref="ITagCollector.Add(string, object?, DataClassification)"/>
    /// methods.
    /// </summary>
    public string TagNamePrefix { get; set; } = string.Empty;
}
