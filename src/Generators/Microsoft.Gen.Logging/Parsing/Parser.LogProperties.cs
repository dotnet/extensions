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

#pragma warning disable S1067 // Expressions should not be too complex
#pragma warning disable S1168 // Empty arrays and collections should be returned instead of null

namespace Microsoft.Gen.Logging.Parsing;

internal partial class Parser
{
    private static readonly HashSet<TypeKind> _allowedTypeKinds = new() { TypeKind.Class, TypeKind.Struct, TypeKind.Interface };

    private bool ProcessLogPropertiesForParameter(
        AttributeData logPropertiesAttribute,
        LoggingMethod lm,
        LoggingMethodParameter lp,
        IParameterSymbol paramSymbol,
        SymbolHolder symbols)
    {
        var paramName = paramSymbol.Name;
        if (!lp.IsNormalParameter)
        {
            Diag(DiagDescriptors.LogPropertiesInvalidUsage, paramSymbol.GetLocation(), paramName);
            return false;
        }

        if (!CanLogProperties(paramSymbol, paramSymbol.Type, symbols))
        {
            return false;
        }

        var paramTypeSymbol = paramSymbol.Type;
        if (paramTypeSymbol.IsNullableOfT())
        {
            // extract the T from a Nullable<T>
            paramTypeSymbol = ((INamedTypeSymbol)paramTypeSymbol).TypeArguments[0];
        }

        (lp.SkipNullProperties, lp.OmitReferenceName) = AttributeProcessors.ExtractLogPropertiesAttributeValues(logPropertiesAttribute);

        var typesChain = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        _ = typesChain.Add(paramTypeSymbol); // Add itself

        var props = GetTypePropertiesToLog(paramTypeSymbol, typesChain, symbols);
        if (props == null)
        {
            return false;
        }

        if (props.Count > 0)
        {
            lp.Properties.AddRange(props);
        }
        else
        {
            Diag(DiagDescriptors.LogPropertiesParameterSkipped, paramSymbol.GetLocation(), paramTypeSymbol.Name, paramName);
            return true;
        }

        return true;

        static string GetPropertyIdentifier(IPropertySymbol property, CancellationToken token)
        {
            var syntax = property.DeclaringSyntaxReferences[0].GetSyntax(token);
            return syntax switch
            {
                PropertyDeclarationSyntax propertySyntax => propertySyntax.Identifier.Text, // a regular property
                ParameterSyntax paramSyntax => paramSyntax.Identifier.Text, // a property of a "record"
                _ => property.Name
            };
        }

        List<LoggingProperty>? GetTypePropertiesToLog(
            ITypeSymbol type,
            ISet<ITypeSymbol> typesChain,
            SymbolHolder symbols)
        {
            var result = new List<LoggingProperty>();
            var seenProps = new HashSet<string>();
            var namedType = type as INamedTypeSymbol;
            while (namedType != null && namedType.SpecialType != SpecialType.System_Object)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var members = namedType.GetMembers();
                if (members != null)
                {
                    foreach (var property in members.Where(m => m.Kind == SymbolKind.Property).Cast<IPropertySymbol>())
                    {
                        var logPropertiesAttribute = ParserUtilities.GetSymbolAttributeAnnotationOrDefault(symbols.LogPropertiesAttribute, property);
                        var logPropertyIgnoreAttribute = ParserUtilities.GetSymbolAttributeAnnotationOrDefault(symbols.LogPropertyIgnoreAttribute, property);
                        var tagProviderAttribute = ParserUtilities.GetSymbolAttributeAnnotationOrDefault(symbols.TagProviderAttribute, property);

                        var count = 0;
                        if (logPropertiesAttribute != null)
                        {
                            count++;
                        }

                        if (logPropertyIgnoreAttribute != null)
                        {
                            count++;
                        }

                        if (tagProviderAttribute != null)
                        {
                            count++;
                        }

                        if (count > 1)
                        {
                            Diag(DiagDescriptors.CantMixAttributes, property.GetLocation());
                            continue;
                        }

                        if (!seenProps.Add(property.Name))
                        {
                            // already saw this property in a derived type, skip it
                            continue;
                        }

                        var classification = new HashSet<string>();
                        var current = type;
                        while (current != null)
                        {
                            classification.UnionWith(GetDataClassificationAttributes(current, symbols).Select(x => x.ToDisplayString()));
                            current = current.BaseType;
                        }

                        classification.UnionWith(GetDataClassificationAttributes(property, symbols).Select(x => x.ToDisplayString()));

                        var extractedType = property.Type;
                        if (extractedType.IsNullableOfT())
                        {
                            // extract the T from a Nullable<T>
                            extractedType = ((INamedTypeSymbol)extractedType).TypeArguments[0];
                        }

                        var lp = new LoggingProperty
                        {
                            Name = property.Name,
                            Type = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            ClassificationAttributeTypes = classification,
                            IsReference = property.Type.IsReferenceType,
                            IsEnumerable = property.Type.IsEnumerable(symbols),
                            IsNullable = property.Type.NullableAnnotation == NullableAnnotation.Annotated,
                            ImplementsIConvertible = property.Type.ImplementsIConvertible(symbols),
                            ImplementsIFormattable = property.Type.ImplementsIFormattable(symbols),
                            ImplementsISpanFormattable = property.Type.ImplementsISpanFormattable(symbols),
                        };

                        if (!property.DeclaringSyntaxReferences.IsDefaultOrEmpty)
                        {
                            var propertyIdentifier = GetPropertyIdentifier(property, _cancellationToken);

                            if (!string.IsNullOrEmpty(propertyIdentifier))
                            {
                                lp.NeedsAtSign = propertyIdentifier[0] == '@';
                            }
                        }

                        if (property.Type.IsValueType && !property.Type.IsNullableOfT())
                        {
#pragma warning disable S125
                            // deal with an oddity in Roslyn. If you have this case:
                            //
                            // class Gen<T>
                            // {
                            //     public T? Value { get; set; }
                            // }
                            //
                            // class Foo
                            // {
                            //     public Gen<int> MyInt { get; set; }
                            // }
                            //
                            // then Roslyn claims that MyInt has nullable annotations. Yet, doing x.MyInt?.ToString isn't valid.
                            // So we explicitly check whether the value is indeed a Nullable<T>.
                            lp.IsNullable = false;
#pragma warning restore S125
                        }

                        // Check if this property hides a base property
                        if (ParserUtilities.PropertyHasModifier(property, SyntaxKind.NewKeyword, _cancellationToken))
                        {
                            Diag(DiagDescriptors.LogPropertiesHiddenPropertyDetected, paramSymbol.GetLocation(), paramName, lm.Name, property.Name);
                            return null;
                        }

                        if (logPropertiesAttribute != null)
                        {
                            _ = CanLogProperties(property, property.Type, symbols);

                            if ((property.DeclaredAccessibility != Accessibility.Public || property.IsStatic)
                              || (property.GetMethod == null || property.GetMethod.DeclaredAccessibility != Accessibility.Public))
                            {
                                Diag(DiagDescriptors.InvalidAttributeUsage, logPropertiesAttribute.ApplicationSyntaxReference?.GetSyntax(_cancellationToken).GetLocation(), "LogProperties");
                                continue;
                            }

                            // Checking "extractedType" here
                            if (typesChain.Contains(extractedType))
                            {
                                Diag(DiagDescriptors.LogPropertiesCycleDetected, paramSymbol.GetLocation(), paramName, namedType.ToDisplayString(), property.Type.ToDisplayString(), lm.Name);
                                return null;
                            }

                            var propName = property.Name;

                            extractedType = property.Type;
                            if (extractedType.IsNullableOfT())
                            {
                                // extract the T from a Nullable<T>
                                extractedType = ((INamedTypeSymbol)extractedType).TypeArguments[0];
                            }

                            _ = typesChain.Add(namedType);
                            var props = GetTypePropertiesToLog(extractedType, typesChain, symbols);
                            _ = typesChain.Remove(namedType);

                            if (props != null)
                            {
                                lp.Properties.AddRange(props);
                            }
                        }

                        if (tagProviderAttribute != null)
                        {
                            if ((property.DeclaredAccessibility != Accessibility.Public || property.IsStatic)
                              || (property.GetMethod == null || property.GetMethod.DeclaredAccessibility != Accessibility.Public))
                            {
                                Diag(DiagDescriptors.InvalidAttributeUsage, tagProviderAttribute.ApplicationSyntaxReference?.GetSyntax(_cancellationToken).GetLocation(), "TagProvider");
                            }

                            if (!ProcessTagProviderForProperty(tagProviderAttribute, lp, property, symbols))
                            {
                                lp.TagProvider = null;
                            }
                        }

                        if (tagProviderAttribute == null && logPropertiesAttribute == null)
                        {
                            if ((property.DeclaredAccessibility != Accessibility.Public || property.IsStatic)
                              || (property.GetMethod == null || property.GetMethod.DeclaredAccessibility != Accessibility.Public)
                              || (property.Type.TypeKind == TypeKind.Delegate)
                              || (logPropertyIgnoreAttribute != null))
                            {
                                continue;
                            }
                        }

                        if (lp.HasDataClassification && (lp.HasProperties || lp.HasTagProvider))
                        {
                            Diag(DiagDescriptors.CantUseDataClassificationWithLogPropertiesOrTagProvider, property.GetLocation());
                            lp.ClassificationAttributeTypes.Clear();
                        }

                        result.Add(lp);
                    }
                }

                // This is to support inheritance, i.e. to fetch public properties of base class(-es) as well:
                namedType = namedType.BaseType;
            }

            return result;
        }

        bool CanLogProperties(ISymbol sym, ITypeSymbol symType, SymbolHolder symbols)
        {
            var isRegularType =
                symType.Kind == SymbolKind.NamedType &&
                _allowedTypeKinds.Contains(symType.TypeKind) &&
                !symType.IsStatic;

            if (symType.IsNullableOfT())
            {
                // extract the T from a Nullable<T>
                symType = ((INamedTypeSymbol)symType).TypeArguments[0];
            }

            if (!isRegularType || symType.IsSpecialType(symbols))
            {
                Diag(DiagDescriptors.InvalidTypeToLogProperties, sym.GetLocation(), symType.ToDisplayString());
                return false;
            }

            return true;
        }
    }
}
