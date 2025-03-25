﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
    /// Return values are serialized to <see cref="JsonElement"/> using <paramref name="options"/>'s
    /// <see cref="AIFunctionFactoryOptions.SerializerOptions"/>. Arguments that are not already of the expected type are
    /// marshaled to the expected type via JSON and using <paramref name="options"/>'s
    /// <see cref="AIFunctionFactoryOptions.SerializerOptions"/>. If the argument is a <see cref="JsonElement"/>,
    /// <see cref="JsonDocument"/>, or <see cref="JsonNode"/>, it is deserialized directly. If the argument is anything else unknown,
    /// it is round-tripped through JSON, serializing the object as JSON and then deserializing it to the expected type.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="method"/> is <see langword="null"/>.</exception>
    public static AIFunction Create(Delegate method, AIFunctionFactoryOptions? options)
    {
        _ = Throw.IfNull(method);

        return ReflectionAIFunction.Build(method.Method, method.Target, options ?? _defaultOptions);
    }

    /// <summary>Creates an <see cref="AIFunction"/> instance for a method, specified via a delegate.</summary>
    /// <param name="method">The method to be represented via the created <see cref="AIFunction"/>.</param>
    /// <param name="name">The name to use for the <see cref="AIFunction"/>.</param>
    /// <param name="description">The description to use for the <see cref="AIFunction"/>.</param>
    /// <param name="serializerOptions">The <see cref="JsonSerializerOptions"/> used to marshal function parameters and any return value.</param>
    /// <returns>The created <see cref="AIFunction"/> for invoking <paramref name="method"/>.</returns>
    /// <remarks>
    /// <para>
    /// Return values are serialized to <see cref="JsonElement"/> using <paramref name="serializerOptions"/>.
    /// Arguments that are not already of the expected type are marshaled to the expected type via JSON and using
    /// <paramref name="serializerOptions"/>. If the argument is a <see cref="JsonElement"/>, <see cref="JsonDocument"/>,
    /// or <see cref="JsonNode"/>, it is deserialized directly. If the argument is anything else unknown, it is
    /// round-tripped through JSON, serializing the object as JSON and then deserializing it to the expected type.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="method"/> is <see langword="null"/>.</exception>
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
    /// Return values are serialized to <see cref="JsonElement"/> using <paramref name="options"/>'s
    /// <see cref="AIFunctionFactoryOptions.SerializerOptions"/>. Arguments that are not already of the expected type are
    /// marshaled to the expected type via JSON and using <paramref name="options"/>'s
    /// <see cref="AIFunctionFactoryOptions.SerializerOptions"/>. If the argument is a <see cref="JsonElement"/>,
    /// <see cref="JsonDocument"/>, or <see cref="JsonNode"/>, it is deserialized directly. If the argument is anything else unknown,
    /// it is round-tripped through JSON, serializing the object as JSON and then deserializing it to the expected type.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="method"/> is <see langword="null"/>.</exception>
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
    /// <param name="name">The name to use for the <see cref="AIFunction"/>.</param>
    /// <param name="description">The description to use for the <see cref="AIFunction"/>.</param>
    /// <param name="serializerOptions">The <see cref="JsonSerializerOptions"/> used to marshal function parameters and return value.</param>
    /// <returns>The created <see cref="AIFunction"/> for invoking <paramref name="method"/>.</returns>
    /// <remarks>
    /// <para>
    /// Return values are serialized to <see cref="JsonElement"/> using <paramref name="serializerOptions"/>.
    /// Arguments that are not already of the expected type are marshaled to the expected type via JSON and using
    /// <paramref name="serializerOptions"/>. If the argument is a <see cref="JsonElement"/>, <see cref="JsonDocument"/>,
    /// or <see cref="JsonNode"/>, it is deserialized directly. If the argument is anything else unknown, it is
    /// round-tripped through JSON, serializing the object as JSON and then deserializing it to the expected type.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="method"/> is <see langword="null"/>.</exception>
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

        private ReflectionAIFunction(ReflectionAIFunctionDescriptor functionDescriptor, object? target, AIFunctionFactoryOptions options)
        {
            FunctionDescriptor = functionDescriptor;
            Target = target;
            AdditionalProperties = options.AdditionalProperties ?? EmptyReadOnlyDictionary<string, object?>.Instance;
        }

        public ReflectionAIFunctionDescriptor FunctionDescriptor { get; }
        public object? Target { get; }
        public override IReadOnlyDictionary<string, object?> AdditionalProperties { get; }
        public override string Name => FunctionDescriptor.Name;
        public override string Description => FunctionDescriptor.Description;
        public override MethodInfo UnderlyingMethod => FunctionDescriptor.Method;
        public override JsonElement JsonSchema => FunctionDescriptor.JsonSchema;
        public override JsonSerializerOptions JsonSerializerOptions => FunctionDescriptor.JsonSerializerOptions;

        protected override ValueTask<object?> InvokeCoreAsync(
            AIFunctionArguments arguments,
            CancellationToken cancellationToken)
        {
            var paramMarshallers = FunctionDescriptor.ParameterMarshallers;
            object?[] args = paramMarshallers.Length != 0 ? new object?[paramMarshallers.Length] : [];

            for (int i = 0; i < args.Length; i++)
            {
                args[i] = paramMarshallers[i](arguments, cancellationToken);
            }

            return FunctionDescriptor.ReturnParameterMarshaller(ReflectionInvoke(FunctionDescriptor.Method, Target, args), cancellationToken);
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

            DescriptorKey key = new(method, options.Name, options.Description, options.ConfigureParameterBinding, options.MarshalResult, schemaOptions);
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

                ParameterMarshallers[i] = GetParameterMarshaller(serializerOptions, options, parameters[i]);
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
            ParameterInfo parameter)
        {
            if (string.IsNullOrWhiteSpace(parameter.Name))
            {
                Throw.ArgumentException(nameof(parameter), "Parameter is missing a name.");
            }

            // Resolve the contract used to marshal the value from JSON -- can throw if not supported or not found.
            Type parameterType = parameter.ParameterType;
            JsonTypeInfo typeInfo = serializerOptions.GetTypeInfo(parameterType);

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

            // For AIFunctionArgument parameters, we bind to the arguments passed directly to InvokeAsync.
            if (parameterType == typeof(AIFunctionArguments))
            {
                return static (arguments, _) => arguments;
            }

            // For IServiceProvider parameters, we bind to the services passed directly to InvokeAsync via AIFunctionArguments.
            if (parameterType == typeof(IServiceProvider))
            {
                return (arguments, _) =>
                {
                    IServiceProvider? services = arguments.Services;
                    if (services is null && !parameter.HasDefaultValue)
                    {
                        Throw.ArgumentException(nameof(arguments), $"An {nameof(IServiceProvider)} was not provided for the {parameter.Name} parameter.");
                    }

                    return services;
                };
            }

            // For all other parameters, create a marshaller that tries to extract the value from the arguments dictionary.
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
                    Throw.ArgumentException(nameof(arguments), $"Missing required parameter '{parameter.Name}' for method '{parameter.Member.Name}'.");
                }

                // Otherwise, use the optional parameter's default value.
                return parameter.DefaultValue;
            };
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
                        await ((Task)ThrowIfNullResult(result)).ConfigureAwait(false);
                        return await marshalResult(null, null, cancellationToken).ConfigureAwait(false);
                    };
                }

                return async static (result, _) =>
                {
                    await ((Task)ThrowIfNullResult(result)).ConfigureAwait(false);
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
                        await ((ValueTask)ThrowIfNullResult(result)).ConfigureAwait(false);
                        return await marshalResult(null, null, cancellationToken).ConfigureAwait(false);
                    };
                }

                return async static (result, _) =>
                {
                    await ((ValueTask)ThrowIfNullResult(result)).ConfigureAwait(false);
                    return null;
                };
            }

            if (returnType.IsGenericType)
            {
                // Task<T>
                if (returnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    MethodInfo taskResultGetter = GetMethodFromGenericMethodDefinition(returnType, _taskGetResult);
                    returnTypeInfo = serializerOptions.GetTypeInfo(taskResultGetter.ReturnType);
                    return async (taskObj, cancellationToken) =>
                    {
                        await ((Task)ThrowIfNullResult(taskObj)).ConfigureAwait(false);
                        object? result = ReflectionInvoke(taskResultGetter, taskObj, null);
                        return marshalResult is not null ?
                            await marshalResult(result, returnTypeInfo.Type, cancellationToken).ConfigureAwait(false) :
                            await SerializeResultAsync(result, returnTypeInfo, cancellationToken).ConfigureAwait(false);
                    };
                }

                // ValueTask<T>
                if (returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
                {
                    MethodInfo valueTaskAsTask = GetMethodFromGenericMethodDefinition(returnType, _valueTaskAsTask);
                    MethodInfo asTaskResultGetter = GetMethodFromGenericMethodDefinition(valueTaskAsTask.ReturnType, _taskGetResult);
                    returnTypeInfo = serializerOptions.GetTypeInfo(asTaskResultGetter.ReturnType);
                    return async (taskObj, cancellationToken) =>
                    {
                        var task = (Task)ReflectionInvoke(valueTaskAsTask, ThrowIfNullResult(taskObj), null)!;
                        await task.ConfigureAwait(false);
                        object? result = ReflectionInvoke(asTaskResultGetter, task, null);
                        return marshalResult is not null ?
                            await marshalResult(result, returnTypeInfo.Type, cancellationToken).ConfigureAwait(false) :
                            await SerializeResultAsync(result, returnTypeInfo, cancellationToken).ConfigureAwait(false);
                    };
                }
            }

            // For everything else, just serialize the result as-is.
            returnTypeInfo = serializerOptions.GetTypeInfo(returnType);
            return marshalResult is not null ?
                (result, cancellationToken) => marshalResult(result, returnTypeInfo.Type, cancellationToken) :
                (result, cancellationToken) => SerializeResultAsync(result, returnTypeInfo, cancellationToken);

            static async ValueTask<object?> SerializeResultAsync(object? result, JsonTypeInfo returnTypeInfo, CancellationToken cancellationToken)
            {
                if (returnTypeInfo.Kind is JsonTypeInfoKind.None)
                {
                    // Special-case trivial contracts to avoid the more expensive general-purpose serialization path.
                    return JsonSerializer.SerializeToElement(result, returnTypeInfo);
                }

                // Serialize asynchronously to support potential IAsyncEnumerable responses.
                using PooledMemoryStream stream = new();
                await JsonSerializer.SerializeAsync(stream, result, returnTypeInfo, cancellationToken).ConfigureAwait(false);
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
            AIJsonSchemaCreateOptions SchemaOptions);
    }
}
