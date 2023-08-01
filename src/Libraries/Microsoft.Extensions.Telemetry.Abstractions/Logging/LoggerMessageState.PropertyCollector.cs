// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.Extensions.Telemetry.Logging;

public partial class LoggerMessageState : ILogPropertyCollector
{
    /// <inheritdoc />
    void ILogPropertyCollector.Add(string propertyName, object? propertyValue)
    {
        string fullName = PropertyNamePrefix.Length > 0 ? PropertyNamePrefix + "_" + propertyName : propertyName;
        AddProperty(fullName, propertyValue);
    }

    /// <inheritdoc />
    void ILogPropertyCollector.Add(string propertyName, object? propertyValue, DataClassification classification)
    {
        string fullName = PropertyNamePrefix.Length > 0 ? PropertyNamePrefix + "_" + propertyName : propertyName;
        AddClassifiedProperty(fullName, propertyValue, classification);
    }

    /// <summary>
    /// Gets or sets the parameter name that is prepended to all property names added to this instance using the
    /// <see cref="ILogPropertyCollector.Add(string, object?)"/> or <see cref="ILogPropertyCollector.Add(string, object?, DataClassification)"/>
    /// methods.
    /// </summary>
    public string PropertyNamePrefix { get; set; } = string.Empty;
}
