﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Gen.Metering.Exceptions;
using Microsoft.Gen.Metering.Model;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.Metering;

internal sealed class Parser
{
    private const int MaxTagNames = 20;

    private static readonly Regex _regex = new("^[A-Z]+[A-za-z0-9]*$", RegexOptions.Compiled);
    private static readonly Regex _regexTagNames = new("^[A-Za-z]+[A-Za-z0-9_.:-]*$", RegexOptions.Compiled);
    private static readonly SymbolDisplayFormat _typeSymbolFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    private static readonly SymbolDisplayFormat _genericTypeSymbolFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    private static readonly HashSet<SpecialType> _allowedGenericAttributeTypeArgs =
        new()
        {
            SpecialType.System_Byte,
            SpecialType.System_Int16,
            SpecialType.System_Int32,
            SpecialType.System_Int64,
            SpecialType.System_Decimal,
            SpecialType.System_Single,
            SpecialType.System_Double
        };

    private readonly CancellationToken _cancellationToken;
    private readonly Compilation _compilation;
    private readonly Action<Diagnostic> _reportDiagnostic;
    private readonly StringBuilderPool _builders = new();

    public Parser(Compilation compilation, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
    {
        _compilation = compilation;
        _cancellationToken = cancellationToken;
        _reportDiagnostic = reportDiagnostic;
    }

    public IReadOnlyList<MetricType> GetMetricClasses(IEnumerable<TypeDeclarationSyntax> types)
    {
        var symbols = SymbolLoader.LoadSymbols(_compilation);
        if (symbols == null)
        {
            return Array.Empty<MetricType>();
        }

        var results = new List<MetricType>();
        var metricNames = new HashSet<string>();

        foreach (var typeDeclarationGroup in types.GroupBy(x => x.SyntaxTree))
        {
            SemanticModel? semanticModel = null;
            foreach (var typeDeclaration in typeDeclarationGroup)
            {
                // stop if we're asked to
                _cancellationToken.ThrowIfCancellationRequested();

                MetricType? metricType = null;
                string nspace = string.Empty;

                metricNames.Clear();

                foreach (var memberSyntax in typeDeclaration.Members.Where(x => x.IsKind(SyntaxKind.MethodDeclaration)))
                {
                    var methodSyntax = (MethodDeclarationSyntax)memberSyntax;
                    semanticModel ??= _compilation.GetSemanticModel(typeDeclaration.SyntaxTree);
                    IMethodSymbol? methodSymbol = semanticModel.GetDeclaredSymbol(methodSyntax, _cancellationToken);
                    if (methodSymbol == null)
                    {
                        continue;
                    }

                    foreach (var methodAttribute in methodSymbol.GetAttributes())
                    {
                        if (methodAttribute == null)
                        {
                            continue;
                        }

                        var (metricMethod, keepMethod) = ProcessMethodAttribute(typeDeclaration, methodSyntax, methodSymbol, methodAttribute, symbols, metricNames, semanticModel);
                        if (metricMethod == null)
                        {
                            continue;
                        }

                        if (metricType == null)
                        {
                            // determine the namespace the class is declared in, if any
                            SyntaxNode? potentialNamespaceParent = typeDeclaration.Parent;
                            while (potentialNamespaceParent != null &&
                                potentialNamespaceParent is not NamespaceDeclarationSyntax &&
                                potentialNamespaceParent is not FileScopedNamespaceDeclarationSyntax)
                            {
                                potentialNamespaceParent = potentialNamespaceParent.Parent;
                            }

                            var ns = potentialNamespaceParent as BaseNamespaceDeclarationSyntax;
                            if (ns != null)
                            {
                                nspace = ns.Name.ToString();
                                while (true)
                                {
                                    ns = ns.Parent as NamespaceDeclarationSyntax;
                                    if (ns == null)
                                    {
                                        break;
                                    }

                                    nspace = $"{ns.Name}.{nspace}";
                                }
                            }
                        }

                        if (keepMethod)
                        {
                            metricType ??= new MetricType
                            {
                                Namespace = nspace,
                                Name = typeDeclaration.Identifier.ToString() + typeDeclaration.TypeParameterList,
                                Constraints = typeDeclaration.ConstraintClauses.ToString(),
                                Keyword = typeDeclaration.Keyword.ValueText,
                                Parent = null,
                            };

                            UpdateMetricKeywordIfRequired(typeDeclaration, metricType);

                            MetricType currentMetricClass = metricType;
                            var parentMetricClass = typeDeclaration.Parent as TypeDeclarationSyntax;
                            var parentType = methodSymbol.ContainingType.ContainingType;

                            static bool IsAllowedKind(SyntaxKind kind) =>
                                kind is SyntaxKind.ClassDeclaration or
                                    SyntaxKind.StructDeclaration or
                                    SyntaxKind.RecordDeclaration;

                            while (parentMetricClass != null && IsAllowedKind(parentMetricClass.Kind()))
                            {
                                currentMetricClass.Parent = new MetricType
                                {
                                    Namespace = nspace,
                                    Name = parentMetricClass.Identifier.ToString() + parentMetricClass.TypeParameterList,
                                    Constraints = parentMetricClass.ConstraintClauses.ToString(),
                                    Keyword = parentMetricClass.Keyword.ValueText,
                                    Modifiers = parentMetricClass.Modifiers.ToString(),
                                    Parent = null,
                                };

                                UpdateMetricKeywordIfRequired(parentMetricClass, currentMetricClass);

                                currentMetricClass = currentMetricClass.Parent;
                                parentMetricClass = parentMetricClass.Parent as TypeDeclarationSyntax;
                                parentType = parentType.ContainingType;
                            }

                            metricType.Methods.Add(metricMethod);
                        }
                    }
                }

                if (metricType != null)
                {
                    metricType.Modifiers = typeDeclaration.Modifiers.ToString();

                    results.Add(metricType);
                }
            }
        }

        return results;
    }

    private static void UpdateMetricKeywordIfRequired(TypeDeclarationSyntax? typeDeclaration, MetricType metricType)
    {
        if (typeDeclaration.IsKind(SyntaxKind.RecordStructDeclaration) &&
            !metricType.Keyword.Contains("struct"))
        {
            metricType.Keyword += " struct";
        }
    }

    private static bool AreTagNamesValid(MetricMethod metricMethod)
    {
        foreach (string? dynDim in metricMethod.TagKeys)
        {
            if (!_regexTagNames.IsMatch(dynDim))
            {
                return false;
            }
        }

        return true;
    }

    private static ITypeSymbol? GetGenericType(INamedTypeSymbol symbol)
        => symbol.TypeArguments.IsDefaultOrEmpty
            ? null
            : symbol.TypeArguments[0];

    private static (InstrumentKind instrumentKind, ITypeSymbol? genericType) GetInstrumentType(
    INamedTypeSymbol? methodAttributeSymbol,
    SymbolHolder symbols)
    {
        if (methodAttributeSymbol == null)
        {
            return (InstrumentKind.None, null);
        }

        if (methodAttributeSymbol.Equals(symbols.CounterAttribute, SymbolEqualityComparer.Default))
        {
            return (InstrumentKind.Counter, symbols.LongTypeSymbol);
        }

        if (methodAttributeSymbol.Equals(symbols.HistogramAttribute, SymbolEqualityComparer.Default))
        {
            return (InstrumentKind.Histogram, symbols.LongTypeSymbol);
        }

        // Gauge is not supported yet
        if (methodAttributeSymbol.Equals(symbols.GaugeAttribute, SymbolEqualityComparer.Default))
        {
            return (InstrumentKind.Gauge, symbols.LongTypeSymbol);
        }

        if (methodAttributeSymbol.OriginalDefinition.Equals(symbols.CounterOfTAttribute, SymbolEqualityComparer.Default))
        {
            return (InstrumentKind.CounterT, GetGenericType(methodAttributeSymbol));
        }

        if (methodAttributeSymbol.OriginalDefinition.Equals(symbols.HistogramOfTAttribute, SymbolEqualityComparer.Default))
        {
            return (InstrumentKind.HistogramT, GetGenericType(methodAttributeSymbol));
        }

        return (InstrumentKind.None, null);
    }

    private static bool TryGetTagNameFromAttribute(ISymbol symbol, SymbolHolder symbols, out string tagName)
    {
        var attributeData = ParserUtilities.GetSymbolAttributeAnnotationOrDefault(symbols.TagNameAttribute, symbol);

        if (attributeData is not null
            && !attributeData.ConstructorArguments.IsDefaultOrEmpty
            && attributeData.ConstructorArguments[0].Kind == TypedConstantKind.Primitive)
        {
            var ctorArg0 = attributeData.ConstructorArguments[0].Value as string;

            if (!string.IsNullOrWhiteSpace(ctorArg0))
            {
                tagName = ctorArg0!;
                return true;
            }
        }

        tagName = string.Empty;
        return false;
    }

    private (string metricName, HashSet<string> tagNames, Dictionary<string, string> dimensionDescriptions) ExtractAttributeParameters(
        AttributeData attribute,
        SemanticModel semanticModel)
    {
        var tagHashSet = new HashSet<string>();
        var tagDescriptionMap = new Dictionary<string, string>();
        string metricNameFromAttribute = string.Empty;
        if (!attribute.NamedArguments.IsDefaultOrEmpty)
        {
            foreach (var arg in attribute.NamedArguments)
            {
                if (arg.Value.Kind == TypedConstantKind.Primitive &&
                    arg.Key is "MetricName" or "Name")
                {
                    metricNameFromAttribute = (arg.Value.Value ?? string.Empty).ToString().Replace("\\\\", "\\");
                    break;
                }
            }
        }

        if (!attribute.ConstructorArguments.IsDefaultOrEmpty)
        {
            foreach (var arg in attribute.ConstructorArguments)
            {
                if (arg.Kind != TypedConstantKind.Array)
                {
                    continue;
                }

                foreach (var item in arg.Values)
                {
                    if (item.Kind != TypedConstantKind.Primitive)
                    {
                        continue;
                    }

                    var value = item.Value?.ToString();
                    if (value == null)
                    {
                        continue;
                    }

                    _ = tagHashSet.Add(value);
                }
            }
        }

        if (attribute.ApplicationSyntaxReference != null &&
            attribute.ApplicationSyntaxReference.GetSyntax(_cancellationToken) is AttributeSyntax syntax &&
            syntax.ArgumentList != null)
        {
            foreach (var arg in syntax.ArgumentList.Arguments)
            {
                GetTagDescription(arg, semanticModel, tagDescriptionMap);
            }
        }

        return (metricNameFromAttribute, tagHashSet, tagDescriptionMap);
    }

    private string GetSymbolXmlCommentSummary(ISymbol symbol)
    {
        var xmlComment = symbol.GetDocumentationCommentXml();
        if (string.IsNullOrEmpty(xmlComment))
        {
            return string.Empty;
        }

        try
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlComment);
            var summaryNode = xmlDoc.SelectSingleNode("//summary");
            if (summaryNode != null)
            {
                var summaryString = summaryNode.InnerXml.Trim();
                return summaryString;
            }
            else
            {
                return string.Empty;
            }
        }
        catch (XmlException ex)
        {
            Diag(DiagDescriptors.ErrorXmlNotLoadedCorrectly, symbol.GetLocation(), ex.Message);
            return string.Empty;
        }
    }

    private void GetTagDescription(
        AttributeArgumentSyntax arg,
        SemanticModel semanticModel,
        Dictionary<string, string> tagDescriptionDictionary)
    {
        if (arg.NameEquals != null)
        {
            return;
        }

        var symbol = semanticModel.GetSymbolInfo(arg.Expression, _cancellationToken).Symbol;
        if (symbol is not IFieldSymbol fieldSymbol ||
            !fieldSymbol.HasConstantValue ||
            fieldSymbol.ConstantValue == null)
        {
            return;
        }

        var xmlDefinition = GetSymbolXmlCommentSummary(symbol);
        if (!string.IsNullOrEmpty(xmlDefinition))
        {
            tagDescriptionDictionary.Add(fieldSymbol.ConstantValue.ToString(), xmlDefinition);
        }
    }

    private (MetricMethod? metricMethod, bool keepMethod) ProcessMethodAttribute(
        TypeDeclarationSyntax typeDeclaration,
        MethodDeclarationSyntax methodSyntax,
        IMethodSymbol methodSymbol,
        AttributeData methodAttribute,
        SymbolHolder symbols,
        HashSet<string> metricNames,
        SemanticModel semanticModel)
    {
        var (instrumentKind, genericType) = GetInstrumentType(methodAttribute.AttributeClass, symbols);
        if (instrumentKind == InstrumentKind.None ||
            genericType == null)
        {
            return (null, false);
        }

        if (instrumentKind == InstrumentKind.Gauge)
        {
            Diag(DiagDescriptors.ErrorGaugeNotSupported, methodSymbol.GetLocation());
            return (null, false);
        }

        bool keepMethod = CheckMethodReturnType(methodSymbol);
        if (!_allowedGenericAttributeTypeArgs.Contains(genericType.SpecialType))
        {
            Diag(DiagDescriptors.ErrorInvalidAttributeGenericType, methodSymbol.GetLocation(), genericType.ToString());
            keepMethod = false;
        }

        var strongTypeAttrParams = new StrongTypeAttributeParameters();

        if (!methodAttribute.ConstructorArguments.IsDefaultOrEmpty
            && methodAttribute.ConstructorArguments[0].Kind == TypedConstantKind.Type)
        {
            KeyValuePair<string, TypedConstant> namedArg;
            var ctorArg = methodAttribute.ConstructorArguments[0];

            if (!methodAttribute.NamedArguments.IsDefaultOrEmpty)
            {
                namedArg = methodAttribute.NamedArguments[0];
            }

            strongTypeAttrParams = ExtractStrongTypeAttributeParameters(ctorArg, namedArg, symbols);
        }
        else
        {
            var parameters = ExtractAttributeParameters(methodAttribute, semanticModel);
            (strongTypeAttrParams.MetricNameFromAttribute, strongTypeAttrParams.TagHashSet, strongTypeAttrParams.TagDescriptionDictionary) = parameters;
        }

        string metricNameFromMethod = methodSymbol.ReturnType.Name;

        var metricMethod = new MetricMethod
        {
            Name = methodSymbol.Name,
            MetricName = string.IsNullOrWhiteSpace(strongTypeAttrParams.MetricNameFromAttribute) ? metricNameFromMethod : strongTypeAttrParams.MetricNameFromAttribute,
            InstrumentKind = instrumentKind,
            GenericType = genericType.ToDisplayString(_genericTypeSymbolFormat),
            TagKeys = strongTypeAttrParams.TagHashSet,
            IsExtensionMethod = methodSymbol.IsExtensionMethod,
            Modifiers = methodSyntax.Modifiers.ToString(),
            MetricTypeName = methodSymbol.ReturnType.ToDisplayString(), // Roslyn doesn't know this type yet, no need to use a format here
            StrongTypeConfigs = strongTypeAttrParams.StrongTypeConfigs,
            StrongTypeObjectName = strongTypeAttrParams.StrongTypeObjectName,
            IsTagTypeClass = strongTypeAttrParams.IsClass,
            MetricTypeModifiers = typeDeclaration.Modifiers.ToString(),
            TagDescriptionDictionary = strongTypeAttrParams.TagDescriptionDictionary
        };

        var xmlDefinition = GetSymbolXmlCommentSummary(methodSymbol);
        if (!string.IsNullOrEmpty(xmlDefinition))
        {
            metricMethod.XmlDefinition = xmlDefinition;
        }

        if (metricMethod.Name[0] == '_')
        {
            // can't have logging method names that start with _ since that can lead to conflicting symbol names
            // because the generated symbols start with _
            Diag(DiagDescriptors.ErrorInvalidMethodName, methodSymbol.GetLocation());
            keepMethod = false;
        }

        if (methodSymbol.Arity > 0)
        {
            // we don't currently support generic methods
            Diag(DiagDescriptors.ErrorMethodIsGeneric, methodSymbol.GetLocation());
            keepMethod = false;
        }

        bool isStatic = methodSymbol.IsStatic;
        bool isPartial = methodSymbol.IsPartialDefinition;

        if (!isStatic)
        {
            Diag(DiagDescriptors.ErrorNotStaticMethod, methodSymbol.GetLocation());
            keepMethod = false;
        }

        if (methodSyntax.Body != null)
        {
            Diag(DiagDescriptors.ErrorMethodHasBody, methodSymbol.GetLocation());
            keepMethod = false;
        }
#pragma warning disable S2583 // Conditionally executed code should be reachable
        else if (!isPartial)
        {
            Diag(DiagDescriptors.ErrorNotPartialMethod, methodSymbol.GetLocation());
            keepMethod = false;
        }
#pragma warning restore S2583 // Conditionally executed code should be reachable

        // ensure Metric name is not empty and starts with a Capital letter.
        // ensure there are no duplicate ids.
        if (!_regex.IsMatch(metricNameFromMethod))
        {
            Diag(DiagDescriptors.ErrorInvalidMetricName, methodSymbol.GetLocation(), metricNameFromMethod);
            keepMethod = false;
        }
        else if (!metricNames.Add(metricNameFromMethod))
        {
            Diag(DiagDescriptors.ErrorMetricNameReuse, methodSymbol.GetLocation(), metricNameFromMethod);
            keepMethod = false;
        }

        if (!AreTagNamesValid(metricMethod))
        {
            Diag(DiagDescriptors.ErrorInvalidTagNames, methodSymbol.GetLocation());
            keepMethod = false;
        }

        bool isFirstParam = true;
        foreach (var paramSymbol in methodSymbol.Parameters)
        {
            var paramName = paramSymbol.Name;
            if (string.IsNullOrWhiteSpace(paramName))
            {
                // semantic problem, just bail quietly
                keepMethod = false;
                break;
            }

            var paramTypeSymbol = paramSymbol.Type;
            if (paramTypeSymbol is IErrorTypeSymbol)
            {
                // semantic problem, just bail quietly
                keepMethod = false;
                break;
            }

            var meterParameter = new MetricParameter
            {
                Name = paramName,
                Type = paramTypeSymbol.ToDisplayString(_typeSymbolFormat),
                IsMeter = isFirstParam && ParserUtilities.IsBaseOrIdentity(paramTypeSymbol, symbols.MeterSymbol, _compilation)
            };

            if (meterParameter.Name[0] == '_')
            {
                // can't have method parameter names that start with _ since that can lead to conflicting symbol names
                // because all generated symbols start with _
                Diag(DiagDescriptors.ErrorInvalidParameterName, paramSymbol.Locations[0]);
            }

            metricMethod.AllParameters.Add(meterParameter);
            isFirstParam = false;
        }

        if (keepMethod)
        {
            if (metricMethod.AllParameters.Count < 1 ||
                !metricMethod.AllParameters[0].IsMeter)
            {
                Diag(DiagDescriptors.ErrorMissingMeter, methodSymbol.GetLocation());
                keepMethod = false;
            }
        }

        return (metricMethod, keepMethod);
    }

    private bool CheckMethodReturnType(IMethodSymbol methodSymbol)
    {
        var returnType = methodSymbol.ReturnType;
        if (returnType.SpecialType != SpecialType.None ||
            returnType.TypeKind != TypeKind.Error)
        {
            // Make sure return type is not from existing known type
            Diag(DiagDescriptors.ErrorInvalidMethodReturnType, methodSymbol.ReturnType.GetLocation(), methodSymbol.Name);
            return false;
        }

        if (returnType is INamedTypeSymbol { Arity: > 0 })
        {
            Diag(DiagDescriptors.ErrorInvalidMethodReturnTypeArity, methodSymbol.GetLocation(), methodSymbol.Name, returnType.Name);
            return false;
        }

        if (!string.Equals(returnType.Name, returnType.ToString(), StringComparison.Ordinal))
        {
            Diag(DiagDescriptors.ErrorInvalidMethodReturnTypeLocation, methodSymbol.GetLocation(), methodSymbol.Name, returnType.Name);
            return false;
        }

        return true;
    }

    private void Diag(DiagnosticDescriptor desc, Location? location)
    {
        _reportDiagnostic(Diagnostic.Create(desc, location, Array.Empty<object?>()));
    }

    private void Diag(DiagnosticDescriptor desc, Location? location, params object?[]? messageArgs)
    {
        _reportDiagnostic(Diagnostic.Create(desc, location, messageArgs));
    }

    private StrongTypeAttributeParameters ExtractStrongTypeAttributeParameters(
        TypedConstant constructorArg,
        KeyValuePair<string, TypedConstant> namedArgument,
        SymbolHolder symbols)
    {
        var strongTypeAttributeParameters = new StrongTypeAttributeParameters();

        if (namedArgument is { Key: "Name", Value.Value: { } })
        {
            strongTypeAttributeParameters.MetricNameFromAttribute = namedArgument.Value.Value.ToString();
        }

        if (constructorArg.IsNull ||
            constructorArg.Value is not INamedTypeSymbol strongTypeSymbol)
        {
            return strongTypeAttributeParameters;
        }

        // Need to check if the strongType is a class or struct, classes need a null check whereas structs do not.
        if (strongTypeSymbol.TypeKind == TypeKind.Class)
        {
            strongTypeAttributeParameters.IsClass = true;
        }

        try
        {
            var typesChain = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
            _ = typesChain.Add(strongTypeSymbol); // Add itself

            // Loop through all of the members of the object level and below
            foreach (var member in strongTypeSymbol.GetMembers())
            {
                var tagConfigs = BuildTagConfigs(member, typesChain, strongTypeAttributeParameters.TagHashSet,
                    strongTypeAttributeParameters.TagDescriptionDictionary, symbols, _builders.GetStringBuilder());

                strongTypeAttributeParameters.StrongTypeConfigs.AddRange(tagConfigs);
            }

            // Now that all of the current level and below dimensions are extracted, let's get any parent ones
            strongTypeAttributeParameters.StrongTypeConfigs.AddRange(GetParentTagConfigs(strongTypeSymbol,
                strongTypeAttributeParameters.TagHashSet, strongTypeAttributeParameters.TagDescriptionDictionary, symbols));
        }
        catch (TransitiveTypeCycleException ex)
        {
            Diag(DiagDescriptors.ErrorTagTypeCycleDetected,
                strongTypeSymbol.Locations[0],
                strongTypeSymbol.ToDisplayString(),
                ex.Parent.ToDisplayString(),
                ex.NamedType.ToDisplayString());
        }

        if (strongTypeAttributeParameters.StrongTypeConfigs.Count > MaxTagNames)
        {
            Diag(DiagDescriptors.ErrorTooManyTagNames, strongTypeSymbol.Locations[0]);
        }

        strongTypeAttributeParameters.StrongTypeObjectName = constructorArg.Value.ToString();
        return strongTypeAttributeParameters;
    }

    /// <summary>
    /// Called recursively to build all required StrongTypeDimensionConfigs.
    /// </summary>
    /// <param name="symbol">The Symbol being extracted.</param>
    /// <param name="typesChain">A set of symbols in the current type chain.</param>
    /// <param name="tagHashSet">HashSet of all dimensions seen so far.</param>
    /// <param name="symbols">Shared symbols.</param>
    /// <param name="stringBuilder">List of all property names when walking down the object model. See StrongTypeDimensionConfigs for an example.</param>
    /// <returns>List of all StrongTypeDimensionConfigs seen so far.</returns>
    private List<StrongTypeConfig> BuildTagConfigs(
        ISymbol symbol,
        ISet<ITypeSymbol> typesChain,
        HashSet<string> tagHashSet,
        Dictionary<string, string> tagDescriptionDictionary,
        SymbolHolder symbols,
        StringBuilder stringBuilder)
    {
        var tagConfigs = new List<StrongTypeConfig>();

        TypeKind kind;
        SpecialType specialType;
        ITypeSymbol typeSymbol;

        if (symbol.IsImplicitlyDeclared)
        {
            return tagConfigs;
        }

        switch (symbol.Kind)
        {
            case SymbolKind.Property:
                var propertySymbol = symbol as IPropertySymbol;

                kind = propertySymbol!.Type.TypeKind;
                specialType = propertySymbol.Type.SpecialType;
                typeSymbol = propertySymbol.Type;
                break;

            case SymbolKind.Field:
                var fieldSymbol = symbol as IFieldSymbol;

                kind = fieldSymbol!.Type.TypeKind;
                specialType = fieldSymbol.Type.SpecialType;
                typeSymbol = fieldSymbol.Type;
                break;

            default:
                _builders.ReturnStringBuilder(stringBuilder);
                return tagConfigs;
        }

        // This one is to properly cover "Nullable<T>" cases:
        if (specialType == SpecialType.None)
        {
            specialType = typeSymbol.OriginalDefinition.SpecialType;
        }

        try
        {
            if (kind == TypeKind.Enum)
            {
                var name = TryGetTagNameFromAttribute(symbol, symbols, out var tagName)
                    ? tagName
                    : symbol.Name;

                if (!tagHashSet.Add(name))
                {
                    Diag(DiagDescriptors.ErrorDuplicateTagName, symbol.Locations[0], symbol.Name);
                }
                else
                {
                    tagConfigs.Add(new StrongTypeConfig
                    {
                        Name = symbol.Name,
                        Path = stringBuilder.ToString(),
                        TagName = name,
                        StrongTypeMetricObjectType = StrongTypeMetricObjectType.Enum
                    });

                    var xmlDefinition = GetSymbolXmlCommentSummary(symbol);
                    if (!string.IsNullOrEmpty(xmlDefinition))
                    {
                        tagDescriptionDictionary.Add(string.IsNullOrEmpty(tagName) ? symbol.Name : tagName, xmlDefinition);
                    }
                }

                return tagConfigs;
            }

            if (kind == TypeKind.Class)
            {
                if (specialType == SpecialType.System_String)
                {
                    var name = TryGetTagNameFromAttribute(symbol, symbols, out var tagName)
                        ? tagName
                        : symbol.Name;

                    if (!tagHashSet.Add(name))
                    {
                        Diag(DiagDescriptors.ErrorDuplicateTagName, symbol.Locations[0], symbol.Name);
                    }
                    else
                    {
                        tagConfigs.Add(new StrongTypeConfig
                        {
                            Name = symbol.Name,
                            Path = stringBuilder.ToString(),
                            TagName = name,
                            StrongTypeMetricObjectType = StrongTypeMetricObjectType.String
                        });

                        var xmlDefinition = GetSymbolXmlCommentSummary(symbol);
                        if (!string.IsNullOrEmpty(xmlDefinition))
                        {
                            tagDescriptionDictionary.Add(string.IsNullOrEmpty(tagName) ? symbol.Name : tagName, xmlDefinition);
                        }
                    }

                    return tagConfigs;
                }
                else if (specialType == SpecialType.None)
                {
                    if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
                    {
                        // User defined class, first add into dimensionConfigs, then walk the object model

                        tagConfigs.Add(new StrongTypeConfig
                        {
                            Name = symbol.Name,
                            Path = stringBuilder.ToString(),
                            StrongTypeMetricObjectType = StrongTypeMetricObjectType.Class
                        });

                        tagConfigs.AddRange(
                            WalkObjectModel(symbol, typesChain, namedTypeSymbol, stringBuilder,
                                tagHashSet, tagDescriptionDictionary, symbols, true));

                        return tagConfigs;
                    }
                }
                else
                {
                    Diag(DiagDescriptors.ErrorInvalidTagNameType, symbol.Locations[0]);
                    return tagConfigs;
                }
            }

            if (kind == TypeKind.Struct && specialType == SpecialType.None)
            {
                if (typeSymbol is not INamedTypeSymbol namedTypeSymbol)
                {
                    Diag(DiagDescriptors.ErrorInvalidTagNameType, symbol.Locations[0]);
                }
                else
                {
                    // User defined struct. First add into dimensionConfigs, then walk down the rest of the struct.
                    tagConfigs.Add(new StrongTypeConfig
                    {
                        Name = symbol.Name,
                        Path = stringBuilder.ToString(),
                        StrongTypeMetricObjectType = StrongTypeMetricObjectType.Struct
                    });

                    tagConfigs.AddRange(
                        WalkObjectModel(symbol, typesChain, namedTypeSymbol, stringBuilder,
                            tagHashSet, tagDescriptionDictionary, symbols, false));
                }

                return tagConfigs;
            }
            else
            {
                Diag(DiagDescriptors.ErrorInvalidTagNameType, symbol.Locations[0]);
                return tagConfigs;
            }
        }
        finally
        {
            _builders.ReturnStringBuilder(stringBuilder);
        }
    }
#pragma warning disable S107 // Methods should not have too many parameters

    // we can deal with this warning later
    private List<StrongTypeConfig> WalkObjectModel(
        ISymbol parentSymbol,
        ISet<ITypeSymbol> typesChain,
        INamedTypeSymbol namedTypeSymbol,
        StringBuilder stringBuilder,
        HashSet<string> tagHashSet,
        Dictionary<string, string> tagDescriptionDictionary,
        SymbolHolder symbols,
        bool isClass)
#pragma warning restore S107 // Methods should not have too many parameters
    {
        var tagConfigs = new List<StrongTypeConfig>();

        if (stringBuilder.Length != 0)
        {
            _ = stringBuilder.Append('.');
        }

        _ = stringBuilder.Append(parentSymbol.Name);

        if (isClass)
        {
            _ = stringBuilder.Append('?');
        }

        if (!typesChain.Add(namedTypeSymbol))
        {
            throw new TransitiveTypeCycleException(parentSymbol.ContainingSymbol, namedTypeSymbol); // Interrupt the whole traversal
        }

        foreach (var member in namedTypeSymbol.GetMembers())
        {
            tagConfigs.AddRange(
                BuildTagConfigs(member, typesChain, tagHashSet,
                    tagDescriptionDictionary, symbols, stringBuilder));
        }

        _ = typesChain.Remove(namedTypeSymbol);

        return tagConfigs;
    }

    private List<StrongTypeConfig> GetParentTagConfigs(
        ITypeSymbol symbol,
        HashSet<string> tagHashSet,
        Dictionary<string, string> tagDescriptionDictionary,
        SymbolHolder symbols)
    {
        var tagConfigs = new List<StrongTypeConfig>();
        INamedTypeSymbol? parentObjectBase = symbol.BaseType;

        do
        {
            if (parentObjectBase == null)
            {
                continue;
            }

            var typesChain = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
            _ = typesChain.Add(parentObjectBase); // Add itself

            foreach (var member in parentObjectBase.GetMembers())
            {
                tagConfigs.AddRange(
                    BuildTagConfigs(member, typesChain, tagHashSet,
                        tagDescriptionDictionary, symbols, _builders.GetStringBuilder()));
            }

            parentObjectBase = parentObjectBase.BaseType;
        }
        while (parentObjectBase?.BaseType != null);

        return tagConfigs;
    }
}
