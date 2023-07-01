// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.Extensions.Telemetry.Logging;

public partial class LoggerMessageState : ILogPropertyCollector
{
    private const string Separator = "_";

    /// <inheritdoc />
    void ILogPropertyCollector.Add(string propertyName, object? propertyValue)
    {
        string fullName = ParameterName.Length > 0 ? ParameterName + Separator + propertyName : propertyName;
        var s = AllocPropertySpace(1);
        s[0] = new(fullName, propertyValue);
    }

    /// <inheritdoc />
    void ILogPropertyCollector.Add(string propertyName, object? propertyValue, DataClassification classification)
    {
        string fullName = ParameterName.Length > 0 ? ParameterName + Separator + propertyName : propertyName;
        var s = AllocClassifiedPropertySpace(1);
        s[0] = new(fullName, propertyValue, classification);
    }

    internal string ParameterName { get; set; } = string.Empty;
}
