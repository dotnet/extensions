// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.Logging.Parsing;

internal static class TypeSymbolExtensions
{
    internal static bool IsEnumerable(this ITypeSymbol sym, SymbolHolder symbols)
        => sym.ImplementsInterface(symbols.EnumerableSymbol) && sym.SpecialType != SpecialType.System_String;

    internal static bool ImplementsIConvertible(this ITypeSymbol sym, SymbolHolder symbols)
    {
        foreach (var member in sym.GetMembers("ToString"))
        {
            if (member is IMethodSymbol ts)
            {
                if (ts.DeclaredAccessibility == Accessibility.Public)
                {
                    if (ts.Arity == 0
                        && ts.Parameters.Length == 1
                        && SymbolEqualityComparer.Default.Equals(ts.Parameters[0].Type, symbols.FormatProviderSymbol))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    internal static bool ImplementsIFormattable(this ITypeSymbol sym, SymbolHolder symbols)
    {
        foreach (var member in sym.GetMembers("ToString"))
        {
            if (member is IMethodSymbol ts)
            {
                if (ts.DeclaredAccessibility == Accessibility.Public)
                {
                    if (ts.Arity == 0
                        && ts.Parameters.Length == 2
                        && ts.Parameters[0].Type.SpecialType == SpecialType.System_String
                        && SymbolEqualityComparer.Default.Equals(ts.Parameters[1].Type, symbols.FormatProviderSymbol))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    internal static bool ImplementsISpanFormattable(this ITypeSymbol sym, SymbolHolder symbols)
        => symbols.SpanFormattableSymbol != null && sym.ImplementsInterface(symbols.SpanFormattableSymbol);

    internal static bool IsSpecialType(this ITypeSymbol typeSymbol, SymbolHolder symbols)
        => typeSymbol.SpecialType != SpecialType.None ||
        typeSymbol.OriginalDefinition.SpecialType != SpecialType.None ||
#pragma warning disable RS1024
        symbols.IgnorePropertiesSymbols.Contains(typeSymbol);
#pragma warning restore RS1024
}
