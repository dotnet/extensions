// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace ApiChief.Format;

internal static class FormattingExtensions
{
    private static readonly HashSet<char> _numberLiterals = new() { 'l', 'L', 'u', 'U', 'f', 'F', 'd', 'D', 'm', 'M' };
    private static readonly HashSet<char> _secondCharInLiterals = new() { 'l', 'L', 'u', 'U' };
    private static readonly HashSet<char> _possibleSpecialCharactersInANumber = new() { '.', 'x', 'X', 'b', 'B' };

    /// <summary>
    /// Ensures a single space between parameters.
    /// </summary>
    public static string WithSpaceBetweenParameters(this string signature)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < signature.Length; i++)
        {
            var current = signature[i];
            sb.Append(current);

            if (current == '"')
            {
                var index = i + 1;
                var next = signature[index];

                while (next != '"')
                {
                    sb.Append(next);
                    index++;
                    next = signature[index];
                }

                sb.Append(next);
#pragma warning disable S127 // "for" loop stop conditions should be invariant
                i = index;
#pragma warning restore S127 // "for" loop stop conditions should be invariant
            }
            else if (current == ',')
            {
                if (i + 1 < signature.Length)
                {
                    var next = signature[i + 1];

                    if (next != ' ')
                    {
                        sb.Append(' ');
                    }
                }
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Deletes literal letters from the number.
    /// </summary>
    public static string WithNumbersWithoutLiterals(this string memberDecl)
    {
        var sb = new StringBuilder();

        var inMethod = false;
        var sawEqualitySign = false;

        for (var i = 0; i < memberDecl.Length; i++)
        {
            var current = memberDecl[i];

            if (!inMethod)
            {
                if (current == '(')
                {
                    inMethod = true;
                }

                sb.Append(current);

                continue;
            }

            sb.Append(current);

            if (current == ')')
            {
                inMethod = false;
            }
            else if (current == '"')
            {
                var initial = i + 1;
                var next = memberDecl[initial];

                while (next != '"')
                {
                    sb.Append(next);
                    initial++;
                    next = memberDecl[initial];
                }

                sb.Append(next);
#pragma warning disable S127 // "for" loop stop conditions should be invariant
                i = initial;
#pragma warning restore S127 // "for" loop stop conditions should be invariant
            }
#pragma warning disable S2583 // Conditionally executed code should be reachable
            else if (current == '=')
            {
                sawEqualitySign = true;
            }
            else if (char.IsDigit(current) && sawEqualitySign)
            {
                var initial = i + 1;
                var next = memberDecl[initial];

#pragma warning disable S1067 // Expressions should not be too complex
                while (char.IsDigit(next) || (char.IsDigit(memberDecl[initial - 1]) && _possibleSpecialCharactersInANumber.Contains(next)))
                {
                    sb.Append(next);
                    initial++;
                    next = memberDecl[initial];
                }
#pragma warning restore S1067 // Expressions should not be too complex

                if (!_numberLiterals.Contains(next))
                {
                    sb.Append(next);
                }
                else if (_secondCharInLiterals.Contains(memberDecl[initial + 1]))
                {
                    initial++;
                }
#pragma warning disable S127 // "for" loop stop conditions should be invariant
                i = initial;
#pragma warning restore S127 // "for" loop stop conditions should be invariant
                sawEqualitySign = false;
            }
#pragma warning restore S2583 // Conditionally executed code should be reachable

        }

        return sb.ToString();
    }
}
