// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.Logging.Parsing;

internal static class LogPropertiesProviderValidator
{
    internal delegate void DiagCallback(DiagnosticDescriptor desc, Location? loc, params object?[]? args);

    public static IMethodSymbol? Validate(
        ITypeSymbol providerType,
        string? providerMethodName,
        ITypeSymbol logPropertyCollectorType,
        ITypeSymbol complexObjType,
        DiagCallback diagCallback,
        Location? attrLocation,
        Compilation comp)
    {
        if (providerType is IErrorTypeSymbol)
        {
            return null;
        }

        if (providerMethodName != null)
        {
            var methodSymbols = providerType.GetMembers(providerMethodName).Where(m => m.Kind == SymbolKind.Method).Cast<IMethodSymbol>();
            bool visitedLoop = false;
            foreach (var method in methodSymbols)
            {
                visitedLoop = true;

#pragma warning disable S1067 // Expressions should not be too complex
                if (method.IsStatic
                    && method.ReturnsVoid
                    && !method.IsGenericMethod
                    && IsParameterCountValid(method)
                    && method.Parameters[0].RefKind == RefKind.None
                    && method.Parameters[1].RefKind == RefKind.None
                    && SymbolEqualityComparer.Default.Equals(logPropertyCollectorType, method.Parameters[0].Type)
                    && complexObjType.IsAssignableTo(method.Parameters[1].Type, comp))
#pragma warning restore S1067 // Expressions should not be too complex
                {
                    if (IsProviderMethodVisible(method))
                    {
                        return method;
                    }

                    diagCallback(DiagDescriptors.LogPropertiesProviderMethodInaccessible, attrLocation, providerMethodName, providerType.ToString());
                    return null;
                }
            }

            if (visitedLoop)
            {
                diagCallback(DiagDescriptors.LogPropertiesProviderMethodInvalidSignature, attrLocation,
                    providerMethodName,
                    providerType.ToString(),
                    $"static void {providerMethodName}(ILogPropertyCollector, {complexObjType.Name})");
                return null;
            }
        }

        diagCallback(DiagDescriptors.LogPropertiesProviderMethodNotFound, attrLocation, providerMethodName, providerType.ToString());
        return null;
    }

    private static bool IsParameterCountValid(IMethodSymbol method)
    {
        if (method.Parameters.Length == 2)
        {
            return true;
        }

        if (method.Parameters.Length < 2)
        {
            return false;
        }

        for (int i = 2; i < method.Parameters.Length; i++)
        {
            if (!method.Parameters[i].IsOptional)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsAssignableTo(this ITypeSymbol type, ITypeSymbol target, Compilation comp)
    {
        if (type.NullableAnnotation == NullableAnnotation.Annotated)
        {
            if (target.NullableAnnotation == NullableAnnotation.NotAnnotated)
            {
                return false;
            }
        }

        if (target.TypeKind == TypeKind.Interface)
        {
            if (SymbolEqualityComparer.Default.Equals(type.WithNullableAnnotation(NullableAnnotation.None), target.WithNullableAnnotation(NullableAnnotation.None)))
            {
                return true;
            }

            foreach (var iface in type.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(target.WithNullableAnnotation(NullableAnnotation.None), iface.WithNullableAnnotation(NullableAnnotation.None)))
                {
                    return true;
                }
            }

            return false;
        }

        return ParserUtilities.IsBaseOrIdentity(type, target, comp);
    }

    private static bool IsProviderMethodVisible(this ISymbol symbol)
    {
        while (symbol != null && symbol.Kind != SymbolKind.Namespace)
        {
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.NotApplicable:
                case Accessibility.Private:
                case Accessibility.Protected:
                    return false;
            }

            symbol = symbol.ContainingSymbol;
        }

        return true;
    }
}
