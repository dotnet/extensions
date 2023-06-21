// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Telemetry.Enrichment;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Telemetry.Logging;

/// <summary>
/// Additional state to use with <see cref="ILogger.Log"/>.
/// </summary>
[Experimental(diagnosticId: "TBD", UrlFormat = "TBD")]
public partial class LoggerMessageState : IResettable
{
    private readonly PropertyBag _enrichmentPropertyBag;
    private readonly PropertyCollector _propertyCollector;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggerMessageState"/> class.
    /// </summary>
    public LoggerMessageState()
    {
        _enrichmentPropertyBag = new(Properties);
        _propertyCollector = new(Properties, ClassifiedProperties);
    }

    /// <summary>
    /// Adds a property to the state.
    /// </summary>
    /// <param name="propertyName">The name of the property to add.</param>
    /// <param name="propertyValue">The value of the property to add.</param>
    /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="propertyName" /> is empty or contains exclusively whitespace,
    /// or when a property of the same name has already been added.
    /// </exception>
    public void AddProperty(string propertyName, object? propertyValue)
        => Properties.Add(new KeyValuePair<string, object?>(propertyName, propertyValue));

    /// <summary>
    /// Adds a property to the state.
    /// </summary>
    /// <param name="propertyName">The name of the property to add.</param>
    /// <param name="propertyValue">The value of the property to add.</param>
    /// <param name="classification">The data classification of the property value.</param>
    /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="propertyName" /> is empty or contains exclusively whitespace,
    /// or when a property of the same name has already been added.
    /// </exception>
    public void AddProperty(string propertyName, object? propertyValue, DataClassification classification)
        => ClassifiedProperties.Add(new(propertyName, propertyValue, classification));

    /// <summary>
    /// Resets state of this container as described in <see cref="IResettable.TryReset"/>.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if the object successfully reset and can be reused.
    /// </returns>
    public bool TryReset()
    {
        Properties.Clear();
        ClassifiedProperties.Clear();
        _propertyCollector.ParameterName = string.Empty;
        return true;
    }

    /// <summary>
    /// Gets the list of properties added to this instance.
    /// </summary>
    [SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "Not intended for application use")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public List<KeyValuePair<string, object?>> Properties { get; } = new();

    /// <summary>
    /// Gets a list of properties which must receive redaction before being used.
    /// </summary>
    [SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "Not intended for application use")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public List<ClassifiedProperty> ClassifiedProperties { get; } = new();

    /// <summary>
    /// Gets the property collector instance.
    /// </summary>
    /// <param name="parameterName">The name of the parameter to prefix in front of all property names inserted into the collector.</param>
    /// <returns>The collector instance.</returns>
    /// <remarks>
    /// This method is used by the logger message code generator to get an instance of a collector to
    /// use when invoking a custom property collector method.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ILogPropertyCollector GetPropertyCollector(string parameterName)
    {
        _propertyCollector.ParameterName = parameterName;
        return _propertyCollector;
    }

    /// <summary>
    /// Gets an enrichment property bag.
    /// </summary>
    /// <remarks>
    /// This method is used by logger implementations that receive a <see cref="LoggerMessageState" />
    /// instance and want to use the instance as an enrichment property bag in order to harvest
    /// properties from enrichers.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public IEnrichmentPropertyBag EnrichmentPropertyBag => _enrichmentPropertyBag;

    /// <summary>
    /// Returns a string representation of this object.
    /// </summary>
    /// <returns>The string representation of this object.</returns>
    public override string ToString()
    {
        var sb = PoolFactory.SharedStringBuilderPool.Get();

        foreach (var kvp in Properties)
        {
            if (sb.Length > 0)
            {
                _ = sb.Append(',');
            }

            _ = sb.Append(kvp.Key);
            _ = sb.Append('=');
            _ = sb.Append(kvp.Value);
        }

        foreach (var kvp in ClassifiedProperties)
        {
            if (sb.Length > 0)
            {
                _ = sb.Append(',');
            }

            // note we don't emit the value here as that could lead to a privacy incident.
            _ = sb.Append(kvp.Name);
            _ = sb.Append('=');
            _ = sb.Append(kvp.Classification.ToString());
        }

        var result = sb.ToString();
        PoolFactory.SharedStringBuilderPool.Return(sb);

        return result;
    }
}

