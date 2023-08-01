// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.Extensions.Telemetry.Logging;

public partial class LoggerMessageState : ILogPropertyCollector
{
    /// <inheritdoc />
    public void Add(string propertyName, object? propertyValue)
    {
        string fullName = PropertyNamePrefix.Length > 0 ? PropertyNamePrefix + propertyName : propertyName;
        var index = EnsurePropertySpace(1);
        _properties[index] = new(fullName, propertyValue);
    }

    /// <inheritdoc />
    public void Add(string propertyName, object? propertyValue, DataClassification classification)
    {
        string fullName = PropertyNamePrefix.Length > 0 ? PropertyNamePrefix + propertyName : propertyName;
        var index = EnsureClassifiedPropertySpace(1);
        _classifiedProperties[index] = new(fullName, propertyValue, classification);
    }

    /// <summary>
    /// Gets or sets the parameter name that is prepended to all property names added to this instance using the
    /// <see cref="ILogPropertyCollector.Add(string, object?)"/> or <see cref="ILogPropertyCollector.Add(string, object?, DataClassification)"/>
    /// methods.
    /// </summary>
    public string PropertyNamePrefix { get; set; } = string.Empty;
}
