// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Gen.Metering.Model;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.Metering;

internal sealed class Parser
{
    private const int MaxDimensions = 20;

    private static readonly Regex _regex = new("^[A-Z]+[A-za-z0-9]*$", RegexOptions.Compiled);
    private static readonly Regex _regexDimensionNames = new("^[A-Za-z]+[A-Za-z0-9_.:-]*$", RegexOptions.Compiled);
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

                        var (metricMethod, keepMethod) = ProcessMethodAttribute(typeDeclaration, methodSyntax, methodSymbol, methodAttribute, symbols, metricNames);
                        if (metricMethod == null)
                        {
                            continue;
                        }

                        if (metricType == null)
                        {
                            // determine the namespace the class is declared in, if any
                            SyntaxNode? potentialNamespaceParent = typeDeclaration.Parent;
                            while (potentialNamespaceParent != null &&
#if ROSLYN_4_0_OR_GREATER
                                potentialNamespaceParent is not NamespaceDeclarationSyntax &&
                                potentialNamespaceParent is not FileScopedNamespaceDeclarationSyntax)
#else
                                        potentialNamespaceParent is not NamespaceDeclarationSyntax)
#endif
                            {
                                potentialNamespaceParent = potentialNamespaceParent.Parent;
                            }

#if ROSLYN_4_0_OR_GREATER
                            var ns = potentialNamespaceParent as BaseNamespaceDeclarationSyntax;
#else
                            var ns = potentialNamespaceParent as NamespaceDeclarationSyntax;
#endif

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
#if ROSLYN_4_0_OR_GREATER
        if (typeDeclaration.IsKind(SyntaxKind.RecordStructDeclaration) &&
            !metricType.Keyword.Contains("struct"))
        {
            metricType.Keyword += " struct";
        }
#endif
    }

    private static bool AreDimensionKeyNamesValid(MetricMethod metricMethod)
    {
        foreach (string? dynDim in metricMethod.DimensionsKeys)
        {
            if (!_regexDimensionNames.IsMatch(dynDim))
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

    private static bool TryGetDimensionNameFromAttribute(ISymbol symbol, SymbolHolder symbols, out string dimensionName)
    {
        var attributeData = ParserUtilities.GetSymbolAttributeAnnotationOrDefault(symbols.DimensionAttribute, symbol);

        if (attributeData is not null
            && !attributeData.ConstructorArguments.IsDefaultOrEmpty
            && attributeData.ConstructorArguments[0].Kind == TypedConstantKind.Primitive)
        {
            var ctorArg0 = attributeData.ConstructorArguments[0].Value as string;

            if (!string.IsNullOrWhiteSpace(ctorArg0))
            {
                dimensionName = ctorArg0!;
                return true;
            }
        }

        dimensionName = string.Empty;
        return false;
    }

    private static (string metricName, HashSet<string> dimensions) ExtractAttributeParameters(AttributeData attribute)
    {
        var dimensionHashSet = new HashSet<string>();
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

                    _ = dimensionHashSet.Add(value);
                }
            }
        }

        return (metricNameFromAttribute, dimensionHashSet);
    }

    private (MetricMethod? metricMethod, bool keepMethod) ProcessMethodAttribute(
        TypeDeclarationSyntax typeDeclaration,
        MethodDeclarationSyntax methodSyntax,
        IMethodSymbol methodSymbol,
        AttributeData methodAttribute,
        SymbolHolder symbols,
        HashSet<string> metricNames)
    {
        var (instrumentKind, genericType) = GetInstrumentType(methodAttribute.AttributeClass, symbols);
        if (instrumentKind == InstrumentKind.None ||
            genericType == null)
        {
            return (null, false);
        }

        bool keepMethod = CheckMethodReturnType(methodSymbol);
        if (!_allowedGenericAttributeTypeArgs.Contains(genericType.SpecialType))
        {
            Diag(DiagDescriptors.ErrorInvalidAttributeGenericType, methodSymbol.GetLocation(), genericType.ToString());
            keepMethod = false;
        }

        string metricNameFromAttribute;
        HashSet<string> dimensions;
        var strongTypeDimensionConfigs = new List<StrongTypeConfig>();
        var strongTypeObjectName = string.Empty;
        var isClass = false;

        if (!methodAttribute.ConstructorArguments.IsDefaultOrEmpty
            && methodAttribute.ConstructorArguments[0].Kind == TypedConstantKind.Type)
        {
            KeyValuePair<string, TypedConstant> namedArg;
            var ctorArg = methodAttribute.ConstructorArguments[0];

            if (!methodAttribute.NamedArguments.IsDefaultOrEmpty)
            {
                namedArg = methodAttribute.NamedArguments[0];
            }

            (metricNameFromAttribute, dimensions, strongTypeDimensionConfigs, strongTypeObjectName, isClass) = ExtractStrongTypeAttributeParameters(
                ctorArg,
                namedArg,
                symbols);
        }
        else
        {
            (metricNameFromAttribute, dimensions) = ExtractAttributeParameters(methodAttribute);
        }

        string metricNameFromMethod = methodSymbol.ReturnType.Name;

        var metricMethod = new MetricMethod
        {
            Name = methodSymbol.Name,
            MetricName = string.IsNullOrWhiteSpace(metricNameFromAttribute) ? metricNameFromMethod : metricNameFromAttribute,
            InstrumentKind = instrumentKind,
            GenericType = genericType.ToDisplayString(_genericTypeSymbolFormat),
            DimensionsKeys = dimensions,
            IsExtensionMethod = methodSymbol.IsExtensionMethod,
            Modifiers = methodSyntax.Modifiers.ToString(),
            MetricTypeName = methodSymbol.ReturnType.ToDisplayString(), // Roslyn doesn't know this type yet, no need to use a format here
            StrongTypeConfigs = strongTypeDimensionConfigs,
            StrongTypeObjectName = strongTypeObjectName,
            IsDimensionTypeClass = isClass,
            MetricTypeModifiers = typeDeclaration.Modifiers.ToString()
        };

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
#if ROSLYN_4_0_OR_GREATER
        bool isPartial = methodSymbol.IsPartialDefinition;
#else
        bool isPartial = true;  // don't check for this condition on older versions of Roslyn since IsPartialDefinition doesn't exist
#endif

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

        if (!AreDimensionKeyNamesValid(metricMethod))
        {
            Diag(DiagDescriptors.ErrorInvalidDimensionNames, methodSymbol.GetLocation());
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

            if (isFirstParam &&
                symbols.IMeterInterface != null &&
                ParserUtilities.IsBaseOrIdentity(paramTypeSymbol, symbols.IMeterInterface, _compilation))
            {
                // The method uses old IMeter, no need to parse it
                return (null, false);
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

    private (string metricName, HashSet<string> dimensions, List<StrongTypeConfig> strongTypeConfigs, string strongTypeObjectName, bool isClass)
        ExtractStrongTypeAttributeParameters(
            TypedConstant constructorArg,
            KeyValuePair<string, TypedConstant> namedArgument,
            SymbolHolder symbols)
    {
        var dimensionHashSet = new HashSet<string>();
        string metricNameFromAttribute = string.Empty;
        var strongTypeConfigs = new List<StrongTypeConfig>();
        bool isClass = false;

        if (namedArgument is { Key: "Name", Value.Value: { } })
        {
            metricNameFromAttribute = namedArgument.Value.Value.ToString();
        }

        if (constructorArg.IsNull ||
            constructorArg.Value is not INamedTypeSymbol strongTypeSymbol)
        {
            return (metricNameFromAttribute, dimensionHashSet, strongTypeConfigs, string.Empty, isClass);
        }

        // Need to check if the strongType is a class or struct, classes need a null check whereas structs do not.
        if (strongTypeSymbol.TypeKind == TypeKind.Class)
        {
            isClass = true;
        }

        // Loop through all of the members of the object level and below
        foreach (var member in strongTypeSymbol.GetMembers())
        {
            strongTypeConfigs.AddRange(BuildDimensionConfigs(member, dimensionHashSet, symbols, _builders.GetStringBuilder()));
        }

        // Now that all of the current level and below dimensions are extracted, let's get any parent ones
        strongTypeConfigs.AddRange(GetParentDimensionConfigs(strongTypeSymbol, dimensionHashSet, symbols));

        if (strongTypeConfigs.Count > MaxDimensions)
        {
            Diag(DiagDescriptors.ErrorTooManyDimensions, strongTypeSymbol.Locations[0]);
        }

        return (metricNameFromAttribute, dimensionHashSet, strongTypeConfigs, constructorArg.Value.ToString(), isClass);
    }

    /// <summary>
    /// Called recursively to build all required StrongTypeDimensionConfigs.
    /// </summary>
    /// <param name="symbol">The Symbol being extracted.</param>
    /// <param name="dimensionHashSet">HashSet of all dimensions seen so far.</param>
    /// <param name="symbols">Shared symbols.</param>
    /// <param name="stringBuilder">List of all property names when walking down the object model. See StrongTypeDimensionConfigs for an example.</param>
    /// <returns>List of all StrongTypeDimensionConfigs seen so far.</returns>
    private List<StrongTypeConfig> BuildDimensionConfigs(
        ISymbol symbol,
        HashSet<string> dimensionHashSet,
        SymbolHolder symbols,
        StringBuilder stringBuilder)
    {
        var dimensionConfigs = new List<StrongTypeConfig>();

        TypeKind kind;
        SpecialType specialType;
        ITypeSymbol typeSymbol;

        if (symbol.IsImplicitlyDeclared)
        {
            return dimensionConfigs;
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
                return dimensionConfigs;
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
                var name = TryGetDimensionNameFromAttribute(symbol, symbols, out var dimensionName)
                    ? dimensionName
                    : symbol.Name;

                if (!dimensionHashSet.Add(name))
                {
                    Diag(DiagDescriptors.ErrorDuplicateDimensionName, symbol.Locations[0], symbol.Name);
                }
                else
                {
                    dimensionConfigs.Add(new StrongTypeConfig
                    {
                        Name = symbol.Name,
                        Path = stringBuilder.ToString(),
                        DimensionName = name,
                        StrongTypeMetricObjectType = StrongTypeMetricObjectType.Enum
                    });
                }

                return dimensionConfigs;
            }

            if (kind == TypeKind.Class)
            {
                if (specialType == SpecialType.System_String)
                {
                    var name = TryGetDimensionNameFromAttribute(symbol, symbols, out var dimensionName)
                        ? dimensionName
                        : symbol.Name;

                    if (!dimensionHashSet.Add(name))
                    {
                        Diag(DiagDescriptors.ErrorDuplicateDimensionName, symbol.Locations[0], symbol.Name);
                    }
                    else
                    {
                        dimensionConfigs.Add(new StrongTypeConfig
                        {
                            Name = symbol.Name,
                            Path = stringBuilder.ToString(),
                            DimensionName = name,
                            StrongTypeMetricObjectType = StrongTypeMetricObjectType.String
                        });
                    }

                    return dimensionConfigs;
                }
                else if (specialType == SpecialType.None)
                {
                    if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
                    {
                        // User defined class, first add into dimensionConfigs, then walk the object model

                        dimensionConfigs.Add(new StrongTypeConfig
                        {
                            Name = symbol.Name,
                            Path = stringBuilder.ToString(),
                            StrongTypeMetricObjectType = StrongTypeMetricObjectType.Class
                        });

                        dimensionConfigs.AddRange(
                            WalkObjectModel(
                                symbol,
                                namedTypeSymbol,
                                stringBuilder,
                                dimensionHashSet,
                                symbols,
                                true));

                        return dimensionConfigs;
                    }
                }
                else
                {
                    Diag(DiagDescriptors.ErrorInvalidDimensionType, symbol.Locations[0]);
                    return dimensionConfigs;
                }
            }

            if (kind == TypeKind.Struct && specialType == SpecialType.None)
            {
                if (typeSymbol is not INamedTypeSymbol namedTypeSymbol)
                {
                    Diag(DiagDescriptors.ErrorInvalidDimensionType, symbol.Locations[0]);
                }
                else
                {
                    // User defined struct. First add into dimensionConfigs, then walk down the rest of the struct.
                    dimensionConfigs.Add(new StrongTypeConfig
                    {
                        Name = symbol.Name,
                        Path = stringBuilder.ToString(),
                        StrongTypeMetricObjectType = StrongTypeMetricObjectType.Struct
                    });

                    dimensionConfigs.AddRange(
                        WalkObjectModel(
                            symbol,
                            namedTypeSymbol,
                            stringBuilder,
                            dimensionHashSet,
                            symbols,
                            false));
                }

                return dimensionConfigs;
            }
            else
            {
                Diag(DiagDescriptors.ErrorInvalidDimensionType, symbol.Locations[0]);
                return dimensionConfigs;
            }
        }
        finally
        {
            _builders.ReturnStringBuilder(stringBuilder);
        }
    }

    private List<StrongTypeConfig> WalkObjectModel(
        ISymbol parentSymbol,
        INamedTypeSymbol namedTypeSymbol,
        StringBuilder stringBuilder,
        HashSet<string> dimensionHashSet,
        SymbolHolder symbols,
        bool isClass)
    {
        var dimensionConfigs = new List<StrongTypeConfig>();

        if (stringBuilder.Length != 0)
        {
            _ = stringBuilder.Append('.');
        }

        _ = stringBuilder.Append(parentSymbol.Name);

        if (isClass)
        {
            _ = stringBuilder.Append('?');
        }

        foreach (var member in namedTypeSymbol.GetMembers())
        {
            dimensionConfigs.AddRange(BuildDimensionConfigs(member, dimensionHashSet, symbols, stringBuilder));
        }

        return dimensionConfigs;
    }

    private List<StrongTypeConfig> GetParentDimensionConfigs(
        ITypeSymbol symbol,
        HashSet<string> dimensionHashSet,
        SymbolHolder symbols)
    {
        var dimensionConfigs = new List<StrongTypeConfig>();
        INamedTypeSymbol? parentObjectBase = symbol.BaseType;

        do
        {
            if (parentObjectBase == null)
            {
                continue;
            }

            foreach (var member in parentObjectBase.GetMembers())
            {
                dimensionConfigs.AddRange(BuildDimensionConfigs(member, dimensionHashSet, symbols, _builders.GetStringBuilder()));
            }

            parentObjectBase = parentObjectBase.BaseType;
        }
        while (parentObjectBase?.BaseType != null);

        return dimensionConfigs;
    }
}
