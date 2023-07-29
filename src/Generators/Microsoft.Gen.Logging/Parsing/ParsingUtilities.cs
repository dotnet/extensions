// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Gen.Logging.Model;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.Logging.Parsing;

internal static class ParsingUtilities
{
    private static readonly HashSet<TypeKind> _allowedParameterTypeKinds = new() { TypeKind.Class, TypeKind.Struct, TypeKind.Interface };

    internal static bool IsEnumerable(ITypeSymbol sym, SymbolHolder symbols)
        => sym.ImplementsInterface(symbols.EnumerableSymbol) && sym.SpecialType != SpecialType.System_String;

    internal static bool ImplementsIConvertible(ITypeSymbol sym, SymbolHolder symbols)
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

    internal static bool ImplementsIFormattable(ITypeSymbol sym, SymbolHolder symbols)
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

#pragma warning disable S107 // Methods should not have too many parameters
    internal static LogPropertiesProcessingResult ProcessLogPropertiesForParameter(
        AttributeData logPropertiesAttribute,
        LoggingMethod lm,
        LoggingMethodParameter lp,
        IParameterSymbol paramSymbol,
        SymbolHolder symbols,
        Action<DiagnosticDescriptor, Location?, object?[]?> diagCallback,
        Compilation comp,
        CancellationToken token)
#pragma warning restore S107 // Methods should not have too many parameters
    {
        var paramName = paramSymbol.Name;
        if (!lp.IsNormalParameter)
        {
            Diag(DiagDescriptors.LogPropertiesInvalidUsage, paramSymbol.GetLocation(), paramName);
            return LogPropertiesProcessingResult.Fail;
        }

        var paramTypeSymbol = paramSymbol.Type;
        var isRegularType =
            paramTypeSymbol.Kind == SymbolKind.NamedType &&
            _allowedParameterTypeKinds.Contains(paramTypeSymbol.TypeKind) &&
            !paramTypeSymbol.IsStatic;

        if (paramTypeSymbol.IsNullableOfT())
        {
            // extract the T from a Nullable<T>
            paramTypeSymbol = ((INamedTypeSymbol)paramTypeSymbol).TypeArguments[0];
        }

        var isObjectWithPropsProvider = IsObjectType(paramTypeSymbol) &&
            !logPropertiesAttribute.ConstructorArguments.IsDefaultOrEmpty;

        if (!isRegularType ||
            (IsSpecialType(paramTypeSymbol, symbols) && !isObjectWithPropsProvider))
        {
            Diag(DiagDescriptors.InvalidTypeToLogProperties, paramSymbol.GetLocation(), paramTypeSymbol.ToDisplayString());
            return LogPropertiesProcessingResult.Fail;
        }

        (lp.SkipNullProperties, lp.OmitParameterName, var providerType, var providerMethodName) = AttributeProcessors.ExtractLogPropertiesAttributeValues(logPropertiesAttribute);

        // is there a custom property provider?
        if (providerType != null)
        {
            var providerMethod = LogPropertiesProviderValidator.Validate(
                providerType,
                providerMethodName,
                symbols.ILogPropertyCollectorSymbol,
                paramTypeSymbol,
                Diag,
                logPropertiesAttribute.ApplicationSyntaxReference!.GetSyntax(token).GetLocation(),
                comp);

            if (providerMethod is not null)
            {
                lp.LogPropertiesProvider =
                    new LoggingPropertyProvider(
                        providerMethod.Name,
                        providerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));

                return LogPropertiesProcessingResult.Succeeded;
            }

            return LogPropertiesProcessingResult.Fail;
        }

        var foundDataClassificationAnnotation = false;

        // No custom properties provider, we'll emit properties by ourselves:
        var typesChain = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        _ = typesChain.Add(paramTypeSymbol); // Add itself
        try
        {
            var props = GetTypePropertiesToLog(paramTypeSymbol, typesChain, symbols, ref foundDataClassificationAnnotation, token);
            if (props.Count > 0)
            {
                lp.PropertiesToLog.AddRange(props);
            }
            else
            {
                Diag(DiagDescriptors.LogPropertiesParameterSkipped, paramSymbol.GetLocation(), paramTypeSymbol.Name, paramName);
                return LogPropertiesProcessingResult.Succeeded;
            }
        }
        catch (TransitiveTypeCycleException ex)
        {
            Diag(DiagDescriptors.LogPropertiesCycleDetected, paramSymbol.GetLocation(), paramName, ex.NamedType.ToDisplayString(), ex.Property.Type.ToDisplayString(), lm.Name);
            return LogPropertiesProcessingResult.Fail;
        }
        catch (MultipleDataClassesAppliedException ex)
        {
            Diag(DiagDescriptors.MultipleDataClassificationAttributes, ex.Property.GetLocation(), null);
            return LogPropertiesProcessingResult.Fail;
        }
        catch (PropertyHiddenException ex)
        {
            Diag(DiagDescriptors.LogPropertiesHiddenPropertyDetected, paramSymbol.GetLocation(), paramName, lm.Name, ex.Property.Name);
            return LogPropertiesProcessingResult.Fail;
        }

        return foundDataClassificationAnnotation
            ? LogPropertiesProcessingResult.SucceededWithRedaction
            : LogPropertiesProcessingResult.Succeeded;

        void Diag(DiagnosticDescriptor desc, Location? loc, params object?[]? args) => diagCallback(desc, loc, args);
    }

    internal static void CheckMethodParametersAreUnique(
        LoggingMethod lm,
        Action<DiagnosticDescriptor, Location?, object?[]?> diagCallback,
        Dictionary<LoggingMethodParameter, IParameterSymbol> parameterSymbols)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var parameter in lm.Parameters)
        {
            var parameterName = lm.GetParameterNameInTemplate(parameter);
            if (!names.Add(parameterName))
            {
                Diag(DiagDescriptors.LogPropertiesNameCollision, parameterSymbols[parameter].GetLocation(), parameter.Name, parameterName, lm.Name);
            }

            if (parameter.HasProperties)
            {
                parameter.TraverseParameterPropertiesTransitively((chain, leaf) =>
                {
                    if (parameter.OmitParameterName)
                    {
                        chain = chain.Skip(1);
                    }

                    var fullName = string.Join("_", chain.Concat(new[] { leaf }).Select(static x => x.Name));
                    if (!names.Add(fullName))
                    {
                        Diag(DiagDescriptors.LogPropertiesNameCollision, parameterSymbols[parameter].GetLocation(), parameter.Name, fullName, lm.Name);
                    }
                });
            }
        }

        void Diag(DiagnosticDescriptor desc, Location? loc, params object[]? args) => diagCallback(desc, loc, args);
    }

    // Returns all the classification attributes attached to a symbol.
    internal static IReadOnlyList<INamedTypeSymbol> GetDataClassificationAttributes(this ISymbol symbol, SymbolHolder symbols)
        => symbol
            .GetAttributes()
            .Select(static attribute => attribute.AttributeClass)
            .Where(x => x is not null && symbols.DataClassificationAttribute is not null && ParserUtilities.IsBaseOrIdentity(x, symbols.DataClassificationAttribute, symbols.Compilation))
            .Select(static x => x!)
            .ToList();

    internal static string GetPropertyIdentifier(IPropertySymbol property, CancellationToken token)
    {
        var syntax = property.DeclaringSyntaxReferences[0].GetSyntax(token);
        return syntax switch
        {
            PropertyDeclarationSyntax propertySyntax => propertySyntax.Identifier.Text, // a regular property
            ParameterSyntax paramSyntax => paramSyntax.Identifier.Text, // a property of a "record"
            _ => property.Name
        };
    }

    private static bool IsSpecialType(ITypeSymbol typeSymbol, SymbolHolder symbols)
        => typeSymbol.SpecialType != SpecialType.None ||
        typeSymbol.OriginalDefinition.SpecialType != SpecialType.None ||
#pragma warning disable RS1024
        symbols.IgnorePropertiesSymbols.Contains(typeSymbol);
#pragma warning restore RS1024

    private static bool IsObjectType(ITypeSymbol typeSymbol)
        => typeSymbol.SpecialType == SpecialType.System_Object ||
        typeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Object;

    private static List<LoggingProperty> GetTypePropertiesToLog(
        ITypeSymbol type,
        ISet<ITypeSymbol> typesChain,
        SymbolHolder symbols,
        ref bool foundDataClassificationAnnotation,
        CancellationToken token)
    {
        var result = new List<LoggingProperty>();
        var overriddenMembers = new HashSet<string>();
        var namedType = type as INamedTypeSymbol;
        while (namedType != null && namedType.SpecialType != SpecialType.System_Object)
        {
            token.ThrowIfCancellationRequested();
            var members = namedType.GetMembers();
            if (members != null)
            {
                foreach (var m in members)
                {
                    // Skip static, non-public or non-properties members:
                    if (m.Kind != SymbolKind.Property ||
                        m.DeclaredAccessibility != Accessibility.Public ||
                        m.IsStatic)
                    {
                        continue;
                    }

                    // Skip properties annotated with [LogPropertyIgnore]
                    if (m.GetAttributes().Any(x => SymbolEqualityComparer.Default.Equals(symbols.LogPropertyIgnoreAttribute, x.AttributeClass)))
                    {
                        continue;
                    }

                    // Skip write-only properties and ones with non-public getter:
                    if (m is not IPropertySymbol property ||
                        property.GetMethod == null ||
                        property.GetMethod.DeclaredAccessibility != Accessibility.Public)
                    {
                        continue;
                    }

                    // Skip properties with delegate types:
                    var propertyType = property.Type;
                    if (propertyType.TypeKind == TypeKind.Delegate)
                    {
                        continue;
                    }

                    // Property is already overridden in derived class, skip it:
                    if ((property.IsVirtual || property.IsOverride) &&
                        overriddenMembers.Contains(property.Name))
                    {
                        continue;
                    }

                    var extractedType = propertyType;
                    if (propertyType.IsNullableOfT())
                    {
                        // extract the T from a Nullable<T>
                        extractedType = ((INamedTypeSymbol)propertyType).TypeArguments[0];
                    }

                    // Checking "extractedType" here:
                    if (typesChain.Contains(extractedType))
                    {
                        throw new TransitiveTypeCycleException(property, namedType); // Interrupt the whole traversal
                    }

                    // Adding property that overrides some base class virtual property:
                    if (property.IsOverride)
                    {
                        _ = overriddenMembers.Add(property.Name);
                    }

                    // Check if this property hides a base property:
                    if (ParserUtilities.PropertyHasModifier(property, SyntaxKind.NewKeyword, token))
                    {
                        throw new PropertyHiddenException(property); // Interrupt the whole traversal
                    }

                    bool isEnumerable = IsEnumerable(propertyType, symbols);
                    bool implementsIConvertible = ImplementsIConvertible(propertyType, symbols);
                    bool implementsIFormattable = ImplementsIFormattable(propertyType, symbols);

                    bool propertyHasComplexType =
#pragma warning disable CA1508 // Avoid dead conditional code
                        extractedType.Kind == SymbolKind.NamedType &&
#pragma warning restore CA1508 // Avoid dead conditional code
                        !IsSpecialType(extractedType, symbols) &&
                        !isEnumerable &&
                        extractedType.DeclaredAccessibility == Accessibility.Public;    // Ignore non-public types for transitive traversal

                    IReadOnlyCollection<LoggingProperty> transitiveMembers = Array.Empty<LoggingProperty>();

                    INamedTypeSymbol? classificationAttributeType = null;

                    if (propertyHasComplexType)
                    {
                        _ = typesChain.Add(namedType);
                        transitiveMembers = GetTypePropertiesToLog(
                            extractedType,
                            typesChain,
                            symbols,
                            ref foundDataClassificationAnnotation,
                            token);

                        _ = typesChain.Remove(namedType);
                    }
                    else
                    {
                        var propertyDataClassAttributes = property.GetDataClassificationAttributes(symbols);
                        if (propertyDataClassAttributes.Count > 1)
                        {
                            throw new MultipleDataClassesAppliedException(property); // Interrupt the whole traversal
                        }
                        else
                        {
                            if (propertyDataClassAttributes.Count == 1)
                            {
                                classificationAttributeType = propertyDataClassAttributes[0];

                                foundDataClassificationAnnotation = true;
                            }
                        }
                    }

                    var needsAtSign = false;
                    if (!property.DeclaringSyntaxReferences.IsDefaultOrEmpty)
                    {
                        var propertyIdentifier = GetPropertyIdentifier(property, token);

                        if (!string.IsNullOrEmpty(propertyIdentifier))
                        {
                            needsAtSign = propertyIdentifier[0] == '@';
                        }
                    }

                    // Using here original "propertyType":
                    LoggingProperty propertyToLog = new(
                        property.Name,
                        propertyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        classificationAttributeType?.ToDisplayString(),
                        needsAtSign,
                        propertyType.NullableAnnotation == NullableAnnotation.Annotated,
                        propertyType.IsReferenceType,
                        isEnumerable,
                        implementsIConvertible,
                        implementsIFormattable,
                        transitiveMembers);

                    result.Add(propertyToLog);
                }
            }

            // This is to support inheritance, i.e. to fetch public properties of base class(-es) as well:
            namedType = namedType.BaseType;
        }

        return result;
    }
}
