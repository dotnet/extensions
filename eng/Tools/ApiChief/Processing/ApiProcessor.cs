// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ApiChief.Format;
using ApiChief.Model;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;

namespace ApiChief.Processing;

internal class ApiProcessor
{
    /// <remarks>
    /// This function is mutating the state of the passed <paramref name="finalApi"/>.
    /// </remarks>
    public static void Process(ApiModel finalApi, CSharpDecompiler decompiler)
    {
        var publicTypes = decompiler.TypeSystem.MainModule.TypeDefinitions
            .Where<ITypeDefinition>(type => FilterPublicTypes(type))
            .OrderBy(type => type.Name);

        foreach (var currentType in publicTypes)
        {
            if (currentType.DeclaringType != null)
            {
                continue;
            }

            var typeString = Formatter.TypeToString(currentType, decompiler);
            var parsedCurrentType = new ParsedMember(currentType, decompiler.TypeSystem.MainModule);
            var currentTypeModel = new ApiType
            {
                Type = typeString,
                Stage = parsedCurrentType.Stage,
                FullTypeName = currentType.FullTypeName.Name
            };

            if (!finalApi.Types.Add(currentTypeModel))
            {
                currentTypeModel = finalApi.Types.First(x => x.Type == currentTypeModel.Type);

                var detectedNestedTypeThatWasDecompiledTwice = currentTypeModel.FullTypeName != currentType.FullTypeName.Name;

                if (detectedNestedTypeThatWasDecompiledTwice)
                {
                    continue;
                }
            }

            if (currentType.Kind == TypeKind.Delegate)
            {
                continue;
            }

            ProcessType(finalApi, currentType, currentTypeModel, parsedCurrentType.Stage);
        }

        static bool FilterPublicTypes(ITypeDefinition type)
            => type.EffectiveAccessibility() == Accessibility.Public;
    }

    private static void ProcessType(ApiModel finalApi, ITypeDefinition type, ApiType finalTypeApi, ApiStage classStage)
    {
        ProcessNestedTypes(finalApi, type, classStage);
        ProcessMethods(type, finalTypeApi, classStage);
        ProcessProperties(type, finalTypeApi, classStage);
        ProcessFields(type, finalTypeApi, classStage);
    }

    private static void ProcessFields(ITypeDefinition type, ApiType finalTypeApi, ApiStage classStage)
    {
        var fields = type.Fields
            .Where(field => FilterFields(field, type))
            .OrderBy(field => field.Name);

        foreach (var field in fields)
        {
            var fieldString = Formatter.FieldToString(field);

            if (fieldString.EndsWith("value__"))
            {
                continue;
            }

            var parsedMember = new ParsedMember(field, parentStage: classStage);
            var serializationModel = new ApiMember
            {
                Member = fieldString,
                Stage = parsedMember.Stage,
                Value = field.GetConstantValue()?.ToString(),
            };

            finalTypeApi.Fields ??= new HashSet<ApiMember>();
            finalTypeApi.Fields.Add(serializationModel);
        }

        static bool FilterFields(IField field, ITypeDefinition type)
            => (field.EffectiveAccessibility() == Accessibility.Public || field.EffectiveAccessibility() == Accessibility.Protected)
                && field.DeclaringType.Equals(type);
    }

    private static void ProcessProperties(ITypeDefinition type, ApiType finalTypeApi, ApiStage classStage)
    {
        var properties = type.Properties
            .Where(property => FilterProperties(property, type))
            .OrderBy(property => property.Name);

        foreach (var property in properties)
        {
            var propertyString = Formatter.PropertyToString(property);
            var parsedMember = new ParsedMember(property, parentStage: classStage);
            var serializationModel = new ApiMember
            {
                Member = propertyString,
                Stage = parsedMember.Stage,
            };

            finalTypeApi.Properties ??= new HashSet<ApiMember>();
            finalTypeApi.Properties.Add(serializationModel);
        }

        static bool FilterProperties(IProperty property, ITypeDefinition type)
            => (property.EffectiveAccessibility() == Accessibility.Public || property.EffectiveAccessibility() == Accessibility.Protected)
                && property.DeclaringType.Equals(type);
    }

    [SuppressMessage("Critical Code Smell", "S1067:Expressions should not be too complex", Justification = "Cannot reduce it.")]
    private static void ProcessMethods(ITypeDefinition type, ApiType finalTypeApi, ApiStage classStage)
    {
        var methods = type.Methods
            .Where(method => FilterMethods(method, type))
            .OrderBy(method => method.Name);

        foreach (var method in methods)
        {
            var methodString = Formatter.MethodToString(method);
            var parsedMember = new ParsedMember(method, parentStage: classStage);

            var serializationModel = new ApiMember
            {
                Member = methodString,
                Stage = parsedMember.Stage,
            };

            finalTypeApi.Methods ??= new HashSet<ApiMember>();
            finalTypeApi.Methods.Add(serializationModel);
        }

        static bool FilterMethods(IMethod method, ITypeDefinition type)
            => (method.EffectiveAccessibility() == Accessibility.Public || method.EffectiveAccessibility() == Accessibility.Protected)
                && method.DeclaringType.Equals(type);
    }

    private static void ProcessNestedTypes(ApiModel finalApi, ITypeDefinition type, ApiStage classStage)
    {
        var nestedTypes = type.NestedTypes
            .Where(nested => FilterNestedTypes(nested))
            .OrderBy(nested => nested.Name);

        foreach (var nested in nestedTypes)
        {
            var nestedString = Formatter.NestedTypeToString(nested);
            var parsedMember = new ParsedMember(nested, parentStage: classStage);
            var serializationModel = new ApiType
            {
                Type = nestedString,
                Stage = parsedMember.Stage,
            };

            ProcessType(finalApi, nested, serializationModel, parsedMember.Stage);

            finalApi.Types.Add(serializationModel);
        }

        static bool FilterNestedTypes(ITypeDefinition nested)
            => (nested.EffectiveAccessibility() == Accessibility.Public || nested.EffectiveAccessibility() == Accessibility.Protected)
                && nested.Kind != TypeKind.Delegate;
    }
}
