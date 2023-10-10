// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.LocalAnalyzers.ApiLifecycle.Model;
using Microsoft.Extensions.LocalAnalyzers.Utilities;

namespace Microsoft.Extensions.LocalAnalyzers.ApiLifecycle;

internal sealed class AssemblyAnalysis
{
    public Assembly Assembly { get; }
    public HashSet<TypeDef> MissingTypes { get; } = [];
    public Dictionary<ISymbol, List<string>> MissingConstraints { get; } = new(SymbolEqualityComparer.Default);
    public Dictionary<ISymbol, List<string>> MissingBaseTypes { get; } = new(SymbolEqualityComparer.Default);
    public HashSet<Method> MissingMethods { get; } = [];
    public HashSet<Prop> MissingProperties { get; } = [];
    public HashSet<Field> MissingFields { get; } = [];
    public HashSet<(ISymbol symbol, Stage stage)> FoundInBaseline { get; } = [];
    public HashSet<ISymbol> NotFoundInBaseline { get; } = new(SymbolEqualityComparer.Default);

#pragma warning disable SA1118 // Parameter should not span multiple lines
    private static readonly SymbolDisplayFormat _format =
            new(globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters |
                                 SymbolDisplayGenericsOptions.IncludeTypeConstraints |
                                 SymbolDisplayGenericsOptions.IncludeVariance,
                kindOptions: SymbolDisplayKindOptions.IncludeNamespaceKeyword |
                             SymbolDisplayKindOptions.IncludeTypeKeyword |
                             SymbolDisplayKindOptions.IncludeMemberKeyword,
                parameterOptions:
                            SymbolDisplayParameterOptions.IncludeExtensionThis |
                            SymbolDisplayParameterOptions.IncludeParamsRefOut |
                            SymbolDisplayParameterOptions.IncludeType |
                            SymbolDisplayParameterOptions.IncludeName |
                            SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                                      SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier,
                delegateStyle: SymbolDisplayDelegateStyle.NameAndSignature);

    private static readonly SymbolDisplayFormat _formatNoVariance =
            new(globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
                memberOptions: SymbolDisplayMemberOptions.IncludeType |
                               SymbolDisplayMemberOptions.IncludeModifiers |
                               SymbolDisplayMemberOptions.IncludeExplicitInterface |
                               SymbolDisplayMemberOptions.IncludeParameters |
                               SymbolDisplayMemberOptions.IncludeContainingType |
                               SymbolDisplayMemberOptions.IncludeRef,
                kindOptions: SymbolDisplayKindOptions.IncludeNamespaceKeyword |
                             SymbolDisplayKindOptions.IncludeTypeKeyword |
                             SymbolDisplayKindOptions.IncludeMemberKeyword,
                parameterOptions:
                            SymbolDisplayParameterOptions.IncludeExtensionThis |
                            SymbolDisplayParameterOptions.IncludeParamsRefOut |
                            SymbolDisplayParameterOptions.IncludeType |
                            SymbolDisplayParameterOptions.IncludeName |
                            SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                                      SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier,
                delegateStyle: SymbolDisplayDelegateStyle.NameAndSignature);

    private static readonly SymbolDisplayFormat _shortSymbolNameFormat =
            new(globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                                      SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier,
                delegateStyle: SymbolDisplayDelegateStyle.NameAndSignature);

    private static readonly SymbolDisplayFormat _enumType =
            new(globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters |
                                 SymbolDisplayGenericsOptions.IncludeTypeConstraints |
                                 SymbolDisplayGenericsOptions.IncludeVariance,
                propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
                memberOptions: SymbolDisplayMemberOptions.IncludeType |
                               SymbolDisplayMemberOptions.IncludeExplicitInterface |
                               SymbolDisplayMemberOptions.IncludeParameters |
                               SymbolDisplayMemberOptions.IncludeContainingType |
                               SymbolDisplayMemberOptions.IncludeModifiers |
                               SymbolDisplayMemberOptions.IncludeRef,
                kindOptions: SymbolDisplayKindOptions.IncludeNamespaceKeyword |
                             SymbolDisplayKindOptions.IncludeMemberKeyword,
                parameterOptions:
                            SymbolDisplayParameterOptions.IncludeExtensionThis |
                            SymbolDisplayParameterOptions.IncludeParamsRefOut |
                            SymbolDisplayParameterOptions.IncludeType |
                            SymbolDisplayParameterOptions.IncludeName |
                            SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                                      SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier,
                delegateStyle: SymbolDisplayDelegateStyle.NameAndSignature);

#pragma warning restore SA1118 // Parameter should not span multiple lines

    public AssemblyAnalysis(Assembly assembly)
    {
        Assembly = assembly;

        foreach (var type in Assembly.Types)
        {
            _ = MissingTypes.Add(type);

            foreach (var method in type.Methods)
            {
                _ = MissingMethods.Add(method);
            }

            foreach (var property in type.Properties)
            {
                _ = MissingProperties.Add(property);
            }

            foreach (var field in type.Fields)
            {
                _ = MissingFields.Add(field);
            }
        }
    }

    public void AnalyzeType(INamedTypeSymbol type)
    {
        var typeSignature = type.ToDisplayString(_format);
        var typeModifiersAndName = PrependModifiers(typeSignature, type);
        var typeDef = Assembly.Types.FirstOrDefault(x => x.ModifiersAndName == typeModifiersAndName);

        if (typeDef == null)
        {
            _ = NotFoundInBaseline.Add(type);
            return;
        }

        var baseTypes = new HashSet<string>(type.AllInterfaces.Select(x => x.ToDisplayString(_shortSymbolNameFormat)));
        var baseType = type.BaseType;

        if (type.EnumUnderlyingType != null)
        {
            baseTypes.Clear();

            if (type.EnumUnderlyingType.SpecialType != SpecialType.System_Int32)
            {
                _ = baseTypes.Add(type.EnumUnderlyingType.ToDisplayString(_format));
            }
        }
        else
        {
            if (baseType != null && baseType.SpecialType != SpecialType.System_Object && baseType.SpecialType != SpecialType.System_ValueType)
            {
                _ = baseTypes.Add(baseType.ToDisplayString(_shortSymbolNameFormat));
            }
        }

        foreach (var @base in typeDef.BaseTypes)
        {
            if (!baseTypes.Contains(@base))
            {
                if (MissingBaseTypes.TryGetValue(type, out var collection))
                {
                    collection.Add(@base);
                }
                else
                {
                    MissingBaseTypes.Add(type, [@base]);
                }
            }
        }

        var constraints = new HashSet<string>(Utils.GetConstraints(typeSignature));
        foreach (var constraint in typeDef.Constraints)
        {
            if (!constraints.Contains(constraint))
            {
                if (MissingConstraints.TryGetValue(type, out var collection))
                {
                    collection.Add(constraint);
                }
                else
                {
                    MissingConstraints.Add(type, [constraint]);
                }
            }
        }

        _ = MissingTypes.Remove(typeDef);
        _ = FoundInBaseline.Add((type, typeDef.Stage));

        if (type.TypeKind != TypeKind.Delegate)
        {
            var members = type
                .GetMembers()
                .Where(member => member.IsExternallyVisible())
                .Where(member =>
                {
                    if (member.Kind != SymbolKind.Method)
                    {
                        return true;
                    }

                    var method = (IMethodSymbol)member;
                    return method.MethodKind
                        is not MethodKind.PropertyGet
                        and not MethodKind.PropertySet
                        and not MethodKind.EventAdd
                        and not MethodKind.EventRemove;
                });

            foreach (var member in members)
            {
                if (member is IMethodSymbol methodSymbol)
                {
                    var methodSignature = member.ToDisplayString(_formatNoVariance) + ";";

                    var method = typeDef.Methods.FirstOrDefault(x => x.Member == methodSignature);
                    if (method != null)
                    {
                        _ = MissingMethods.Remove(method);
                        _ = FoundInBaseline.Add((member, method.Stage));
                    }
                    else
                    {
                        _ = NotFoundInBaseline.Add(member);
                    }
                }
                else if (member is IPropertySymbol)
                {
                    var propSignature = member.ToDisplayString(_formatNoVariance);

                    var prop = typeDef.Properties.FirstOrDefault(x => x.Member == propSignature);
                    if (prop != null)
                    {
                        _ = MissingProperties.Remove(prop);
                        _ = FoundInBaseline.Add((member, prop.Stage));
                    }
                    else
                    {
                        _ = NotFoundInBaseline.Add(member);
                    }
                }
                else if (member is IFieldSymbol fieldSym)
                {
                    var fieldSignature = member.ToDisplayString(_formatNoVariance);

                    var t = fieldSym.GetFieldOrPropertyType() as INamedTypeSymbol;
                    if (t?.EnumUnderlyingType != null)
                    {
                        // enum value definitions need special attention
                        fieldSignature = "const " + t.ToDisplayString(_enumType) + " " + fieldSignature;
                    }

                    var field = typeDef.Fields.FirstOrDefault(x => x.Member == fieldSignature);
                    if (field != null)
                    {
                        _ = MissingFields.Remove(field);
                        _ = FoundInBaseline.Add((member, field.Stage));
                    }
                    else
                    {
                        _ = NotFoundInBaseline.Add(member);
                    }
                }
            }
        }
    }

    private static string PrependModifiers(string typeSignature, INamedTypeSymbol type)
    {
        if (type.EnumUnderlyingType != null)
        {
            return typeSignature;
        }

        if (typeSignature.StartsWith("class", StringComparison.Ordinal)
            || typeSignature.StartsWith("record", StringComparison.Ordinal))
        {
            var modifiers = new List<string>(6);

            if (type.IsStatic)
            {
                modifiers.Add("static");
            }

            if (type.IsSealed)
            {
                modifiers.Add("sealed");
            }

            if (type.IsAbstract)
            {
                modifiers.Add("abstract");
            }

            if (type.IsVirtual)
            {
                modifiers.Add("virtual");
            }

            if (type.IsOverride)
            {
                modifiers.Add("override");
            }

            typeSignature = string.Join(" ", modifiers) + " " + typeSignature;
        }

        return Utils.StripBaseAndConstraints(typeSignature);
    }
}
