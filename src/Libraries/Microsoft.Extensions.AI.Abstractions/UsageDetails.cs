// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides usage details about a request/response.</summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class UsageDetails
{
    /// <summary>Gets or sets the number of tokens in the input.</summary>
    public int? InputTokenCount { get; set; }

    /// <summary>Gets or sets the number of tokens in the output.</summary>
    public int? OutputTokenCount { get; set; }

    /// <summary>Gets or sets the total number of tokens used to produce the response.</summary>
    public int? TotalTokenCount { get; set; }

    /// <summary>Gets or sets a dictionary of additional usage counts.</summary>
    public AdditionalPropertiesDictionary<long>? AdditionalCounts { get; set; }

    /// <summary>Adds usage data from another <see cref="UsageDetails"/> into this instance.</summary>
    public void Add(UsageDetails usage)
    {
        _ = Throw.IfNull(usage);
        InputTokenCount = NullableSum(InputTokenCount, usage.InputTokenCount);
        OutputTokenCount = NullableSum(OutputTokenCount, usage.OutputTokenCount);
        TotalTokenCount = NullableSum(TotalTokenCount, usage.TotalTokenCount);

        if (usage.AdditionalCounts is { } countsToAdd)
        {
            if (AdditionalCounts is null)
            {
                AdditionalCounts = new(countsToAdd);
            }
            else
            {
                foreach (var kvp in countsToAdd)
                {
                    AdditionalCounts[kvp.Key] = AdditionalCounts.TryGetValue(kvp.Key, out var existingValue) ?
                        kvp.Value + existingValue :
                        kvp.Value;
                }
            }
        }
    }

    /// <summary>Gets a string representing this instance to display in the debugger.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal string DebuggerDisplay
    {
        get
        {
            List<string> parts = [];

            if (InputTokenCount is int input)
            {
                parts.Add($"{nameof(InputTokenCount)} = {input}");
            }

            if (OutputTokenCount is int output)
            {
                parts.Add($"{nameof(OutputTokenCount)} = {output}");
            }

            if (TotalTokenCount is int total)
            {
                parts.Add($"{nameof(TotalTokenCount)} = {total}");
            }

            if (AdditionalCounts is { } additionalCounts)
            {
                foreach (var entry in additionalCounts)
                {
                    parts.Add($"{entry.Key} = {entry.Value}");
                }
            }

            return string.Join(", ", parts);
        }
    }

    private static int? NullableSum(int? a, int? b) => (a.HasValue || b.HasValue) ? (a ?? 0) + (b ?? 0) : null;
}
