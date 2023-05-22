// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.ExtraAnalyzers;

internal static class DataClassificationStaticAnalysisCommon
{
    internal const string DataClassifierNS = "Microsoft.Extensions.Data.Classification";
    internal const string BaseClassifierAttributeName = "DataClassificationAttribute";
    internal const string DataClassificationInterfaceName = "IClassifiedData";

    internal static ITypeSymbol? GetFieldOrPropertyType(this ISymbol symbol)
    {
        if (symbol is IFieldSymbol fieldSymbol)
        {
            return fieldSymbol.Type;
        }
        else if (symbol is IPropertySymbol propertySymbol)
        {
            return propertySymbol.Type;
        }
        else
        {
            return null;
        }
    }

    internal static string GetDataClassifierName(this ISymbol symbol)
    {
        if (symbol != null)
        {
            var classifier = symbol
                .GetAttributes()
                .Where(a => a.IsDataClassifier())
                .SingleOrDefault();

            if (classifier != default)
            {
                var classifierFullName = classifier!.AttributeClass!.Name;
                return classifierFullName.Substring(0, classifierFullName.Length - "Attribute".Length);
            }

            var symbolType = symbol.GetFieldOrPropertyType();

            if (symbolType != null)
            {
                if (symbolType.IsClassifiedData())
                {
                    return symbolType.Name;
                }
            }
        }

        return string.Empty;
    }

    internal static bool IsAnnotatedWithDataClassifier(this ISymbol symbol)
        => symbol.GetAttributes().Any(a => a.IsDataClassifier());

    internal static bool IsClassifiedData(this ITypeSymbol type)
        => type.Interfaces.Any(i => string.Equals(i.Name, DataClassificationInterfaceName, StringComparison.Ordinal));

    internal static bool HasDataClassificationTypes(this Compilation comp)
        => CompilationUsesType(comp, $"{DataClassifierNS}.{DataClassificationInterfaceName}");

    internal static bool HasDataClassifiers(this Compilation comp)
        => CompilationUsesType(comp, $"{DataClassifierNS}.{BaseClassifierAttributeName}");

    internal static bool IsDataClassifier(this AttributeData attribute)
    {
        if (attribute != null && attribute.AttributeClass != null)
        {
            var attrNS = attribute.AttributeClass.ContainingNamespace.ToString();
            var attrBaseName = attribute.AttributeClass.BaseType!.Name;

            return string.Equals(attrNS, DataClassifierNS, StringComparison.Ordinal)
                && string.Equals(attrBaseName, BaseClassifierAttributeName, StringComparison.Ordinal);
        }

        return false;
    }

    internal static ImmutableArray<string> GetDataClassifierNamesAsImmutableArray(this ISymbol symbol)
    {
        return symbol.GetAttributes()
                     .Where(a => a.IsDataClassifier())
                     .Select(a => a.AttributeClass!.ToDisplayString())
                     .Select(s => s.Substring(0, s.Length - "Attribute".Length))
                     .ToImmutableArray();
    }

    private static bool CompilationUsesType(Compilation comp, string fullyQualifiedTypeName)
        => comp!.GetTypeByMetadataName(fullyQualifiedTypeName) != null;
}
