// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.LocalAnalyzers.ApiLifecycle;

internal static class Utils
{
    private static readonly char[] _colon = { ':' };
    private static readonly char[] _comma = { ',' };
    private static readonly string[] _whereSeparator = { " where " };

    public static string[] GetConstraints(string typeSignature)
    {
        var whereClauseIndex = typeSignature.IndexOf(" where ", StringComparison.Ordinal);

        if (whereClauseIndex == -1)
        {
            return Array.Empty<string>();
        }

        // Extract everything from the first "where" onwards, then split into
        // individual "where T : ..." clauses and collect the constraint values.
        var constraintsPart = typeSignature.Substring(whereClauseIndex);
        var clauses = constraintsPart.Split(_whereSeparator, StringSplitOptions.RemoveEmptyEntries);
        var result = new List<string>();

        foreach (var clause in clauses)
        {
            var colonIndex = clause.IndexOf(':');
            if (colonIndex >= 0)
            {
                var constraintValues = clause.Substring(colonIndex + 1);
                foreach (var c in constraintValues.Split(_comma))
                {
                    result.Add(c.Trim());
                }
            }
        }

        return result.ToArray();
    }

    public static string StripBaseAndConstraints(string typeSignature)
    {
        var type = typeSignature.Split(_colon)[0];
        var whereClauseIndex = type.IndexOf(" where ", StringComparison.Ordinal);

        if (whereClauseIndex != -1)
        {
            return type.Substring(0, whereClauseIndex).Trim();
        }

        return type.Trim();
    }

    public static string[] GetBaseTypes(string typeSignature)
    {
        var whereClauseIndex = typeSignature.IndexOf(" where ", StringComparison.Ordinal);
        var substrings = typeSignature.Split(_colon);
        var result = new List<string>();

        if (whereClauseIndex == -1)
        {
            if (substrings.Length > 1)
            {
                GetBaseTypesImpl(result, substrings[1]);
            }
        }
        else
        {
            if (substrings.Length > 2)
            {
                var substring = substrings[1].Substring(0, substrings[1].IndexOf(" where ", StringComparison.Ordinal));
                GetBaseTypesImpl(result, substring.Trim());
            }
        }

        return result.ToArray();
    }

    private static void GetBaseTypesImpl(List<string> results, string baseTypesString)
    {
        var generic = 0;
        var start = 0;

        for (var i = 0; i < baseTypesString.Length; i++)
        {
            if (baseTypesString[i] == '<')
            {
                generic++;
            }
            else if (baseTypesString[i] == '>')
            {
                generic--;
            }
            else if (generic == 0 && baseTypesString[i] == ',')
            {
                results.Add(baseTypesString.Substring(start, i - start).Trim());
                start = i + 1;
            }
        }

        results.Add(baseTypesString.Substring(start, baseTypesString.Length - start).Trim());
    }
}
