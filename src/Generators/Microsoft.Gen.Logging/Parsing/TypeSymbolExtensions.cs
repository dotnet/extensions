// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.Logging.Parsing;

internal static class TypeSymbolExtensions
{
    internal static bool IsEnumerable(this ITypeSymbol sym, SymbolHolder symbols)
        => (sym.ImplementsInterface(symbols.EnumerableSymbol) || SymbolEqualityComparer.Default.Equals(sym, symbols.EnumerableSymbol))
            && sym.SpecialType != SpecialType.System_String;

    internal static bool ImplementsIConvertible(this ITypeSymbol sym, SymbolHolder symbols)
    {
        sym = sym.GetPossiblyNullWrappedType();

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
        sym = sym.GetPossiblyNullWrappedType();

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
    {
        sym = sym.GetPossiblyNullWrappedType();

        return symbols.SpanFormattableSymbol != null && (sym.ImplementsInterface(symbols.SpanFormattableSymbol) || SymbolEqualityComparer.Default.Equals(sym, symbols.SpanFormattableSymbol));
    }

    internal static bool IsSpecialType(this ITypeSymbol typeSymbol, SymbolHolder symbols)
        => typeSymbol.SpecialType != SpecialType.None ||
        typeSymbol.OriginalDefinition.SpecialType != SpecialType.None ||
#pragma warning disable RS1024
        symbols.IgnorePropertiesSymbols.Contains(typeSymbol);
#pragma warning restore RS1024

    internal static bool HasCustomToString(this ITypeSymbol type)
    {
        ITypeSymbol? current = type;
        while (current != null && current.SpecialType != SpecialType.System_Object)
        {
            if (current.GetMembers("ToString").Where(m => m.Kind == SymbolKind.Method && m.DeclaredAccessibility == Accessibility.Public).Cast<IMethodSymbol>().Any(m => m.Parameters.Length == 0))
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }

    internal static ITypeSymbol GetPossiblyNullWrappedType(this ITypeSymbol sym)
    {
        if (sym is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsGenericType && namedTypeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return namedTypeSymbol.TypeArguments[0];
        }

        return sym;
    }
}
