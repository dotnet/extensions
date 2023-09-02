// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Gen.Logging.Model;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.Logging.Parsing;

internal partial class Parser
{
    private bool ProcessTagProviderForParameter(
        AttributeData tagProviderAttribute,
        LoggingMethodParameter lp,
        IParameterSymbol paramSymbol,
        SymbolHolder symbols)
    {
        var paramName = paramSymbol.Name;
        if (!lp.IsNormalParameter)
        {
            Diag(DiagDescriptors.TagProviderInvalidUsage, paramSymbol.GetLocation(), paramName);
            return false;
        }

        var paramTypeSymbol = paramSymbol.Type;

        (lp.OmitReferenceName, var providerType, var providerMethodName) = AttributeProcessors.ExtractTagProviderAttributeValues(tagProviderAttribute);

        var providerMethod = ValidateTagProvider(
            providerType,
            providerMethodName,
            symbols.ITagCollectorSymbol,
            paramTypeSymbol,
            tagProviderAttribute.ApplicationSyntaxReference!.GetSyntax(_cancellationToken).GetLocation());

        if (providerMethod is not null)
        {
            lp.TagProvider = new(providerMethod.Name, providerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            return true;
        }

        return false;
    }

    private bool ProcessTagProviderForProperty(
        AttributeData tagProviderAttribute,
        LoggingProperty lp,
        IPropertySymbol propSymbol,
        SymbolHolder symbols)
    {
        var propName = propSymbol.Name;

        var propTypeSymbol = propSymbol.Type;

        (lp.OmitReferenceName, var providerType, var providerMethodName) = AttributeProcessors.ExtractTagProviderAttributeValues(tagProviderAttribute);

        var providerMethod = ValidateTagProvider(
            providerType,
            providerMethodName,
            symbols.ITagCollectorSymbol,
            propTypeSymbol,
            tagProviderAttribute.ApplicationSyntaxReference!.GetSyntax(_cancellationToken).GetLocation());

        if (providerMethod is not null)
        {
            lp.TagProvider = new(providerMethod.Name, providerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            return true;
        }

        return false;
    }

    private IMethodSymbol? ValidateTagProvider(
        ITypeSymbol providerType,
        string? providerMethodName,
        ITypeSymbol tagCollectorType,
        ITypeSymbol complexObjType,
        Location? attrLocation)
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
                    && SymbolEqualityComparer.Default.Equals(tagCollectorType, method.Parameters[0].Type)
                    && IsAssignableTo(complexObjType, method.Parameters[1].Type))
#pragma warning restore S1067 // Expressions should not be too complex
                {
                    if (IsProviderMethodVisible(method))
                    {
                        return method;
                    }

                    Diag(DiagDescriptors.TagProviderMethodInaccessible, attrLocation, providerMethodName, providerType.ToString());
                    return null;
                }
            }

            if (visitedLoop)
            {
                Diag(DiagDescriptors.TagProviderMethodInvalidSignature, attrLocation,
                    providerMethodName,
                    providerType.ToString(),
                    $"static void {providerMethodName}(ITagCollector, {complexObjType.Name})");
                return null;
            }
        }

        Diag(DiagDescriptors.TagProviderMethodNotFound, attrLocation, providerMethodName, providerType.ToString());
        return null;

        static bool IsParameterCountValid(IMethodSymbol method)
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

        bool IsAssignableTo(ITypeSymbol type, ITypeSymbol target)
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

            return ParserUtilities.IsBaseOrIdentity(type, target, _compilation);
        }

        static bool IsProviderMethodVisible(ISymbol symbol)
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
}
