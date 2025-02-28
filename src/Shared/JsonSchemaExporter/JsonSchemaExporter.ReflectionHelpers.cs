// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET9_0_OR_GREATER
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
#if !NET
using System.Linq;
#endif
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Shared.Diagnostics;

#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields

namespace System.Text.Json.Schema;

internal static partial class JsonSchemaExporter
{
    private static class ReflectionHelpers
    {
        private const BindingFlags AllInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static PropertyInfo? _jsonTypeInfo_ElementType;
        private static PropertyInfo? _jsonPropertyInfo_MemberName;
        private static FieldInfo? _nullableConverter_ElementConverter_Generic;
        private static FieldInfo? _enumConverter_Options_Generic;
        private static FieldInfo? _enumConverter_NamingPolicy_Generic;

        public static bool IsBuiltInConverter(JsonConverter converter) =>
            converter.GetType().Assembly == typeof(JsonConverter).Assembly;

        public static bool CanBeNull(Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;

        public static Type GetElementType(JsonTypeInfo typeInfo)
        {
            Debug.Assert(typeInfo.Kind is JsonTypeInfoKind.Enumerable or JsonTypeInfoKind.Dictionary, "TypeInfo must be of collection type");

            // Uses reflection to access the element type encapsulated by a JsonTypeInfo.
            if (_jsonTypeInfo_ElementType is null)
            {
                PropertyInfo? elementTypeProperty = typeof(JsonTypeInfo).GetProperty("ElementType", AllInstance);
                _jsonTypeInfo_ElementType = Throw.IfNull(elementTypeProperty);
            }

            return (Type)_jsonTypeInfo_ElementType.GetValue(typeInfo)!;
        }

        public static string? GetMemberName(JsonPropertyInfo propertyInfo)
        {
            // Uses reflection to the member name encapsulated by a JsonPropertyInfo.
            if (_jsonPropertyInfo_MemberName is null)
            {
                PropertyInfo? memberName = typeof(JsonPropertyInfo).GetProperty("MemberName", AllInstance);
                _jsonPropertyInfo_MemberName = Throw.IfNull(memberName);
            }

            return (string?)_jsonPropertyInfo_MemberName.GetValue(propertyInfo);
        }

        public static JsonConverter GetElementConverter(JsonConverter nullableConverter)
        {
            // Uses reflection to access the element converter encapsulated by a nullable converter.
            if (_nullableConverter_ElementConverter_Generic is null)
            {
                FieldInfo? genericFieldInfo = Type
                    .GetType("System.Text.Json.Serialization.Converters.NullableConverter`1, System.Text.Json")!
                    .GetField("_elementConverter", AllInstance);

                _nullableConverter_ElementConverter_Generic = Throw.IfNull(genericFieldInfo);
            }

            Type converterType = nullableConverter.GetType();
            var thisFieldInfo = (FieldInfo)converterType.GetMemberWithSameMetadataDefinitionAs(_nullableConverter_ElementConverter_Generic);
            return (JsonConverter)thisFieldInfo.GetValue(nullableConverter)!;
        }

        public static void GetEnumConverterConfig(JsonConverter enumConverter, out JsonNamingPolicy? namingPolicy, out bool allowString)
        {
            // Uses reflection to access configuration encapsulated by an enum converter.
            if (_enumConverter_Options_Generic is null)
            {
                FieldInfo? genericFieldInfo = Type
                    .GetType("System.Text.Json.Serialization.Converters.EnumConverter`1, System.Text.Json")!
                    .GetField("_converterOptions", AllInstance);

                _enumConverter_Options_Generic = Throw.IfNull(genericFieldInfo);
            }

            if (_enumConverter_NamingPolicy_Generic is null)
            {
                FieldInfo? genericFieldInfo = Type
                    .GetType("System.Text.Json.Serialization.Converters.EnumConverter`1, System.Text.Json")!
                    .GetField("_namingPolicy", AllInstance);

                _enumConverter_NamingPolicy_Generic = Throw.IfNull(genericFieldInfo);
            }

            const int EnumConverterOptionsAllowStrings = 1;
            Type converterType = enumConverter.GetType();
            var converterOptionsField = (FieldInfo)converterType.GetMemberWithSameMetadataDefinitionAs(_enumConverter_Options_Generic);
            var namingPolicyField = (FieldInfo)converterType.GetMemberWithSameMetadataDefinitionAs(_enumConverter_NamingPolicy_Generic);

            namingPolicy = (JsonNamingPolicy?)namingPolicyField.GetValue(enumConverter);
            int converterOptions = (int)converterOptionsField.GetValue(enumConverter)!;
            allowString = (converterOptions & EnumConverterOptionsAllowStrings) != 0;
        }

        // The .NET 8 source generator doesn't populate attribute providers for properties
        // cf. https://github.com/dotnet/runtime/issues/100095
        // Work around the issue by running a query for the relevant MemberInfo using the internal MemberName property
        // https://github.com/dotnet/runtime/blob/de774ff9ee1a2c06663ab35be34b755cd8d29731/src/libraries/System.Text.Json/src/System/Text/Json/Serialization/Metadata/JsonPropertyInfo.cs#L206
        public static ICustomAttributeProvider? ResolveAttributeProvider(
            [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
            Type? declaringType,
            JsonPropertyInfo? propertyInfo)
        {
            if (declaringType is null || propertyInfo is null)
            {
                return null;
            }

            if (propertyInfo.AttributeProvider is { } provider)
            {
                return provider;
            }

            string? memberName = ReflectionHelpers.GetMemberName(propertyInfo);
            if (memberName is not null)
            {
                return (MemberInfo?)declaringType.GetProperty(memberName, AllInstance) ??
                                    declaringType.GetField(memberName, AllInstance);
            }

            return null;
        }

        // Resolves the parameters of the deserialization constructor for a type, if they exist.
        public static Func<JsonPropertyInfo, ParameterInfo?>? ResolveJsonConstructorParameterMapper(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            Type type,
            JsonTypeInfo typeInfo)
        {
            Debug.Assert(type == typeInfo.Type, "The declaring type must match the typeInfo type.");
            Debug.Assert(typeInfo.Kind is JsonTypeInfoKind.Object, "Should only be passed object JSON kinds.");

            if (typeInfo.Properties.Count > 0 &&
                typeInfo.CreateObject is null && // Ensure that a default constructor isn't being used
                TryGetDeserializationConstructor(type, useDefaultCtorInAnnotatedStructs: true, out ConstructorInfo? ctor))
            {
                ParameterInfo[]? parameters = ctor?.GetParameters();
                if (parameters?.Length > 0)
                {
                    Dictionary<ParameterLookupKey, ParameterInfo> dict = new(parameters.Length);
                    foreach (ParameterInfo parameter in parameters)
                    {
                        if (parameter.Name is not null)
                        {
                            // We don't care about null parameter names or conflicts since they
                            // would have already been rejected by JsonTypeInfo exporterOptions.
                            dict[new(parameter.Name, parameter.ParameterType)] = parameter;
                        }
                    }

                    return prop => dict.TryGetValue(new(prop.Name, prop.PropertyType), out ParameterInfo? parameter) ? parameter : null;
                }
            }

            return null;
        }

        // Resolves the nullable reference type annotations for a property or field,
        // additionally addressing a few known bugs of the NullabilityInfo pre .NET 9.
        public static NullabilityInfo GetMemberNullability(NullabilityInfoContext context, MemberInfo memberInfo)
        {
            Debug.Assert(memberInfo is PropertyInfo or FieldInfo, "Member must be property or field.");
            return memberInfo is PropertyInfo prop
                ? context.Create(prop)
                : context.Create((FieldInfo)memberInfo);
        }

        public static NullabilityState GetParameterNullability(NullabilityInfoContext context, ParameterInfo parameterInfo)
        {
#if NET8_0
            // Workaround for https://github.com/dotnet/runtime/issues/92487
            // The fix has been incorporated into .NET 9 (and the polyfilled implementations in netfx).
            // Should be removed once .NET 8 support is dropped.
            if (GetGenericParameterDefinition(parameterInfo) is { ParameterType: { IsGenericParameter: true } typeParam })
            {
                // Step 1. Look for nullable annotations on the type parameter.
                if (GetNullableFlags(typeParam) is byte[] flags)
                {
                    return TranslateByte(flags[0]);
                }

                // Step 2. Look for nullable annotations on the generic method declaration.
                if (typeParam.DeclaringMethod != null && GetNullableContextFlag(typeParam.DeclaringMethod) is byte flag)
                {
                    return TranslateByte(flag);
                }

                // Step 3. Look for nullable annotations on the generic method declaration.
                if (GetNullableContextFlag(typeParam.DeclaringType!) is byte flag2)
                {
                    return TranslateByte(flag2);
                }

                // Default to nullable.
                return NullabilityState.Nullable;

                static byte[]? GetNullableFlags(MemberInfo member)
                {
                    foreach (CustomAttributeData attr in member.GetCustomAttributesData())
                    {
                        Type attrType = attr.AttributeType;
                        if (attrType.Name == "NullableAttribute" && attrType.Namespace == "System.Runtime.CompilerServices")
                        {
                            foreach (CustomAttributeTypedArgument ctorArg in attr.ConstructorArguments)
                            {
                                switch (ctorArg.Value)
                                {
                                    case byte flag:
                                        return [flag];
                                    case byte[] flags:
                                        return flags;
                                }
                            }
                        }
                    }

                    return null;
                }

                static byte? GetNullableContextFlag(MemberInfo member)
                {
                    foreach (CustomAttributeData attr in member.GetCustomAttributesData())
                    {
                        Type attrType = attr.AttributeType;
                        if (attrType.Name == "NullableContextAttribute" && attrType.Namespace == "System.Runtime.CompilerServices")
                        {
                            foreach (CustomAttributeTypedArgument ctorArg in attr.ConstructorArguments)
                            {
                                if (ctorArg.Value is byte flag)
                                {
                                    return flag;
                                }
                            }
                        }
                    }

                    return null;
                }

#pragma warning disable S109 // Magic numbers should not be used
                static NullabilityState TranslateByte(byte b) => b switch
                {
                    1 => NullabilityState.NotNull,
                    2 => NullabilityState.Nullable,
                    _ => NullabilityState.Unknown
                };
#pragma warning restore S109 // Magic numbers should not be used
            }

            static ParameterInfo GetGenericParameterDefinition(ParameterInfo parameter)
            {
                if (parameter.Member is { DeclaringType.IsConstructedGenericType: true }
                                        or MethodInfo { IsGenericMethod: true, IsGenericMethodDefinition: false })
                {
                    var genericMethod = (MethodBase)GetGenericMemberDefinition(parameter.Member);
                    return genericMethod.GetParameters()[parameter.Position];
                }

                return parameter;
            }

            static MemberInfo GetGenericMemberDefinition(MemberInfo member)
            {
                if (member is Type type)
                {
                    return type.IsConstructedGenericType ? type.GetGenericTypeDefinition() : type;
                }

                if (member.DeclaringType?.IsConstructedGenericType is true)
                {
                    return member.DeclaringType.GetGenericTypeDefinition().GetMemberWithSameMetadataDefinitionAs(member);
                }

                if (member is MethodInfo { IsGenericMethod: true, IsGenericMethodDefinition: false } method)
                {
                    return method.GetGenericMethodDefinition();
                }

                return member;
            }
#endif
            return context.Create(parameterInfo).WriteState;
        }

        // Taken from https://github.com/dotnet/runtime/blob/903bc019427ca07080530751151ea636168ad334/src/libraries/System.Text.Json/Common/ReflectionExtensions.cs#L288-L317
        public static object? GetNormalizedDefaultValue(ParameterInfo parameterInfo)
        {
            Type parameterType = parameterInfo.ParameterType;
            object? defaultValue = parameterInfo.DefaultValue;

            if (defaultValue is null)
            {
                return null;
            }

            // DBNull.Value is sometimes used as the default value (returned by reflection) of nullable params in place of null.
            if (defaultValue == DBNull.Value && parameterType != typeof(DBNull))
            {
                return null;
            }

            // Default values of enums or nullable enums are represented using the underlying type and need to be cast explicitly
            // cf. https://github.com/dotnet/runtime/issues/68647
            if (parameterType.IsEnum)
            {
                return Enum.ToObject(parameterType, defaultValue);
            }

            if (Nullable.GetUnderlyingType(parameterType) is Type underlyingType && underlyingType.IsEnum)
            {
                return Enum.ToObject(underlyingType, defaultValue);
            }

            return defaultValue;
        }

        // Resolves the deserialization constructor for a type using logic copied from
        // https://github.com/dotnet/runtime/blob/e12e2fa6cbdd1f4b0c8ad1b1e2d960a480c21703/src/libraries/System.Text.Json/Common/ReflectionExtensions.cs#L227-L286
        private static bool TryGetDeserializationConstructor(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            Type type,
            bool useDefaultCtorInAnnotatedStructs,
            out ConstructorInfo? deserializationCtor)
        {
            ConstructorInfo? ctorWithAttribute = null;
            ConstructorInfo? publicParameterlessCtor = null;
            ConstructorInfo? lonePublicCtor = null;

            ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

            if (constructors.Length == 1)
            {
                lonePublicCtor = constructors[0];
            }

            foreach (ConstructorInfo constructor in constructors)
            {
                if (HasJsonConstructorAttribute(constructor))
                {
                    if (ctorWithAttribute != null)
                    {
                        deserializationCtor = null;
                        return false;
                    }

                    ctorWithAttribute = constructor;
                }
                else if (constructor.GetParameters().Length == 0)
                {
                    publicParameterlessCtor = constructor;
                }
            }

            // Search for non-public ctors with [JsonConstructor].
            foreach (ConstructorInfo constructor in type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (HasJsonConstructorAttribute(constructor))
                {
                    if (ctorWithAttribute != null)
                    {
                        deserializationCtor = null;
                        return false;
                    }

                    ctorWithAttribute = constructor;
                }
            }

            // Structs will use default constructor if attribute isn't used.
            if (useDefaultCtorInAnnotatedStructs && type.IsValueType && ctorWithAttribute == null)
            {
                deserializationCtor = null;
                return true;
            }

            deserializationCtor = ctorWithAttribute ?? publicParameterlessCtor ?? lonePublicCtor;
            return true;

            static bool HasJsonConstructorAttribute(ConstructorInfo constructorInfo) =>
                constructorInfo.GetCustomAttribute<JsonConstructorAttribute>() != null;
        }

        // Parameter to property matching semantics as declared in
        // https://github.com/dotnet/runtime/blob/12d96ccfaed98e23c345188ee08f8cfe211c03e7/src/libraries/System.Text.Json/src/System/Text/Json/Serialization/Metadata/JsonTypeInfo.cs#L1007-L1030
        private readonly struct ParameterLookupKey : IEquatable<ParameterLookupKey>
        {
            public ParameterLookupKey(string name, Type type)
            {
                Name = name;
                Type = type;
            }

            public string Name { get; }
            public Type Type { get; }

            public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Name);
            public bool Equals(ParameterLookupKey other) => Type == other.Type && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
            public override bool Equals(object? obj) => obj is ParameterLookupKey key && Equals(key);
        }
    }

#if !NET
    private static MemberInfo GetMemberWithSameMetadataDefinitionAs(this Type specializedType, MemberInfo member)
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        return specializedType.GetMember(member.Name, member.MemberType, All).First(m => m.MetadataToken == member.MetadataToken);
    }
#endif
}
#endif
