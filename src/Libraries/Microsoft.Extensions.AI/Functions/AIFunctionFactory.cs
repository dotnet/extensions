// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
#if !NET
using System.Linq;
#endif
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Shared.Collections;
using Microsoft.Shared.Diagnostics;

#pragma warning disable CA1031 // Do not catch general exception types
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
#pragma warning disable SA1118 // Parameter should not span multiple lines
#pragma warning disable SA1500 // Braces for multi-line statements should not share line

namespace Microsoft.Extensions.AI;

/// <summary>Provides factory methods for creating commonly used implementations of <see cref="AIFunction"/>.</summary>
/// <related type="Article" href="https://learn.microsoft.com/dotnet/ai/quickstarts/use-function-calling">Invoke .NET functions using an AI model.</related>
public static partial class AIFunctionFactory
{
    /// <summary>Holds the default options instance used when creating function.</summary>
    private static readonly AIFunctionFactoryOptions _defaultOptions = new();

    /// <summary>Creates an <see cref="AIFunction"/> instance for a method, specified via a delegate.</summary>
    /// <param name="method">The method to be represented via the created <see cref="AIFunction"/>.</param>
    /// <param name="options">Metadata to use to override defaults inferred from <paramref name="method"/>.</param>
    /// <returns>The created <see cref="AIFunction"/> for invoking <paramref name="method"/>.</returns>
    /// <remarks>
    /// <para>
    /// By default, any parameters to <paramref name="method"/> are sourced from the <see cref="AIFunctionArguments"/>'s dictionary
    /// of key/value pairs and are represented in the JSON schema for the function, as exposed in the returned <see cref="AIFunction"/>'s
    /// <see cref="AIFunction.JsonSchema"/>. There are a few exceptions to this:
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       <see cref="CancellationToken"/> parameters are automatically bound to the <see cref="CancellationToken"/> passed into
    ///       the invocation via <see cref="AIFunction.InvokeAsync"/>'s <see cref="CancellationToken"/> parameter. The parameter is
    ///       not included in the generated JSON schema. The behavior of <see cref="CancellationToken"/> parameters may not be overridden.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       By default, <see cref="IServiceProvider"/> parameters are bound from the <see cref="AIFunctionArguments.Services"/> property
    ///       and are not included in the JSON schema. If the parameter is optional, such that a default value is provided,
    ///       <see cref="AIFunctionArguments.Services"/> is allowed to be <see langword="null"/>; otherwise, <see cref="AIFunctionArguments.Services"/>
    ///       must be non-<see langword="null"/>, or else the invocation will fail with an exception due to the required nature of the parameter.
    ///       The handling of <see cref="IServiceProvider"/> parameters may be overridden via <see cref="AIFunctionFactoryOptions.ConfigureParameterBinding"/>.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       By default, <see cref="AIFunctionArguments"/> parameters are bound directly to <see cref="AIFunctionArguments"/> instance
    ///       passed into <see cref="AIFunction.InvokeAsync"/> and are not included in the JSON schema. If the <see cref="AIFunctionArguments"/>
    ///       instance passed to <see cref="AIFunction.InvokeAsync"/> is <see langword="null"/>, the <see cref="AIFunction"/> implementation
    ///       manufactures an empty instance, such that parameters of type <see cref="AIFunctionArguments"/> may always be satisfied, whether
    ///       optional or not. The handling of <see cref="AIFunctionArguments"/> parameters may be overridden via
    ///       <see cref="AIFunctionFactoryOptions.ConfigureParameterBinding"/>.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       By default, parameters attributed with <see cref="FromKeyedServicesAttribute"/> are resolved from the <see cref="AIFunctionArguments.Services"/>
    ///       property and are not included in the JSON schema. If the parameter is optional, such that a default value is provided,
    ///       <see cref="AIFunctionArguments.Services"/> is allowed to be <see langword="null"/>; otherwise, <see cref="AIFunctionArguments.Services"/>
    ///       must be non-<see langword="null"/>, or else the invocation will fail with an exception due to the required nature of the parameter.
    ///       The handling of such parameters may be overridden via <see cref="AIFunctionFactoryOptions.ConfigureParameterBinding"/>.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       When the <see cref="AIFunction"/> is constructed, it may be passed an <see cref="IServiceProvider"/> via 
    ///       <see cref="AIFunctionFactoryOptions.Services"/>. Any parameter that can be satisfied by that <see cref="IServiceProvider"/>
    ///       according to <see cref="IServiceProviderIsService"/> will not be included in the generated JSON schema and will be resolved 
    ///       from the <see cref="IServiceProvider"/> provided to <see cref="AIFunction.InvokeAsync"/> via <see cref="AIFunctionArguments.Services"/>,
    ///       rather than from the argument collection. The handling of such parameters may be overridden via
    ///       <see cref="AIFunctionFactoryOptions.ConfigureParameterBinding"/>.
    ///     </description>
    ///   </item>
    /// </list>
    /// All other parameter types are, by default, bound from the <see cref="AIFunctionArguments"/> dictionary passed into <see cref="AIFunction.InvokeAsync"/>
    /// and are included in the generated JSON schema. This may be overridden by the <see cref="AIFunctionFactoryOptions.ConfigureParameterBinding"/> provided
    /// via the <paramref name="options"/> parameter; for every parameter, the delegate is enabled to choose if the parameter should be included in the
    /// generated schema and how its value should be bound, including handling of optionality (by default, required parameters that are not included in the
    /// <see cref="AIFunctionArguments"/> dictionary will result in an exception being thrown). Loosely-typed additional context information may be passed
    /// into <see cref="AIFunction.InvokeAsync"/> via the <see cref="AIFunctionArguments"/>'s <see cref="AIFunctionArguments.Context"/> dictionary; the default
    /// binding ignores this collection, but a custom binding supplied via <see cref="AIFunctionFactoryOptions.ConfigureParameterBinding"/> may choose to
    /// source arguments from this data.
    /// </para>
    /// <para>
    /// The default marshaling of parameters from the <see cref="AIFunctionArguments"/> dictionary permits values to be passed into the <paramref name="method"/>'s
    /// invocation directly if the object is already of a compatible type. Otherwise, if the argument is a <see cref="JsonElement"/>, <see cref="JsonDocument"/>,
    /// or <see cref="JsonNode"/>, it is deserialized into the parameter type, utilizing <see cref="AIFunctionFactoryOptions.SerializerOptions"/> if provided,
    /// or else using <see cref="AIJsonUtilities.DefaultOptions"/>. If the argument is anything else, it is round-tripped through JSON, serializing the object as JSON
    /// and then deserializing it to the expected type.
    /// </para>
    /// <para>
    /// In general, the data supplied via an <see cref="AIFunctionArguments"/>'s dictionary is supplied from an AI service and should be considered
    /// unvalidated and untrusted. To provide validated and trusted data to the invocation of <paramref name="method"/>, consider having <paramref name="method"/>
    /// point to an instance method on an instance configured to hold the appropriate state. An <see cref="IServiceProvider"/> parameter may also be
    /// used to resolve services from a dependency injection container.
    /// </para>
    /// <para>
    /// By default, return values are serialized to <see cref="JsonElement"/> using <paramref name="options"/>'s
    /// <see cref="AIFunctionFactoryOptions.SerializerOptions"/> if provided, or else using <see cref="AIJsonUtilities.DefaultOptions"/>.
    /// Handling of return values may be overridden via <see cref="AIFunctionFactoryOptions.MarshalResult"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="method"/> is <see langword="null"/>.</exception>
    /// <exception cref="JsonException">A parameter to <paramref name="method"/> is not serializable.</exception>
    public static AIFunction Create(Delegate method, AIFunctionFactoryOptions? options)
    {
        _ = Throw.IfNull(method);

        return ReflectionAIFunction.Build(method.Method, method.Target, options ?? _defaultOptions);
    }

    /// <summary>Creates an <see cref="AIFunction"/> instance for a method, specified via a delegate.</summary>
    /// <param name="method">The method to be represented via the created <see cref="AIFunction"/>.</param>
    /// <param name="name">
    /// The name to use for the <see cref="AIFunction"/>. If <see langword="null"/>, the name will be derived from
    /// the name of <paramref name="method"/>.
    /// </param>
    /// <param name="description">
    /// The description to use for the <see cref="AIFunction"/>. If <see langword="null"/>, a description will be derived from
    /// any <see cref="DescriptionAttribute"/> on <paramref name="method"/>, if available.
    /// </param>
    /// <param name="serializerOptions">The <see cref="JsonSerializerOptions"/> used to marshal function parameters and any return value.</param>
    /// <returns>The created <see cref="AIFunction"/> for invoking <paramref name="method"/>.</returns>
    /// <remarks>
    /// <para>
    /// Any parameters to <paramref name="method"/> are sourced from the <see cref="AIFunctionArguments"/>'s dictionary
    /// of key/value pairs and are represented in the JSON schema for the function, as exposed in the returned <see cref="AIFunction"/>'s
    /// <see cref="AIFunction.JsonSchema"/>. There are a few exceptions to this:
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       <see cref="CancellationToken"/> parameters are automatically bound to the <see cref="CancellationToken"/> passed into
    ///       the invocation via <see cref="AIFunction.InvokeAsync"/>'s <see cref="CancellationToken"/> parameter. The parameter is
    ///       not included in the generated JSON schema.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       By default, <see cref="IServiceProvider"/> parameters are bound from the <see cref="AIFunctionArguments.Services"/> property
    ///       and are not included in the JSON schema. If the parameter is optional, such that a default value is provided,
    ///       <see cref="AIFunctionArguments.Services"/> is allowed to be <see langword="null"/>; otherwise, <see cref="AIFunctionArguments.Services"/>
    ///       must be non-<see langword="null"/>, or else the invocation will fail with an exception due to the required nature of the parameter.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       By default, <see cref="AIFunctionArguments"/> parameters are bound directly to <see cref="AIFunctionArguments"/> instance
    ///       passed into <see cref="AIFunction.InvokeAsync"/> and are not included in the JSON schema. If the <see cref="AIFunctionArguments"/>
    ///       instance passed to <see cref="AIFunction.InvokeAsync"/> is <see langword="null"/>, the <see cref="AIFunction"/> implementation
    ///       manufactures an empty instance, such that parameters of type <see cref="AIFunctionArguments"/> may always be satisfied, whether
    ///       optional or not.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       By default, parameters attributed with <see cref="FromKeyedServicesAttribute"/> are resolved from the <see cref="AIFunctionArguments.Services"/>
    ///       property and are not included in the JSON schema. If the parameter is optional, such that a default value is provided,
    ///       <see cref="AIFunctionArguments.Services"/> is allowed to be <see langword="null"/>; otherwise, <see cref="AIFunctionArguments.Services"/>
    ///       must be non-<see langword="null"/>, or else the invocation will fail with an exception due to the required nature of the parameter.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       When the <see cref="AIFunction"/> is constructed, it may be passed an <see cref="IServiceProvider"/> via 
    ///       <see cref="AIFunctionFactoryOptions.Services"/>. Any parameter that can be satisfied by that <see cref="IServiceProvider"/>
    ///       according to <see cref="IServiceProviderIsService"/> will not be included in the generated JSON schema and will be resolved 
    ///       from the <see cref="IServiceProvider"/> provided to <see cref="AIFunction.InvokeAsync"/> via <see cref="AIFunctionArguments.Services"/>,
    ///       rather than from the argument collection.
    ///     </description>
    ///   </item>
    /// </list>
    /// All other parameter types are bound from the <see cref="AIFunctionArguments"/> dictionary passed into <see cref="AIFunction.InvokeAsync"/>
    /// and are included in the generated JSON schema.
    /// </para>
    /// <para>
    /// The marshaling of parameters from the <see cref="AIFunctionArguments"/> dictionary permits values to be passed into the <paramref name="method"/>'s
    /// invocation directly if the object is already of a compatible type. Otherwise, if the argument is a <see cref="JsonElement"/>, <see cref="JsonDocument"/>,
    /// or <see cref="JsonNode"/>, it is deserialized into the parameter type, utilizing <paramref name="serializerOptions"/> if provided, or else
    /// <see cref="AIJsonUtilities.DefaultOptions"/>. If the argument is anything else, it is round-tripped through JSON, serializing the object as JSON
    /// and then deserializing it to the expected type.
    /// </para>
    /// <para>
    /// In general, the data supplied via an <see cref="AIFunctionArguments"/>'s dictionary is supplied from an AI service and should be considered
    /// unvalidated and untrusted. To provide validated and trusted data to the invocation of <paramref name="method"/>, consider having <paramref name="method"/>
    /// point to an instance method on an instance configured to hold the appropriate state. An <see cref="IServiceProvider"/> parameter may also be
    /// used to resolve services from a dependency injection container.
    /// </para>
    /// <para>
    /// Return values are serialized to <see cref="JsonElement"/> using <paramref name="serializerOptions"/> if provided,
    /// or else using <see cref="AIJsonUtilities.DefaultOptions"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="method"/> is <see langword="null"/>.</exception>
    /// <exception cref="JsonException">A parameter to <paramref name="method"/> is not serializable.</exception>
    public static AIFunction Create(Delegate method, string? name = null, string? description = null, JsonSerializerOptions? serializerOptions = null)
    {
        _ = Throw.IfNull(method);

        AIFunctionFactoryOptions createOptions = serializerOptions is null && name is null && description is null
            ? _defaultOptions
            : new()
            {
                Name = name,
                Description = description,
                SerializerOptions = serializerOptions,
            };

        return ReflectionAIFunction.Build(method.Method, method.Target, createOptions);
    }

    /// <summary>
    /// Creates an <see cref="AIFunction"/> instance for a method, specified via an <see cref="MethodInfo"/> instance
    /// and an optional target object if the method is an instance method.
    /// </summary>
    /// <param name="method">The method to be represented via the created <see cref="AIFunction"/>.</param>
    /// <param name="target">
    /// The target object for the <paramref name="method"/> if it represents an instance method.
    /// This should be <see langword="null"/> if and only if <paramref name="method"/> is a static method.
    /// </param>
    /// <param name="options">Metadata to use to override defaults inferred from <paramref name="method"/>.</param>
    /// <returns>The created <see cref="AIFunction"/> for invoking <paramref name="method"/>.</returns>
    /// <remarks>
    /// <para>
    /// By default, any parameters to <paramref name="method"/> are sourced from the <see cref="AIFunctionArguments"/>'s dictionary
    /// of key/value pairs and are represented in the JSON schema for the function, as exposed in the returned <see cref="AIFunction"/>'s
    /// <see cref="AIFunction.JsonSchema"/>. There are a few exceptions to this:
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       <see cref="CancellationToken"/> parameters are automatically bound to the <see cref="CancellationToken"/> passed into
    ///       the invocation via <see cref="AIFunction.InvokeAsync"/>'s <see cref="CancellationToken"/> parameter. The parameter is
    ///       not included in the generated JSON schema. The behavior of <see cref="CancellationToken"/> parameters may not be overridden.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       By default, <see cref="IServiceProvider"/> parameters are bound from the <see cref="AIFunctionArguments.Services"/> property
    ///       and are not included in the JSON schema. If the parameter is optional, such that a default value is provided,
    ///       <see cref="AIFunctionArguments.Services"/> is allowed to be <see langword="null"/>; otherwise, <see cref="AIFunctionArguments.Services"/>
    ///       must be non-<see langword="null"/>, or else the invocation will fail with an exception due to the required nature of the parameter.
    ///       The handling of <see cref="IServiceProvider"/> parameters may be overridden via <see cref="AIFunctionFactoryOptions.ConfigureParameterBinding"/>.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       By default, <see cref="AIFunctionArguments"/> parameters are bound directly to <see cref="AIFunctionArguments"/> instance
    ///       passed into <see cref="AIFunction.InvokeAsync"/> and are not included in the JSON schema. If the <see cref="AIFunctionArguments"/>
    ///       instance passed to <see cref="AIFunction.InvokeAsync"/> is <see langword="null"/>, the <see cref="AIFunction"/> implementation
    ///       manufactures an empty instance, such that parameters of type <see cref="AIFunctionArguments"/> may always be satisfied, whether
    ///       optional or not. The handling of <see cref="AIFunctionArguments"/> parameters may be overridden via
    ///       <see cref="AIFunctionFactoryOptions.ConfigureParameterBinding"/>.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       By default, parameters attributed with <see cref="FromKeyedServicesAttribute"/> are resolved from the <see cref="AIFunctionArguments.Services"/>
    ///       property and are not included in the JSON schema. If the parameter is optional, such that a default value is provided,
    ///       <see cref="AIFunctionArguments.Services"/> is allowed to be <see langword="null"/>; otherwise, <see cref="AIFunctionArguments.Services"/>
    ///       must be non-<see langword="null"/>, or else the invocation will fail with an exception due to the required nature of the parameter.
    ///       The handling of such parameters may be overridden via <see cref="AIFunctionFactoryOptions.ConfigureParameterBinding"/>.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       When the <see cref="AIFunction"/> is constructed, it may be passed an <see cref="IServiceProvider"/> via 
    ///       <see cref="AIFunctionFactoryOptions.Services"/>. Any parameter that can be satisfied by that <see cref="IServiceProvider"/>
    ///       according to <see cref="IServiceProviderIsService"/> will not be included in the generated JSON schema and will be resolved 
    ///       from the <see cref="IServiceProvider"/> provided to <see cref="AIFunction.InvokeAsync"/> via <see cref="AIFunctionArguments.Services"/>,
    ///       rather than from the argument collection. The handling of such parameters may be overridden via
    ///       <see cref="AIFunctionFactoryOptions.ConfigureParameterBinding"/>.
    ///     </description>
    ///   </item>
    /// </list>
    /// All other parameter types are, by default, bound from the <see cref="AIFunctionArguments"/> dictionary passed into <see cref="AIFunction.InvokeAsync"/>
    /// and are included in the generated JSON schema. This may be overridden by the <see cref="AIFunctionFactoryOptions.ConfigureParameterBinding"/> provided
    /// via the <paramref name="options"/> parameter; for every parameter, the delegate is enabled to choose if the parameter should be included in the
    /// generated schema and how its value should be bound, including handling of optionality (by default, required parameters that are not included in the
    /// <see cref="AIFunctionArguments"/> dictionary will result in an exception being thrown). Loosely-typed additional context information may be passed
    /// into <see cref="AIFunction.InvokeAsync"/> via the <see cref="AIFunctionArguments"/>'s <see cref="AIFunctionArguments.Context"/> dictionary; the default
    /// binding ignores this collection, but a custom binding supplied via <see cref="AIFunctionFactoryOptions.ConfigureParameterBinding"/> may choose to
    /// source arguments from this data.
    /// </para>
    /// <para>
    /// The default marshaling of parameters from the <see cref="AIFunctionArguments"/> dictionary permits values to be passed into the <paramref name="method"/>'s
    /// invocation directly if the object is already of a compatible type. Otherwise, if the argument is a <see cref="JsonElement"/>, <see cref="JsonDocument"/>,
    /// or <see cref="JsonNode"/>, it is deserialized into the parameter type, utilizing <see cref="AIFunctionFactoryOptions.SerializerOptions"/> if provided,
    /// or else using <see cref="AIJsonUtilities.DefaultOptions"/>. If the argument is anything else, it is round-tripped through JSON, serializing the object as JSON
    /// and then deserializing it to the expected type.
    /// </para>
    /// <para>
    /// In general, the data supplied via an <see cref="AIFunctionArguments"/>'s dictionary is supplied from an AI service and should be considered
    /// unvalidated and untrusted. To provide validated and trusted data to the invocation of <paramref name="method"/>, consider having <paramref name="method"/>
    /// point to an instance method on an instance configured to hold the appropriate state. An <see cref="IServiceProvider"/> parameter may also be
    /// used to resolve services from a dependency injection container.
    /// </para>
    /// <para>
    /// By default, return values are serialized to <see cref="JsonElement"/> using <paramref name="options"/>'s
    /// <see cref="AIFunctionFactoryOptions.SerializerOptions"/> if provided, or else using <see cref="AIJsonUtilities.DefaultOptions"/>.
    /// Handling of return values may be overridden via <see cref="AIFunctionFactoryOptions.MarshalResult"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="method"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="method"/> represents an instance method but <paramref name="target"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="method"/> represents an open generic method.</exception>
    /// <exception cref="ArgumentException"><paramref name="method"/> contains a parameter without a parameter name.</exception>
    /// <exception cref="JsonException">A parameter to <paramref name="method"/> or its return type is not serializable.</exception>
    public static AIFunction Create(MethodInfo method, object? target, AIFunctionFactoryOptions? options)
    {
        _ = Throw.IfNull(method);

        return ReflectionAIFunction.Build(method, target, options ?? _defaultOptions);
    }

    /// <summary>
    /// Creates an <see cref="AIFunction"/> instance for a method, specified via an <see cref="MethodInfo"/> instance
    /// and an optional target object if the method is an instance method.
    /// </summary>
    /// <param name="method">The method to be represented via the created <see cref="AIFunction"/>.</param>
    /// <param name="target">
    /// The target object for the <paramref name="method"/> if it represents an instance method.
    /// This should be <see langword="null"/> if and only if <paramref name="method"/> is a static method.
    /// </param>
    /// <param name="name">
    /// The name to use for the <see cref="AIFunction"/>. If <see langword="null"/>, the name will be derived from
    /// the name of <paramref name="method"/>.
    /// </param>
    /// <param name="description">
    /// The description to use for the <see cref="AIFunction"/>. If <see langword="null"/>, a description will be derived from
    /// any <see cref="DescriptionAttribute"/> on <paramref name="method"/>, if available.
    /// </param>
    /// <param name="serializerOptions">The <see cref="JsonSerializerOptions"/> used to marshal function parameters and return value.</param>
    /// <returns>The created <see cref="AIFunction"/> for invoking <paramref name="method"/>.</returns>
    /// <remarks>
    /// <para>
    /// Any parameters to <paramref name="method"/> are sourced from the <see cref="AIFunctionArguments"/>'s dictionary
    /// of key/value pairs and are represented in the JSON schema for the function, as exposed in the returned <see cref="AIFunction"/>'s
    /// <see cref="AIFunction.JsonSchema"/>. There are a few exceptions to this:
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       <see cref="CancellationToken"/> parameters are automatically bound to the <see cref="CancellationToken"/> passed into
    ///       the invocation via <see cref="AIFunction.InvokeAsync"/>'s <see cref="CancellationToken"/> parameter. The parameter is
    ///       not included in the generated JSON schema.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       By default, <see cref="IServiceProvider"/> parameters are bound from the <see cref="AIFunctionArguments.Services"/> property
    ///       and are not included in the JSON schema. If the parameter is optional, such that a default value is provided,
    ///       <see cref="AIFunctionArguments.Services"/> is allowed to be <see langword="null"/>; otherwise, <see cref="AIFunctionArguments.Services"/>
    ///       must be non-<see langword="null"/>, or else the invocation will fail with an exception due to the required nature of the parameter.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       By default, <see cref="AIFunctionArguments"/> parameters are bound directly to <see cref="AIFunctionArguments"/> instance
    ///       passed into <see cref="AIFunction.InvokeAsync"/> and are not included in the JSON schema. If the <see cref="AIFunctionArguments"/>
    ///       instance passed to <see cref="AIFunction.InvokeAsync"/> is <see langword="null"/>, the <see cref="AIFunction"/> implementation
    ///       manufactures an empty instance, such that parameters of type <see cref="AIFunctionArguments"/> may always be satisfied, whether
    ///       optional or not.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       By default, parameters attributed with <see cref="FromKeyedServicesAttribute"/> are resolved from the <see cref="AIFunctionArguments.Services"/>
    ///       property and are not included in the JSON schema. If the parameter is optional, such that a default value is provided,
    ///       <see cref="AIFunctionArguments.Services"/> is allowed to be <see langword="null"/>; otherwise, <see cref="AIFunctionArguments.Services"/>
    ///       must be non-<see langword="null"/>, or else the invocation will fail with an exception due to the required nature of the parameter.
    ///     </description>
    ///   <item>
    ///     <description>
    ///       When the <see cref="AIFunction"/> is constructed, it may be passed an <see cref="IServiceProvider"/> via 
    ///       <see cref="AIFunctionFactoryOptions.Services"/>. Any parameter that can be satisfied by that <see cref="IServiceProvider"/>
    ///       according to <see cref="IServiceProviderIsService"/> will not be included in the generated JSON schema and will be resolved 
    ///       from the <see cref="IServiceProvider"/> provided to <see cref="AIFunction.InvokeAsync"/> via <see cref="AIFunctionArguments.Services"/>,
    ///       rather than from the argument collection.
    ///     </description>
    ///   </item>
    ///   </item>
    /// </list>
    /// All other parameter types are bound from the <see cref="AIFunctionArguments"/> dictionary passed into <see cref="AIFunction.InvokeAsync"/>
    /// and are included in the generated JSON schema.
    /// </para>
    /// <para>
    /// The marshaling of parameters from the <see cref="AIFunctionArguments"/> dictionary permits values to be passed into the <paramref name="method"/>'s
    /// invocation directly if the object is already of a compatible type. Otherwise, if the argument is a <see cref="JsonElement"/>, <see cref="JsonDocument"/>,
    /// or <see cref="JsonNode"/>, it is deserialized into the parameter type, utilizing <paramref name="serializerOptions"/> if provided, or else
    /// <see cref="AIJsonUtilities.DefaultOptions"/>. If the argument is anything else, it is round-tripped through JSON, serializing the object as JSON
    /// and then deserializing it to the expected type.
    /// </para>
    /// <para>
    /// In general, the data supplied via an <see cref="AIFunctionArguments"/>'s dictionary is supplied from an AI service and should be considered
    /// unvalidated and untrusted. To provide validated and trusted data to the invocation of <paramref name="method"/>, consider having <paramref name="method"/>
    /// point to an instance method on an instance configured to hold the appropriate state. An <see cref="IServiceProvider"/> parameter may also be
    /// used to resolve services from a dependency injection container.
    /// </para>
    /// <para>
    /// Return values are serialized to <see cref="JsonElement"/> using <paramref name="serializerOptions"/> if provided,
    /// or else using <see cref="AIJsonUtilities.DefaultOptions"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="method"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="method"/> represents an instance method but <paramref name="target"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="method"/> represents an open generic method.</exception>
    /// <exception cref="ArgumentException"><paramref name="method"/> contains a parameter without a parameter name.</exception>
    /// <exception cref="JsonException">A parameter to <paramref name="method"/> or its return type is not serializable.</exception>
    public static AIFunction Create(MethodInfo method, object? target, string? name = null, string? description = null, JsonSerializerOptions? serializerOptions = null)
    {
        _ = Throw.IfNull(method);

        AIFunctionFactoryOptions createOptions = serializerOptions is null && name is null && description is null
            ? _defaultOptions
            : new()
            {
                Name = name,
                Description = description,
                SerializerOptions = serializerOptions,
            };

        return ReflectionAIFunction.Build(method, target, createOptions);
    }

    /// <summary>
    /// Creates an <see cref="AIFunction"/> instance for a method, specified via an <see cref="MethodInfo"/> for
    /// and instance method, along with a <see cref="Type"/> representing the type of the target object to
    /// instantiate each time the method is invoked.
    /// </summary>
    /// <param name="method">The instance method to be represented via the created <see cref="AIFunction"/>.</param>
    /// <param name="targetType">
    /// The <see cref="Type"/> to construct an instance of on which to invoke <paramref name="method"/> when
    /// the resulting <see cref="AIFunction"/> is invoked. If <see cref="AIFunctionArguments.Services"/> is provided,
    /// <see cref="ActivatorUtilities.CreateInstance"/> will be used to construct the instance using those services; otherwise,
    /// <see cref="Activator.CreateInstance(Type)"/> is used, utilizing the type's public parameterless constructor.
    /// If an instance can't be constructed, an exception is thrown during the function's invocation.
    /// </param>
    /// <param name="options">Metadata to use to override defaults inferred from <paramref name="method"/>.</param>
    /// <returns>The created <see cref="AIFunction"/> for invoking <paramref name="method"/>.</returns>
    /// <remarks>
    /// <para>
    /// Return values are serialized to <see cref="JsonElement"/> using <paramref name="options"/>'s
    /// <see cref="AIFunctionFactoryOptions.SerializerOptions"/>. Arguments that are not already of the expected type are
    /// marshaled to the expected type via JSON and using <paramref name="options"/>'s
    /// <see cref="AIFunctionFactoryOptions.SerializerOptions"/>. If the argument is a <see cref="JsonElement"/>,
    /// <see cref="JsonDocument"/>, or <see cref="JsonNode"/>, it is deserialized directly. If the argument is anything else unknown,
    /// it is round-tripped through JSON, serializing the object as JSON and then deserializing it to the expected type.
    /// </para>
    /// <para>
    /// By default, any parameters to <paramref name="method"/> are sourced from the <see cref="AIFunctionArguments"/>'s dictionary
    /// of key/value pairs and are represented in the JSON schema for the function, as exposed in the returned <see cref="AIFunction"/>'s
    /// <see cref="AIFunction.JsonSchema"/>. There are a few exceptions to this:
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       <see cref="CancellationToken"/> parameters are automatically bound to the <see cref="CancellationToken"/> passed into
    ///       the invocation via <see cref="AIFunction.InvokeAsync"/>'s <see cref="CancellationToken"/> parameter. The parameter is
    ///       not included in the generated JSON schema. The behavior of <see cref="CancellationToken"/> parameters may not be overridden.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       By default, <see cref="IServiceProvider"/> parameters are bound from the <see cref="AIFunctionArguments.Services"/> property
    ///       and are not included in the JSON schema. If the parameter is optional, such that a default value is provided,
    ///       <see cref="AIFunctionArguments.Services"/> is allowed to be <see langword="null"/>; otherwise, <see cref="AIFunctionArguments.Services"/>
    ///       must be non-<see langword="null"/>, or else the invocation will fail with an exception due to the required nature of the parameter.
    ///       The handling of <see cref="IServiceProvider"/> parameters may be overridden via <see cref="AIFunctionFactoryOptions.ConfigureParameterBinding"/>.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       By default, <see cref="AIFunctionArguments"/> parameters are bound directly to <see cref="AIFunctionArguments"/> instance
    ///       passed into <see cref="AIFunction.InvokeAsync"/> and are not included in the JSON schema. If the <see cref="AIFunctionArguments"/>
    ///       instance passed to <see cref="AIFunction.InvokeAsync"/> is <see langword="null"/>, the <see cref="AIFunction"/> implementation
    ///       manufactures an empty instance, such that parameters of type <see cref="AIFunctionArguments"/> may always be satisfied, whether
    ///       optional or not. The handling of <see cref="AIFunctionArguments"/> parameters may be overridden via
    ///       <see cref="AIFunctionFactoryOptions.ConfigureParameterBinding"/>.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       By default, parameters attributed with <see cref="FromKeyedServicesAttribute"/> are resolved from the <see cref="AIFunctionArguments.Services"/>
    ///       property and are not included in the JSON schema. If the parameter is optional, such that a default value is provided,
    ///       <see cref="AIFunctionArguments.Services"/> is allowed to be <see langword="null"/>; otherwise, <see cref="AIFunctionArguments.Services"/>
    ///       must be non-<see langword="null"/>, or else the invocation will fail with an exception due to the required nature of the parameter.
    ///       The handling of such parameters may be overridden via <see cref="AIFunctionFactoryOptions.ConfigureParameterBinding"/>.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       When the <see cref="AIFunction"/> is constructed, it may be passed an <see cref="IServiceProvider"/> via 
    ///       <see cref="AIFunctionFactoryOptions.Services"/>. Any parameter that can be satisfied by that <see cref="IServiceProvider"/>
    ///       according to <see cref="IServiceProviderIsService"/> will not be included in the generated JSON schema and will be resolved 
    ///       from the <see cref="IServiceProvider"/> provided to <see cref="AIFunction.InvokeAsync"/> via <see cref="AIFunctionArguments.Services"/>,
    ///       rather than from the argument collection. The handling of such parameters may be overridden via
    ///       <see cref="AIFunctionFactoryOptions.ConfigureParameterBinding"/>.
    ///     </description>
    ///   </item>
    /// </list>
    /// All other parameter types are, by default, bound from the <see cref="AIFunctionArguments"/> dictionary passed into <see cref="AIFunction.InvokeAsync"/>
    /// and are included in the generated JSON schema. This may be overridden by the <see cref="AIFunctionFactoryOptions.ConfigureParameterBinding"/> provided
    /// via the <paramref name="options"/> parameter; for every parameter, the delegate is enabled to choose if the parameter should be included in the
    /// generated schema and how its value should be bound, including handling of optionality (by default, required parameters that are not included in the
    /// <see cref="AIFunctionArguments"/> dictionary will result in an exception being thrown). Loosely-typed additional context information may be passed
    /// into <see cref="AIFunction.InvokeAsync"/> via the <see cref="AIFunctionArguments"/>'s <see cref="AIFunctionArguments.Context"/> dictionary; the default
    /// binding ignores this collection, but a custom binding supplied via <see cref="AIFunctionFactoryOptions.ConfigureParameterBinding"/> may choose to
    /// source arguments from this data.
    /// </para>
    /// <para>
    /// The default marshaling of parameters from the <see cref="AIFunctionArguments"/> dictionary permits values to be passed into the <paramref name="method"/>'s
    /// invocation directly if the object is already of a compatible type. Otherwise, if the argument is a <see cref="JsonElement"/>, <see cref="JsonDocument"/>,
    /// or <see cref="JsonNode"/>, it is deserialized into the parameter type, utilizing <see cref="AIFunctionFactoryOptions.SerializerOptions"/> if provided,
    /// or else using <see cref="AIJsonUtilities.DefaultOptions"/>. If the argument is anything else, it is round-tripped through JSON, serializing the object as JSON
    /// and then deserializing it to the expected type.
    /// </para>
    /// <para>
    /// In general, the data supplied via an <see cref="AIFunctionArguments"/>'s dictionary is supplied from an AI service and should be considered
    /// unvalidated and untrusted. To provide validated and trusted data to the invocation of <paramref name="method"/>, the instance constructed
    /// for each invocation may contain that data in it, such that it's then available to <paramref name="method"/> as instance data.
    /// An <see cref="IServiceProvider"/> parameter may also be used to resolve services from a dependency injection container.
    /// </para>
    /// <para>
    /// By default, return values are serialized to <see cref="JsonElement"/> using <paramref name="options"/>'s
    /// <see cref="AIFunctionFactoryOptions.SerializerOptions"/> if provided, or else using <see cref="AIJsonUtilities.DefaultOptions"/>.
    /// Handling of return values may be overridden via <see cref="AIFunctionFactoryOptions.MarshalResult"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="method"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="targetType"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="method"/> represents a static method.</exception>
    /// <exception cref="ArgumentException"><paramref name="method"/> represents an open generic method.</exception>
    /// <exception cref="ArgumentException"><paramref name="method"/> contains a parameter without a parameter name.</exception>
    /// <exception cref="ArgumentException"><paramref name="targetType"/> is not assignable to <paramref name="method"/>'s declaring type.</exception>
    /// <exception cref="JsonException">A parameter to <paramref name="method"/> or its return type is not serializable.</exception>
    public static AIFunction Create(
        MethodInfo method,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type targetType,
        AIFunctionFactoryOptions? options = null)
    {
        _ = Throw.IfNull(method);
        _ = Throw.IfNull(targetType);

        return ReflectionAIFunction.Build(method, targetType, options ?? _defaultOptions);
    }

    private sealed class ReflectionAIFunction : AIFunction
    {
        public static ReflectionAIFunction Build(MethodInfo method, object? target, AIFunctionFactoryOptions options)
        {
            _ = Throw.IfNull(method);

            if (method.ContainsGenericParameters)
            {
                Throw.ArgumentException(nameof(method), "Open generic methods are not supported");
            }

            if (!method.IsStatic && target is null)
            {
                Throw.ArgumentNullException(nameof(target), "Target must not be null for an instance method.");
            }

            var functionDescriptor = ReflectionAIFunctionDescriptor.GetOrCreate(method, options);

            if (target is null && options.AdditionalProperties is null)
            {
                // We can use a cached value for static methods not specifying additional properties.
                return functionDescriptor.CachedDefaultInstance ??= new(functionDescriptor, target, options);
            }

            return new(functionDescriptor, target, options);
        }

        public static ReflectionAIFunction Build(
            MethodInfo method,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type targetType,
            AIFunctionFactoryOptions options)
        {
            _ = Throw.IfNull(method);

            if (method.ContainsGenericParameters)
            {
                Throw.ArgumentException(nameof(method), "Open generic methods are not supported");
            }

            if (method.IsStatic)
            {
                Throw.ArgumentException(nameof(method), "The method must be an instance method.");
            }

            if (method.DeclaringType is { } declaringType &&
                !declaringType.IsAssignableFrom(targetType))
            {
                Throw.ArgumentException(nameof(targetType), "The target type must be assignable to the method's declaring type.");
            }

            return new(ReflectionAIFunctionDescriptor.GetOrCreate(method, options), targetType, options);
        }

        private ReflectionAIFunction(ReflectionAIFunctionDescriptor functionDescriptor, object? target, AIFunctionFactoryOptions options)
        {
            FunctionDescriptor = functionDescriptor;
            Target = target;
            AdditionalProperties = options.AdditionalProperties ?? EmptyReadOnlyDictionary<string, object?>.Instance;
        }

        private ReflectionAIFunction(
            ReflectionAIFunctionDescriptor functionDescriptor,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type targetType,
            AIFunctionFactoryOptions options)
        {
            FunctionDescriptor = functionDescriptor;
            TargetType = targetType;
            AdditionalProperties = options.AdditionalProperties ?? EmptyReadOnlyDictionary<string, object?>.Instance;
        }

        public ReflectionAIFunctionDescriptor FunctionDescriptor { get; }
        public object? Target { get; }
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        public Type? TargetType { get; }
        public override IReadOnlyDictionary<string, object?> AdditionalProperties { get; }
        public override string Name => FunctionDescriptor.Name;
        public override string Description => FunctionDescriptor.Description;
        public override MethodInfo UnderlyingMethod => FunctionDescriptor.Method;
        public override JsonElement JsonSchema => FunctionDescriptor.JsonSchema;
        public override JsonSerializerOptions JsonSerializerOptions => FunctionDescriptor.JsonSerializerOptions;

        protected override async ValueTask<object?> InvokeCoreAsync(
            AIFunctionArguments arguments,
            CancellationToken cancellationToken)
        {
            bool disposeTarget = false;
            object? target = Target;
            try
            {
                if (TargetType is { } targetType)
                {
                    Debug.Assert(target is null, "Expected target to be null when we have a non-null target type");
                    Debug.Assert(!FunctionDescriptor.Method.IsStatic, "Expected an instance method");

                    target = arguments.Services is { } services ?
                        ActivatorUtilities.CreateInstance(services, targetType!) :
                        Activator.CreateInstance(targetType);
                    disposeTarget = true;
                }

                var paramMarshallers = FunctionDescriptor.ParameterMarshallers;
                object?[] args = paramMarshallers.Length != 0 ? new object?[paramMarshallers.Length] : [];

                for (int i = 0; i < args.Length; i++)
                {
                    args[i] = paramMarshallers[i](arguments, cancellationToken);
                }

                return await FunctionDescriptor.ReturnParameterMarshaller(
                    ReflectionInvoke(FunctionDescriptor.Method, target, args), cancellationToken);
            }
            finally
            {
                if (disposeTarget)
                {
                    if (target is IAsyncDisposable ad)
                    {
                        await ad.DisposeAsync();
                    }
                    else if (target is IDisposable d)
                    {
                        d.Dispose();
                    }
                }
            }
        }
    }

    /// <summary>
    /// A descriptor for a .NET method-backed AIFunction that precomputes its marshalling delegates and JSON schema.
    /// </summary>
    private sealed class ReflectionAIFunctionDescriptor
    {
        private const int InnerCacheSoftLimit = 512;
        private static readonly ConditionalWeakTable<JsonSerializerOptions, ConcurrentDictionary<DescriptorKey, ReflectionAIFunctionDescriptor>> _descriptorCache = new();

        /// <summary>A boxed <see cref="CancellationToken.None"/>.</summary>
        private static readonly object? _boxedDefaultCancellationToken = default(CancellationToken);

        /// <summary>
        /// Gets or creates a descriptors using the specified method and options.
        /// </summary>
        public static ReflectionAIFunctionDescriptor GetOrCreate(MethodInfo method, AIFunctionFactoryOptions options)
        {
            JsonSerializerOptions serializerOptions = options.SerializerOptions ?? AIJsonUtilities.DefaultOptions;
            AIJsonSchemaCreateOptions schemaOptions = options.JsonSchemaCreateOptions ?? AIJsonSchemaCreateOptions.Default;
            serializerOptions.MakeReadOnly();
            ConcurrentDictionary<DescriptorKey, ReflectionAIFunctionDescriptor> innerCache = _descriptorCache.GetOrCreateValue(serializerOptions);

            DescriptorKey key = new(method, options.Name, options.Description, options.ConfigureParameterBinding, options.MarshalResult, options.Services, schemaOptions);
            if (innerCache.TryGetValue(key, out ReflectionAIFunctionDescriptor? descriptor))
            {
                return descriptor;
            }

            descriptor = new(key, serializerOptions);
            return innerCache.Count < InnerCacheSoftLimit
                ? innerCache.GetOrAdd(key, descriptor)
                : descriptor;
        }

        private ReflectionAIFunctionDescriptor(DescriptorKey key, JsonSerializerOptions serializerOptions)
        {
            ParameterInfo[] parameters = key.Method.GetParameters();

            // Determine how each parameter should be bound.
            Dictionary<ParameterInfo, AIFunctionFactoryOptions.ParameterBindingOptions>? boundParameters = null;
            if (parameters.Length != 0 && key.GetBindParameterOptions is not null)
            {
                boundParameters = new(parameters.Length);
                for (int i = 0; i < parameters.Length; i++)
                {
                    boundParameters[parameters[i]] = key.GetBindParameterOptions(parameters[i]);
                }
            }

            IServiceProviderIsService? serviceProviderIsService = key.Services?.GetService<IServiceProviderIsService>();

            // Use that binding information to impact the schema generation.
            AIJsonSchemaCreateOptions schemaOptions = key.SchemaOptions with
            {
                IncludeParameter = parameterInfo =>
                {
                    // AIFunctionArguments and IServiceProvider parameters are always excluded from the schema.
                    if (parameterInfo.ParameterType == typeof(AIFunctionArguments) ||
                        parameterInfo.ParameterType == typeof(IServiceProvider))
                    {
                        return false;
                    }

                    // If the parameter is marked as excluded by GetBindParameterOptions, exclude it.
                    if (boundParameters?.TryGetValue(parameterInfo, out var options) is true &&
                        options.ExcludeFromSchema)
                    {
                        return false;
                    }

                    // If the parameter is attributed as [FromKeyedServices], exclude it, as we'll instead
                    // get its value from the IServiceProvider.
                    if (parameterInfo.GetCustomAttribute<FromKeyedServicesAttribute>(inherit: true) is not null)
                    {
                        return false;
                    }

                    // We assume that if the services used to create the function support a particular type,
                    // so too do the services that will be passed into InvokeAsync. This is the same basic assumption
                    // made in ASP.NET.
                    if (serviceProviderIsService?.IsService(parameterInfo.ParameterType) is true)
                    {
                        return false;
                    }

                    // If there was an existing IncludeParameter delegate, now defer to it as we've
                    // excluded everything we need to exclude.
                    if (key.SchemaOptions.IncludeParameter is { } existingIncludeParameter)
                    {
                        return existingIncludeParameter(parameterInfo);
                    }

                    // Everything else is included.
                    return true;
                },
            };

            // Get marshaling delegates for parameters.
            ParameterMarshallers = parameters.Length > 0 ? new Func<AIFunctionArguments, CancellationToken, object?>[parameters.Length] : [];
            for (int i = 0; i < parameters.Length; i++)
            {
                if (boundParameters?.TryGetValue(parameters[i], out AIFunctionFactoryOptions.ParameterBindingOptions options) is not true)
                {
                    options = default;
                }

                ParameterMarshallers[i] = GetParameterMarshaller(serializerOptions, options, parameters[i], serviceProviderIsService);
            }

            // Get a marshaling delegate for the return value.
            ReturnParameterMarshaller = GetReturnParameterMarshaller(key, serializerOptions);

            Method = key.Method;
            Name = key.Name ?? GetFunctionName(key.Method);
            Description = key.Description ?? key.Method.GetCustomAttribute<DescriptionAttribute>(inherit: true)?.Description ?? string.Empty;
            JsonSerializerOptions = serializerOptions;
            JsonSchema = AIJsonUtilities.CreateFunctionJsonSchema(
                key.Method,
                Name,
                Description,
                serializerOptions,
                schemaOptions);
        }

        public string Name { get; }
        public string Description { get; }
        public MethodInfo Method { get; }
        public JsonSerializerOptions JsonSerializerOptions { get; }
        public JsonElement JsonSchema { get; }
        public Func<AIFunctionArguments, CancellationToken, object?>[] ParameterMarshallers { get; }
        public Func<object?, CancellationToken, ValueTask<object?>> ReturnParameterMarshaller { get; }
        public ReflectionAIFunction? CachedDefaultInstance { get; set; }

        private static string GetFunctionName(MethodInfo method)
        {
            // Get the function name to use.
            string name = SanitizeMemberName(method.Name);

            const string AsyncSuffix = "Async";
            if (IsAsyncMethod(method) &&
                name.EndsWith(AsyncSuffix, StringComparison.Ordinal) &&
                name.Length > AsyncSuffix.Length)
            {
                name = name.Substring(0, name.Length - AsyncSuffix.Length);
            }

            return name;

            static bool IsAsyncMethod(MethodInfo method)
            {
                Type t = method.ReturnType;

                if (t == typeof(Task) || t == typeof(ValueTask))
                {
                    return true;
                }

                if (t.IsGenericType)
                {
                    t = t.GetGenericTypeDefinition();
                    if (t == typeof(Task<>) || t == typeof(ValueTask<>) || t == typeof(IAsyncEnumerable<>))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets a delegate for handling the marshaling of a parameter.
        /// </summary>
        private static Func<AIFunctionArguments, CancellationToken, object?> GetParameterMarshaller(
            JsonSerializerOptions serializerOptions,
            AIFunctionFactoryOptions.ParameterBindingOptions bindingOptions,
            ParameterInfo parameter,
            IServiceProviderIsService? serviceProviderIsService)
        {
            if (string.IsNullOrWhiteSpace(parameter.Name))
            {
                Throw.ArgumentException(nameof(parameter), "Parameter is missing a name.");
            }

            Type parameterType = parameter.ParameterType;

            // For CancellationToken parameters, we always bind to the token passed directly to InvokeAsync.
            if (parameterType == typeof(CancellationToken))
            {
                return static (_, cancellationToken) =>
                    cancellationToken == default ? _boxedDefaultCancellationToken : // optimize common case of a default CT to avoid boxing
                    cancellationToken;
            }

            // CancellationToken is the only parameter type that's handled exclusively by the implementation.
            // Now that it's been processed, check to see if the parameter should be handled via BindParameter.
            if (bindingOptions.BindParameter is { } bindParameter)
            {
                return (arguments, _) => bindParameter(parameter, arguments);
            }

            // We're now into default handling of everything else.

            // For AIFunctionArgument parameters, we bind to the arguments passed to InvokeAsync.
            if (parameterType == typeof(AIFunctionArguments))
            {
                return static (arguments, _) => arguments;
            }

            // For IServiceProvider parameters, we bind to the services passed to InvokeAsync via AIFunctionArguments.
            if (parameterType == typeof(IServiceProvider))
            {
                return (arguments, _) =>
                {
                    IServiceProvider? services = arguments.Services;
                    if (!parameter.HasDefaultValue && services is null)
                    {
                        ThrowNullServices(parameter.Name);
                    }

                    return services;
                };
            }

            // For [FromKeyedServices] parameters, we resolve from the services passed to InvokeAsync via AIFunctionArguments.
            if (parameter.GetCustomAttribute<FromKeyedServicesAttribute>(inherit: true) is { } keyedAttr)
            {
                return (arguments, _) =>
                {
                    if ((arguments.Services as IKeyedServiceProvider)?.GetKeyedService(parameterType, keyedAttr.Key) is { } service)
                    {
                        return service;
                    }

                    if (!parameter.HasDefaultValue)
                    {
                        if (arguments.Services is null)
                        {
                            ThrowNullServices(parameter.Name);
                        }

                        Throw.ArgumentException(nameof(arguments), $"No service of type '{parameterType}' with key '{keyedAttr.Key}' was found for parameter '{parameter.Name}'.");
                    }

                    return parameter.DefaultValue;
                };
            }

            // For any parameters that are satisfiable from the IServiceProvider, we resolve from the services passed to InvokeAsync
            // via AIFunctionArguments. This is determined by the same same IServiceProviderIsService instance used to determine whether
            // the parameter should be included in the schema.
            if (serviceProviderIsService?.IsService(parameterType) is true)
            {
                return (arguments, _) =>
                {
                    if (arguments.Services?.GetService(parameterType) is { } service)
                    {
                        return service;
                    }

                    if (!parameter.HasDefaultValue)
                    {
                        if (arguments.Services is null)
                        {
                            ThrowNullServices(parameter.Name);
                        }

                        Throw.ArgumentException(nameof(arguments), $"No service of type '{parameterType}' was found for parameter '{parameter.Name}'.");
                    }

                    return parameter.DefaultValue;
                };
            }

            // For all other parameters, create a marshaller that tries to extract the value from the arguments dictionary.
            // Resolve the contract used to marshal the value from JSON -- can throw if not supported or not found.
            JsonTypeInfo? typeInfo = serializerOptions.GetTypeInfo(parameterType);
            return (arguments, _) =>
            {
                // If the parameter has an argument specified in the dictionary, return that argument.
                if (arguments.TryGetValue(parameter.Name, out object? value))
                {
                    return value switch
                    {
                        null => null, // Return as-is if null -- if the parameter is a struct this will be handled by MethodInfo.Invoke
                        _ when parameterType.IsInstanceOfType(value) => value, // Do nothing if value is assignable to parameter type
                        JsonElement element => JsonSerializer.Deserialize(element, typeInfo),
                        JsonDocument doc => JsonSerializer.Deserialize(doc, typeInfo),
                        JsonNode node => JsonSerializer.Deserialize(node, typeInfo),
                        _ => MarshallViaJsonRoundtrip(value),
                    };

                    object? MarshallViaJsonRoundtrip(object value)
                    {
                        try
                        {
                            string json = JsonSerializer.Serialize(value, serializerOptions.GetTypeInfo(value.GetType()));
                            return JsonSerializer.Deserialize(json, typeInfo);
                        }
                        catch
                        {
                            // Eat any exceptions and fall back to the original value to force a cast exception later on.
                            return value;
                        }
                    }
                }

                // If the parameter is required and there's no argument specified for it, throw.
                if (!parameter.HasDefaultValue)
                {
                    Throw.ArgumentException(nameof(arguments), $"The arguments dictionary is missing a value for the required parameter '{parameter.Name}'.");
                }

                // Otherwise, use the optional parameter's default value.
                return parameter.DefaultValue;
            };

            // Throws an ArgumentNullException indicating that AIFunctionArguments.Services must be provided.
            static void ThrowNullServices(string parameterName) =>
                Throw.ArgumentNullException($"arguments.{nameof(AIFunctionArguments.Services)}", $"Services are required for parameter '{parameterName}'.");
        }

        /// <summary>
        /// Gets a delegate for handling the result value of a method, converting it into the <see cref="Task{FunctionResult}"/> to return from the invocation.
        /// </summary>
        private static Func<object?, CancellationToken, ValueTask<object?>> GetReturnParameterMarshaller(
            DescriptorKey key, JsonSerializerOptions serializerOptions)
        {
            Type returnType = key.Method.ReturnType;
            JsonTypeInfo returnTypeInfo;
            Func<object?, Type?, CancellationToken, ValueTask<object?>>? marshalResult = key.MarshalResult;

            // Void
            if (returnType == typeof(void))
            {
                if (marshalResult is not null)
                {
                    return (result, cancellationToken) => marshalResult(null, null, cancellationToken);
                }

                return static (_, _) => new ValueTask<object?>((object?)null);
            }

            // Task
            if (returnType == typeof(Task))
            {
                if (marshalResult is not null)
                {
                    return async (result, cancellationToken) =>
                    {
                        await ((Task)ThrowIfNullResult(result));
                        return await marshalResult(null, null, cancellationToken);
                    };
                }

                return async static (result, _) =>
                {
                    await ((Task)ThrowIfNullResult(result));
                    return null;
                };
            }

            // ValueTask
            if (returnType == typeof(ValueTask))
            {
                if (marshalResult is not null)
                {
                    return async (result, cancellationToken) =>
                    {
                        await ((ValueTask)ThrowIfNullResult(result));
                        return await marshalResult(null, null, cancellationToken);
                    };
                }

                return async static (result, _) =>
                {
                    await ((ValueTask)ThrowIfNullResult(result));
                    return null;
                };
            }

            if (returnType.IsGenericType)
            {
                // Task<T>
                if (returnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    MethodInfo taskResultGetter = GetMethodFromGenericMethodDefinition(returnType, _taskGetResult);
                    if (marshalResult is not null)
                    {
                        return async (taskObj, cancellationToken) =>
                        {
                            await ((Task)ThrowIfNullResult(taskObj));
                            object? result = ReflectionInvoke(taskResultGetter, taskObj, null);
                            return await marshalResult(result, taskResultGetter.ReturnType, cancellationToken);
                        };
                    }

                    returnTypeInfo = serializerOptions.GetTypeInfo(taskResultGetter.ReturnType);
                    return async (taskObj, cancellationToken) =>
                    {
                        await ((Task)ThrowIfNullResult(taskObj));
                        object? result = ReflectionInvoke(taskResultGetter, taskObj, null);
                        return await SerializeResultAsync(result, returnTypeInfo, cancellationToken);
                    };
                }

                // ValueTask<T>
                if (returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
                {
                    MethodInfo valueTaskAsTask = GetMethodFromGenericMethodDefinition(returnType, _valueTaskAsTask);
                    MethodInfo asTaskResultGetter = GetMethodFromGenericMethodDefinition(valueTaskAsTask.ReturnType, _taskGetResult);

                    if (marshalResult is not null)
                    {
                        return async (taskObj, cancellationToken) =>
                        {
                            var task = (Task)ReflectionInvoke(valueTaskAsTask, ThrowIfNullResult(taskObj), null)!;
                            await task;
                            object? result = ReflectionInvoke(asTaskResultGetter, task, null);
                            return await marshalResult(result, asTaskResultGetter.ReturnType, cancellationToken);
                        };
                    }

                    returnTypeInfo = serializerOptions.GetTypeInfo(asTaskResultGetter.ReturnType);
                    return async (taskObj, cancellationToken) =>
                    {
                        var task = (Task)ReflectionInvoke(valueTaskAsTask, ThrowIfNullResult(taskObj), null)!;
                        await task;
                        object? result = ReflectionInvoke(asTaskResultGetter, task, null);
                        return await SerializeResultAsync(result, returnTypeInfo, cancellationToken);
                    };
                }
            }

            // For everything else, just serialize the result as-is.
            if (marshalResult is not null)
            {
                return (result, cancellationToken) => marshalResult(result, returnType, cancellationToken);
            }

            returnTypeInfo = serializerOptions.GetTypeInfo(returnType);
            return (result, cancellationToken) => SerializeResultAsync(result, returnTypeInfo, cancellationToken);

            static async ValueTask<object?> SerializeResultAsync(object? result, JsonTypeInfo returnTypeInfo, CancellationToken cancellationToken)
            {
                if (returnTypeInfo.Kind is JsonTypeInfoKind.None)
                {
                    // Special-case trivial contracts to avoid the more expensive general-purpose serialization path.
                    return JsonSerializer.SerializeToElement(result, returnTypeInfo);
                }

                // Serialize asynchronously to support potential IAsyncEnumerable responses.
                using PooledMemoryStream stream = new();
                await JsonSerializer.SerializeAsync(stream, result, returnTypeInfo, cancellationToken);
                Utf8JsonReader reader = new(stream.GetBuffer());
                return JsonElement.ParseValue(ref reader);
            }

            // Throws an exception if a result is found to be null unexpectedly
            static object ThrowIfNullResult(object? result) => result ?? throw new InvalidOperationException("Function returned null unexpectedly.");
        }

        private static readonly MethodInfo _taskGetResult = typeof(Task<>).GetProperty(nameof(Task<int>.Result), BindingFlags.Instance | BindingFlags.Public)!.GetMethod!;
        private static readonly MethodInfo _valueTaskAsTask = typeof(ValueTask<>).GetMethod(nameof(ValueTask<int>.AsTask), BindingFlags.Instance | BindingFlags.Public)!;

        private static MethodInfo GetMethodFromGenericMethodDefinition(Type specializedType, MethodInfo genericMethodDefinition)
        {
            Debug.Assert(specializedType.IsGenericType && specializedType.GetGenericTypeDefinition() == genericMethodDefinition.DeclaringType, "generic member definition doesn't match type.");
#if NET
            return (MethodInfo)specializedType.GetMemberWithSameMetadataDefinitionAs(genericMethodDefinition);
#else
            const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            return specializedType.GetMethods(All).First(m => m.MetadataToken == genericMethodDefinition.MetadataToken);
#endif
        }

        private record struct DescriptorKey(
            MethodInfo Method,
            string? Name,
            string? Description,
            Func<ParameterInfo, AIFunctionFactoryOptions.ParameterBindingOptions>? GetBindParameterOptions,
            Func<object?, Type?, CancellationToken, ValueTask<object?>>? MarshalResult,
            IServiceProvider? Services,
            AIJsonSchemaCreateOptions SchemaOptions);
    }
}
