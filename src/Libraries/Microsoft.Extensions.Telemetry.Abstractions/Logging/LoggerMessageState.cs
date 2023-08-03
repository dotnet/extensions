// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Telemetry.Logging;

/// <summary>
/// Additional state to use with <see cref="ILogger.Log"/>.
/// </summary>
[Experimental(diagnosticId: Experiments.Telemetry, UrlFormat = Experiments.UrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed partial class LoggerMessageState
{
    private KeyValuePair<string, object?>[] _tags = Array.Empty<KeyValuePair<string, object?>>();
    private KeyValuePair<string, object?>[] _redactedTags = Array.Empty<KeyValuePair<string, object?>>();
    private ClassifiedTag[] _classifiedTags = Array.Empty<ClassifiedTag>();

#pragma warning disable CA1819 // Properties should not return arrays
    /// <summary>
    /// Gets the array of tags.
    /// </summary>
    public KeyValuePair<string, object?>[] TagArray => _tags;

    /// <summary>
    /// Gets the array of tags.
    /// </summary>
    public KeyValuePair<string, object?>[] RedactedTagArray => _redactedTags;

    /// <summary>
    /// Gets the array of classified tags.
    /// </summary>
    public ClassifiedTag[] ClassifiedTagArray => _classifiedTags;
#pragma warning restore CA1819 // Properties should not return arrays

    /// <summary>
    /// Allocates some room to put some tags.
    /// </summary>
    /// <param name="count">The amount of space to allocate.</param>
    /// <returns>The index in the <see cref="TagArray"/> where to store the tags.</returns>
    public int ReserveTagSpace(int count)
    {
        int avail = _tags.Length - NumTags;
        if (count > avail)
        {
            var need = _tags.Length + (count - avail);
            Array.Resize(ref _tags, need);
        }

        var index = NumTags;
        NumTags += count;
        return index;
    }

    /// <summary>
    /// Allocates some room to put some redacted tags.
    /// </summary>
    /// <param name="count">The amount of space to allocate.</param>
    /// <returns>The index in the <see cref="RedactedTagArray"/> where to store the tags.</returns>
    public int ReserveRedactedTagSpace(int count)
    {
        int avail = _redactedTags.Length - NumRedactedTags;
        if (count > avail)
        {
            var need = _redactedTags.Length + (count - avail);
            Array.Resize(ref _redactedTags, need);
        }

        var index = NumRedactedTags;
        NumRedactedTags += count;
        return index;
    }

    /// <summary>
    /// Allocates some room to put some tags.
    /// </summary>
    /// <param name="count">The amount of space to allocate.</param>
    /// <returns>The index in the <see cref="ClassifiedTagArray"/> where to store the classified tags.</returns>
    public int ReserveClassifiedTagSpace(int count)
    {
        int avail = _classifiedTags.Length - NumClassifiedTags;
        if (count > avail)
        {
            var need = _classifiedTags.Length + (count - avail);
            Array.Resize(ref _classifiedTags, need);
        }

        var index = NumClassifiedTags;
        NumClassifiedTags += count;
        return index;
    }

    /// <summary>
    /// Adds a tag to the array.
    /// </summary>
    /// <param name="name">The name of the tag.</param>
    /// <param name="value">The value.</param>
    public void AddTag(string name, object? value)
    {
        var index = ReserveTagSpace(1);
        TagArray[index] = new(name, value);
    }

    /// <summary>
    /// Adds a classified tag to the array.
    /// </summary>
    /// <param name="name">The name of the tag.</param>
    /// <param name="value">The value.</param>
    /// <param name="classification">The data classification of the tag.</param>
    public void AddClassifiedTag(string name, object? value, DataClassification classification)
    {
        var index = ReserveClassifiedTagSpace(1);
        ClassifiedTagArray[index] = new(name, value, classification);
    }

    /// <summary>
    /// Resets state of this object to its initial condition.
    /// </summary>
    public void Clear()
    {
        Array.Clear(_tags, 0, NumTags);
        Array.Clear(_redactedTags, 0, NumRedactedTags);
        Array.Clear(_classifiedTags, 0, NumClassifiedTags);
        NumTags = 0;
        NumRedactedTags = 0;
        NumClassifiedTags = 0;
        TagNamePrefix = string.Empty;
    }

    /// <summary>
    /// Gets a value indicating the number of unclassified tags currently in this instance.
    /// </summary>
    public int NumTags { get; private set; }

    /// <summary>
    /// Gets a value indicating the number of redacted tags currently in this instance.
    /// </summary>
    public int NumRedactedTags { get; private set; }

    /// <summary>
    /// Gets a value indicating the number of classified tags currently in this instance.
    /// </summary>
    public int NumClassifiedTags { get; private set; }

    /// <summary>
    /// Returns a string representation of this object.
    /// </summary>
    /// <returns>The string representation of this object.</returns>
    public override string ToString()
    {
        var sb = PoolFactory.SharedStringBuilderPool.Get();

        for (int i = 0; i < NumTags; i++)
        {
            if (sb.Length > 0)
            {
                _ = sb.Append(',');
            }

            _ = sb.Append(_tags[i].Key);
            _ = sb.Append('=');
            _ = sb.Append(_tags[i].Value);
        }

        for (int i = 0; i < NumClassifiedTags; i++)
        {
            if (sb.Length > 0)
            {
                _ = sb.Append(',');
            }

            // note we don't emit the value here as that could lead to a privacy incident.
            _ = sb.Append(_classifiedTags[i].Name);
            _ = sb.Append('=');
            _ = sb.Append(_classifiedTags[i].Classification.ToString());
        }

        var result = sb.ToString();
        PoolFactory.SharedStringBuilderPool.Return(sb);

        return result;
    }
}

