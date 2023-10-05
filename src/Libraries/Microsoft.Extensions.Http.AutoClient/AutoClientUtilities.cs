// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;

namespace Microsoft.Extensions.Http.AutoClient;

/// <summary>
/// Utilities for AutoClient feature.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class AutoClientUtilities
{
    private static readonly char[] _slashes = { '/', '\\' };
    private static readonly char[] _slashesOrDot = { '/', '\\', '.' };

    /// <summary>
    /// Returns whether a value is a valid path argument.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns>Whether the value is a valid path argument.</returns>
    public static bool IsPathParameterValid(string value)
    {
        var trimmed = value.AsSpan().Trim();

        if (trimmed.Length == 0)
        {
            return false;
        }

        // Fast path for most common cases
        var index = trimmed.IndexOfAny(_slashesOrDot);
        if (index == -1)
        {
            return true;
        }

        // Slashes can't be used
        if (trimmed.Slice(index).IndexOfAny(_slashes) >= 0)
        {
            return false;
        }

        // The trimmed string can't be made of dots only

#if NET7_0_OR_GREATER

        if (trimmed.IndexOfAnyExcept('.') >= 0)
        {
            return true;
        }

#else

        for (var i = 0; i < trimmed.Length; i++)
        {
            if (trimmed[i] != '.')
            {
                return true;
            }
        }

#endif

        return false;
    }
}
