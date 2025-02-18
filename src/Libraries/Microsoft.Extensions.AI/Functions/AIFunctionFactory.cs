// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Collections;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides factory methods for creating commonly used implementations of <see cref="AIFunction"/>.</summary>
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

            ReflectionAIFunctionDescriptor functionDescriptor = ReflectionAIFunctionDescriptor.GetOrCreate(method, options);

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
        public override string Name => FunctionDescriptor.Name;
        public override string Description => FunctionDescriptor.Description;
        public override MethodInfo UnderlyingMethod => FunctionDescriptor.Method;
        public override JsonElement JsonSchema => FunctionDescriptor.JsonSchema;
        public override JsonSerializerOptions JsonSerializerOptions => FunctionDescriptor.JsonSerializerOptions;
        public override IReadOnlyDictionary<string, object?> AdditionalProperties { get; }
        protected override async Task<object?> InvokeCoreAsync(
            IEnumerable<KeyValuePair<string, object?>>? arguments,
            CancellationToken cancellationToken)
        {
            var paramMarshallers = FunctionDescriptor.ParameterMarshallers;
            object?[] args = paramMarshallers.Length != 0 ? new object?[paramMarshallers.Length] : [];

            IReadOnlyDictionary<string, object?> argDict =
                arguments is null || args.Length == 0 ? EmptyReadOnlyDictionary<string, object?>.Instance :
                arguments as IReadOnlyDictionary<string, object?> ??
                arguments.
#if NET8_0_OR_GREATER
                    ToDictionary();
#else
                    ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
#endif
            AIFunctionContext? context = FunctionDescriptor.RequiresAIFunctionContext ?
                new() { CancellationToken = cancellationToken } :
                null;

            for (int i = 0; i < args.Length; i++)
            {
                args[i] = paramMarshallers[i](argDict, context);
            }

            return await FunctionDescriptor.ReturnParameterMarshaller(ReflectionInvoke(FunctionDescriptor.Method, Target, args), cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// A descriptor for a .NET method-backed AIFunction that precomputes its marshalling delegates and JSON schema.
    /// </summary>
    private sealed class ReflectionAIFunctionDescriptor
    {
        private const int InnerCacheSoftLimit = 512;
        private static readonly ConditionalWeakTable<JsonSerializerOptions, ConcurrentDictionary<DescriptorKey, ReflectionAIFunctionDescriptor>> _descriptorCache = new();

        /// <summary>
        /// Gets or creates a descriptors using the specified method and options.
        /// </summary>
        public static ReflectionAIFunctionDescriptor GetOrCreate(MethodInfo method, AIFunctionFactoryOptions options)
        {
            JsonSerializerOptions serializerOptions = options.SerializerOptions ?? AIJsonUtilities.DefaultOptions;
            AIJsonSchemaCreateOptions schemaOptions = options.JsonSchemaCreateOptions ?? AIJsonSchemaCreateOptions.Default;
            serializerOptions.MakeReadOnly();
            ConcurrentDictionary<DescriptorKey, ReflectionAIFunctionDescriptor> innerCache = _descriptorCache.GetOrCreateValue(serializerOptions);

            DescriptorKey key = new(method, options.Name, options.Description, schemaOptions);
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
            // Get marshaling delegates for parameters.
            ParameterInfo[] parameters = key.Method.GetParameters();
            ParameterMarshallers = new Func<IReadOnlyDictionary<string, object?>, AIFunctionContext?, object?>[parameters.Length];
            bool foundAIFunctionContextParameter = false;
            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterMarshallers[i] = GetParameterMarshaller(serializerOptions, parameters[i], ref foundAIFunctionContextParameter);
            }

            // Get a marshaling delegate for the return value.
            ReturnParameterMarshaller = GetReturnParameterMarshaller(key.Method, serializerOptions);

            Method = key.Method;
            Name = key.Name ?? GetFunctionName(key.Method);
            Description = key.Description ?? key.Method.GetCustomAttribute<DescriptionAttribute>(inherit: true)?.Description ?? string.Empty;
            RequiresAIFunctionContext = foundAIFunctionContextParameter;
            JsonSerializerOptions = serializerOptions;
            JsonSchema = AIJsonUtilities.CreateFunctionJsonSchema(
                key.Method,
                Name,
                Description,
                serializerOptions,
                key.SchemaOptions);
        }

        public string Name { get; }
        public string Description { get; }
        public MethodInfo Method { get; }
        public JsonSerializerOptions JsonSerializerOptions { get; }
        public JsonElement JsonSchema { get; }
        public Func<IReadOnlyDictionary<string, object?>, AIFunctionContext?, object?>[] ParameterMarshallers { get; }
        public Func<object?, CancellationToken, ValueTask<object?>> ReturnParameterMarshaller { get; }
        public bool RequiresAIFunctionContext { get; }
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
        private static Func<IReadOnlyDictionary<string, object?>, AIFunctionContext?, object?> GetParameterMarshaller(
            JsonSerializerOptions serializerOptions,
            ParameterInfo parameter,
            ref bool foundAIFunctionContextParameter)
        {
            if (string.IsNullOrWhiteSpace(parameter.Name))
            {
                Throw.ArgumentException(nameof(parameter), "Parameter is missing a name.");
            }

            // Special-case an AIFunctionContext parameter.
            if (parameter.ParameterType == typeof(AIFunctionContext))
            {
                if (foundAIFunctionContextParameter)
                {
                    Throw.ArgumentException(nameof(parameter), $"Only one {nameof(AIFunctionContext)} parameter is permitted.");
                }

                foundAIFunctionContextParameter = true;

                return static (_, ctx) =>
                {
                    Debug.Assert(ctx is not null, "Expected a non-null context object.");
                    return ctx;
                };
            }

            // Resolve the contract used to marshal the value from JSON -- can throw if not supported or not found.
            Type parameterType = parameter.ParameterType;
            JsonTypeInfo typeInfo = serializerOptions.GetTypeInfo(parameterType);

            // Create a marshaller that simply looks up the parameter by name in the arguments dictionary.
            return (IReadOnlyDictionary<string, object?> arguments, AIFunctionContext? _) =>
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
#pragma warning disable CA1031 // Do not catch general exception types
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
#pragma warning restore CA1031 // Do not catch general exception types
                    }
                }

                // There was no argument for the parameter. Try to use a default value.
                if (parameter.HasDefaultValue)
                {
                    return parameter.DefaultValue;
                }

                // No default either. Leave it empty.
                return null;
            };
        }

        /// <summary>
        /// Gets a delegate for handling the result value of a method, converting it into the <see cref="Task{FunctionResult}"/> to return from the invocation.
        /// </summary>
        private static Func<object?, CancellationToken, ValueTask<object?>> GetReturnParameterMarshaller(MethodInfo method, JsonSerializerOptions serializerOptions)
        {
            Type returnType = method.ReturnType;
            JsonTypeInfo returnTypeInfo;

            // Void
            if (returnType == typeof(void))
            {
                return static (_, _) => default;
            }

            // Task
            if (returnType == typeof(Task))
            {
                return async static (result, _) =>
                {
                    await ((Task)ThrowIfNullResult(result)).ConfigureAwait(false);
                    return null;
                };
            }

            // ValueTask
            if (returnType == typeof(ValueTask))
            {
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
                        return await SerializeAsync(result, returnTypeInfo, cancellationToken).ConfigureAwait(false);
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
                        return await SerializeAsync(result, returnTypeInfo, cancellationToken).ConfigureAwait(false);
                    };
                }
            }

            // For everything else, just serialize the result as-is.
            returnTypeInfo = serializerOptions.GetTypeInfo(returnType);
            return (result, cancellationToken) => SerializeAsync(result, returnTypeInfo, cancellationToken);

            static async ValueTask<object?> SerializeAsync(object? result, JsonTypeInfo returnTypeInfo, CancellationToken cancellationToken)
            {
                if (returnTypeInfo.Kind is JsonTypeInfoKind.None)
                {
                    // Special-case trivial contracts to avoid the more expensive general-purpose serialization path.
                    return JsonSerializer.SerializeToElement(result, returnTypeInfo);
                }

                // Serialize asynchronously to support potential IAsyncEnumerable responses.
                using MemoryStream stream = new();
                await JsonSerializer.SerializeAsync(stream, result, returnTypeInfo, cancellationToken).ConfigureAwait(false);
                Utf8JsonReader reader = new(stream.GetBuffer().AsSpan(0, (int)stream.Length));
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
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
            const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
            return specializedType.GetMethods(All).First(m => m.MetadataToken == genericMethodDefinition.MetadataToken);
#endif
        }

        private record struct DescriptorKey(MethodInfo Method, string? Name, string? Description, AIJsonSchemaCreateOptions SchemaOptions);
    }
}
