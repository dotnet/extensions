// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

public static partial class AIFunctionFactory
{
    /// <summary>
    /// Removes characters from a .NET member name that shouldn't be used in an AI function name.
    /// </summary>
    /// <param name="memberName">The .NET member name that should be sanitized.</param>
    /// <returns>
    /// Replaces non-alphanumeric characters in the identifier with the underscore character.
    /// Primarily intended to remove characters produced by compiler-generated method name mangling.
    /// </returns>
    internal static string SanitizeMemberName(string memberName)
    {
        _ = Throw.IfNull(memberName);
        return InvalidNameCharsRegex().Replace(memberName, "_");
    }

    /// <summary>Regex that flags any character other than ASCII digits or letters or the underscore.</summary>
#if NET
    [GeneratedRegex("[^0-9A-Za-z_]")]
    private static partial Regex InvalidNameCharsRegex();
#else
    private static Regex InvalidNameCharsRegex() => _invalidNameCharsRegex;
    private static readonly Regex _invalidNameCharsRegex = new("[^0-9A-Za-z_]", RegexOptions.Compiled);
#endif
}
