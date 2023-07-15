// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Telemetry.Logging;

/// <summary>
/// Additional state to use with <see cref="ILogger.Log"/>.
/// </summary>
[Experimental(diagnosticId: "TBD", UrlFormat = "TBD")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed partial class LoggerMessageState
{
    private KeyValuePair<string, object?>[] _properties = Array.Empty<KeyValuePair<string, object?>>();
    private ClassifiedProperty[] _classifiedProperties = Array.Empty<ClassifiedProperty>();

    /// <summary>
    /// Gets the array of properties.
    /// </summary>
#pragma warning disable CA1819 // Properties should not return arrays
    public KeyValuePair<string, object?>[] PropertyArray => _properties;

    /// <summary>
    /// Gets the array of classified properties.
    /// </summary>
    public ClassifiedProperty[] ClassifiedPropertyArray => _classifiedProperties;
#pragma warning restore CA1819 // Properties should not return arrays

    /// <summary>
    /// Allocates some room to put some properties.
    /// </summary>
    /// <param name="count">The amount of space to allocate.</param>
    /// <returns>The index in the <see cref="PropertyArray"/> where to store the properties.</returns>
    public int EnsurePropertySpace(int count)
    {
        int avail = _properties.Length - NumProperties;
        if (count > avail)
        {
            var need = _properties.Length + (count - avail);
            Array.Resize(ref _properties, need);
        }

        var index = NumProperties;
        NumProperties += count;
        return index;
    }

    /// <summary>
    /// Allocates some room to put some properties.
    /// </summary>
    /// <param name="count">The amount of space to allocate.</param>
    /// <returns>The index in the <see cref="ClassifiedPropertyArray"/> where to store the classified properties.</returns>
    public int EnsureClassifiedPropertySpace(int count)
    {
        int avail = _classifiedProperties.Length - NumClassifiedProperties;
        if (count > avail)
        {
            var need = _classifiedProperties.Length + (count - avail);
            Array.Resize(ref _classifiedProperties, need);
        }

        var index = NumClassifiedProperties;
        NumClassifiedProperties += count;
        return index;
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

        for (int i = 0; i < NumProperties; i++)
        {
            if (sb.Length > 0)
            {
                _ = sb.Append(',');
            }

            _ = sb.Append(_properties[i].Key);
            _ = sb.Append('=');
            _ = sb.Append(_properties[i].Value);
        }

        for (int i = 0; i < NumClassifiedProperties; i++)
        {
            if (sb.Length > 0)
            {
                _ = sb.Append(',');
            }

            // note we don't emit the value here as that could lead to a privacy incident.
            _ = sb.Append(_classifiedProperties[i].Name);
            _ = sb.Append('=');
            _ = sb.Append(_classifiedProperties[i].Classification.ToString());
        }

        var result = sb.ToString();
        PoolFactory.SharedStringBuilderPool.Return(sb);

        return result;
    }
}

