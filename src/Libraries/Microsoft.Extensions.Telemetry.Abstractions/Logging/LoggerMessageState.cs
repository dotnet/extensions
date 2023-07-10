// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Telemetry.Logging;

/// <summary>
/// Additional state to use with <see cref="ILogger.Log"/>.
/// </summary>
[Experimental(diagnosticId: "TBD", UrlFormat = "TBD")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed partial class LoggerMessageState : IResettable
{
    private KeyValuePair<string, object?>[] _properties = Array.Empty<KeyValuePair<string, object?>>();
    private ClassifiedProperty[] _classifiedProperties = Array.Empty<ClassifiedProperty>();

    /// <summary>
    /// Allocates some room to put some properties.
    /// </summary>
    /// <param name="count">The amount of space to allocate.</param>
    /// <returns>The slots to initialize with property data.</returns>
    public Span<KeyValuePair<string, object?>> AllocPropertySpace(int count)
    {
        int avail = _properties.Length - NumProperties;
        if (count > avail)
        {
            var need = _properties.Length + (count - avail);
            var fresh = new KeyValuePair<string, object?>[need];
            Array.Copy(_properties, fresh, NumProperties);
            _properties = fresh;
        }

        var sp = _properties.AsSpan(NumProperties, count);
        NumProperties += count;
        return sp;
    }

    /// <summary>
    /// Allocates some room to put some properties.
    /// </summary>
    /// <param name="count">The amount of space to allocate.</param>
    /// <returns>The slots to initialize with property data.</returns>
    public Span<ClassifiedProperty> AllocClassifiedPropertySpace(int count)
    {
        int avail = _classifiedProperties.Length - NumClassifiedProperties;
        if (count > avail)
        {
            var need = _classifiedProperties.Length + (count - avail);
            var fresh = new ClassifiedProperty[need];
            Array.Copy(_classifiedProperties, fresh, NumClassifiedProperties);
            _classifiedProperties = fresh;
        }

        var sp = _classifiedProperties.AsSpan(NumClassifiedProperties, count);
        NumClassifiedProperties += count;
        return sp;
    }

    /// <summary>
    /// Resets state of this object to its initial condition.
    /// </summary>
    public void Clear()
    {
        Array.Clear(_properties, 0, NumProperties);
        Array.Clear(_classifiedProperties, 0, NumClassifiedProperties);
        NumProperties = 0;
        NumClassifiedProperties = 0;
        PropertyNamePrefix = string.Empty;
    }

    /// <summary>
    /// Resets state of this container as described in <see cref="IResettable.TryReset"/>.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if the object successfully reset and can be reused.
    /// </returns>
    bool IResettable.TryReset()
    {
        Clear();
        return true;
    }

    /// <summary>
    /// Gets the list of properties added to this instance.
    /// </summary>
    public ReadOnlySpan<KeyValuePair<string, object?>> Properties => _properties.AsSpan(0, NumProperties);

    /// <summary>
    /// Gets the list of properties which must receive redaction before being used.
    /// </summary>
    public ReadOnlySpan<ClassifiedProperty> ClassifiedProperties => _classifiedProperties.AsSpan(0, NumClassifiedProperties);

    /// <summary>
    /// Gets a value indicating the number of unclassified properties currently in this instance.
    /// </summary>
    public int NumProperties { get; private set; }

    /// <summary>
    /// Gets a value indicating the number of classified properties currently in this instance.
    /// </summary>
    public int NumClassifiedProperties { get; private set; }

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

