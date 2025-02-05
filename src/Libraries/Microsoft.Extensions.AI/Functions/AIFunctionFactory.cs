// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
    private static readonly AIFunctionFactoryCreateOptions _defaultOptions = new();

    /// <summary>Creates an <see cref="AIFunction"/> instance for a method, specified via a delegate.</summary>
    /// <param name="method">The method to be represented via the created <see cref="AIFunction"/>.</param>
    /// <param name="options">Metadata to use to override defaults inferred from <paramref name="method"/>.</param>
    /// <returns>The created <see cref="AIFunction"/> for invoking <paramref name="method"/>.</returns>
    /// <remarks>
    /// <para>
    /// The resulting <see cref="AIFunction"/> exposes metadata about the function via <see cref="AIFunction.Metadata"/>.
    /// This metadata includes the function's name, description, and parameters. All of that information may be specified
    /// explicitly via <paramref name="options"/>; however, if not specified, defaults are inferred by examining
    /// <paramref name="method"/>. That includes examining the method and its parameters for <see cref="DescriptionAttribute"/>s.
    /// </para>
    /// <para>
    /// Return values are serialized to <see cref="JsonElement"/> using <paramref name="options"/>'s
    /// <see cref="AIFunctionFactoryCreateOptions.SerializerOptions"/>. Arguments that are not already of the expected type are
    /// marshaled to the expected type via JSON and using <paramref name="options"/>'s
    /// <see cref="AIFunctionFactoryCreateOptions.SerializerOptions"/>. If the argument is a <see cref="JsonElement"/>,
    /// <see cref="JsonDocument"/>, or <see cref="JsonNode"/>, it is deserialized directly. If the argument is anything else unknown,
    /// it is round-tripped through JSON, serializing the object as JSON and then deserializing it to the expected type.
    /// </para>
    /// </remarks>
    public static AIFunction Create(Delegate method, AIFunctionFactoryCreateOptions? options)
    {
        _ = Throw.IfNull(method);

        return new ReflectionAIFunction(method.Method, method.Target, options ?? _defaultOptions);
    }

    /// <summary>Creates an <see cref="AIFunction"/> instance for a method, specified via a delegate.</summary>
    /// <param name="method">The method to be represented via the created <see cref="AIFunction"/>.</param>
    /// <param name="name">The name to use for the <see cref="AIFunction"/>.</param>
    /// <param name="description">The description to use for the <see cref="AIFunction"/>.</param>
    /// <param name="serializerOptions">The <see cref="JsonSerializerOptions"/> used to marshal function parameters and any return value.</param>
    /// <returns>The created <see cref="AIFunction"/> for invoking <paramref name="method"/>.</returns>
    /// <remarks>
    /// <para>
    /// The resulting <see cref="AIFunction"/> exposes metadata about the function via <see cref="AIFunction.Metadata"/>.
    /// This metadata includes the function's name, description, and parameters. The function's name and description may
    /// be specified explicitly via <paramref name="name"/> and <paramref name="description"/>, but if they're not, this method
    /// will infer values from examining <paramref name="method"/>. That includes looking for <see cref="DescriptionAttribute"/>
    /// attributes on the method itself and on its parameters.
    /// </para>
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

        AIFunctionFactoryCreateOptions createOptions = serializerOptions is null && name is null && description is null
            ? _defaultOptions
            : new()
            {
                SerializerOptions = serializerOptions ?? _defaultOptions.SerializerOptions,
                Name = name,
                Description = description
            };

        return new ReflectionAIFunction(method.Method, method.Target, createOptions);
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
    /// The resulting <see cref="AIFunction"/> exposes metadata about the function via <see cref="AIFunction.Metadata"/>.
    /// This metadata includes the function's name, description, and parameters. All of that information may be specified
    /// explicitly via <paramref name="options"/>; however, if not specified, defaults are inferred by examining
    /// <paramref name="method"/>. That includes examining the method and its parameters for <see cref="DescriptionAttribute"/>s.
    /// </para>
    /// <para>
    /// Return values are serialized to <see cref="JsonElement"/> using <paramref name="options"/>'s
    /// <see cref="AIFunctionFactoryCreateOptions.SerializerOptions"/>. Arguments that are not already of the expected type are
    /// marshaled to the expected type via JSON and using <paramref name="options"/>'s
    /// <see cref="AIFunctionFactoryCreateOptions.SerializerOptions"/>. If the argument is a <see cref="JsonElement"/>,
    /// <see cref="JsonDocument"/>, or <see cref="JsonNode"/>, it is deserialized directly. If the argument is anything else unknown,
    /// it is round-tripped through JSON, serializing the object as JSON and then deserializing it to the expected type.
    /// </para>
    /// </remarks>
    public static AIFunction Create(MethodInfo method, object? target, AIFunctionFactoryCreateOptions? options)
    {
        _ = Throw.IfNull(method);
        return new ReflectionAIFunction(method, target, options ?? _defaultOptions);
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
    /// The resulting <see cref="AIFunction"/> exposes metadata about the function via <see cref="AIFunction.Metadata"/>.
    /// This metadata includes the function's name, description, and parameters. The function's name and description may
    /// be specified explicitly via <paramref name="name"/> and <paramref name="description"/>, but if they're not, this method
    /// will infer values from examining <paramref name="method"/>. That includes looking for <see cref="DescriptionAttribute"/>
    /// attributes on the method itself and on its parameters.
    /// </para>
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

        AIFunctionFactoryCreateOptions? createOptions = serializerOptions is null && name is null && description is null
            ? _defaultOptions
            : new()
            {
                SerializerOptions = serializerOptions ?? _defaultOptions.SerializerOptions,
                Name = name,
                Description = description
            };

        return new ReflectionAIFunction(method, target, createOptions);
    }

    private sealed class ReflectionAIFunction : AIFunction
    {
        private readonly MethodInfo _method;
        private readonly object? _target;
        private readonly Func<IReadOnlyDictionary<string, object?>, AIFunctionContext?, object?>[] _parameterMarshallers;
        private readonly Func<object?, ValueTask<object?>> _returnMarshaller;
        private readonly JsonTypeInfo? _returnTypeInfo;
        private readonly bool _needsAIFunctionContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReflectionAIFunction"/> class for a method, specified via an <see cref="MethodInfo"/> instance
        /// and an optional target object if the method is an instance method.
        /// </summary>
        /// <param name="method">The method to be represented via the created <see cref="AIFunction"/>.</param>
        /// <param name="target">
        /// The target object for the <paramref name="method"/> if it represents an instance method.
        /// This should be <see langword="null"/> if and only if <paramref name="method"/> is a static method.
        /// </param>
        /// <param name="options">Function creation options.</param>
        public ReflectionAIFunction(MethodInfo method, object? target, AIFunctionFactoryCreateOptions options)
        {
            _ = Throw.IfNull(method);
            _ = Throw.IfNull(options);

            options.SerializerOptions.MakeReadOnly();

            if (method.ContainsGenericParameters)
            {
                Throw.ArgumentException(nameof(method), "Open generic methods are not supported");
            }

            if (!method.IsStatic && target is null)
            {
                Throw.ArgumentNullException(nameof(target), "Target must not be null for an instance method.");
            }

            _method = method;
            _target = target;

            // Get the function name to use.
            string? functionName = options.Name;
            if (functionName is null)
            {
                functionName = SanitizeMemberName(method.Name!);

                const string AsyncSuffix = "Async";
                if (IsAsyncMethod(method) &&
                    functionName.EndsWith(AsyncSuffix, StringComparison.Ordinal) &&
                    functionName.Length > AsyncSuffix.Length)
                {
                    functionName = functionName.Substring(0, functionName.Length - AsyncSuffix.Length);
                }

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

            // Build up a list of AIParameterMetadata for the parameters we expect to be populated
            // from arguments. Some arguments are populated specially, not from arguments, and thus
            // we don't want to advertise their metadata.
            List<AIFunctionParameterMetadata>? parameterMetadata = options.Parameters is not null ? null : [];

            // Get marshaling delegates for parameters and build up the parameter metadata.
            var parameters = method.GetParameters();
            _parameterMarshallers = new Func<IReadOnlyDictionary<string, object?>, AIFunctionContext?, object?>[parameters.Length];
            bool sawAIContextParameter = false;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (GetParameterMarshaller(options, parameters[i], ref sawAIContextParameter, out _parameterMarshallers[i]) is AIFunctionParameterMetadata parameterView)
                {
                    parameterMetadata?.Add(parameterView);
                }
            }

            _needsAIFunctionContext = sawAIContextParameter;

            // Get the return type and a marshaling func for the return value.
            Type returnType = GetReturnMarshaller(method, out _returnMarshaller);
            _returnTypeInfo = returnType != typeof(void) ? options.SerializerOptions.GetTypeInfo(returnType) : null;

            string? description = options.Description ?? method.GetCustomAttribute<DescriptionAttribute>(inherit: true)?.Description;
            Metadata = new AIFunctionMetadata(functionName)
            {
                Description = description,
                Parameters = options.Parameters ?? parameterMetadata!,
                ReturnParameter = options.ReturnParameter ?? new()
                {
                    ParameterType = returnType,
                    Description = method.ReturnParameter.GetCustomAttribute<DescriptionAttribute>(inherit: true)?.Description,
                    Schema = AIJsonUtilities.CreateJsonSchema(returnType, serializerOptions: options.SerializerOptions, inferenceOptions: options.SchemaCreateOptions),
                },
                AdditionalProperties = options.AdditionalProperties ?? EmptyReadOnlyDictionary<string, object?>.Instance,
                JsonSerializerOptions = options.SerializerOptions,
                Schema = AIJsonUtilities.CreateFunctionJsonSchema(
                    title: functionName,
                    description: description,
                    parameters: options.Parameters ?? parameterMetadata,
                    options.SerializerOptions,
                    options.SchemaCreateOptions)
            };
        }

        /// <inheritdoc />
        public override AIFunctionMetadata Metadata { get; }

        /// <inheritdoc />
        protected override async Task<object?> InvokeCoreAsync(
            IEnumerable<KeyValuePair<string, object?>>? arguments,
            CancellationToken cancellationToken)
        {
            var paramMarshallers = _parameterMarshallers;
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
            AIFunctionContext? context = _needsAIFunctionContext ?
                new() { CancellationToken = cancellationToken } :
                null;

            for (int i = 0; i < args.Length; i++)
            {
                args[i] = paramMarshallers[i](argDict, context);
            }

            object? result = await _returnMarshaller(ReflectionInvoke(_method, _target, args)).ConfigureAwait(false);

            switch (_returnTypeInfo)
            {
                case null:
                    Debug.Assert(Metadata.ReturnParameter.ParameterType == typeof(void), "The return parameter is not void.");
                    return null;

                case { Kind: JsonTypeInfoKind.None }:
                    // Special-case trivial contracts to avoid the more expensive general-purpose serialization path.
                    return JsonSerializer.SerializeToElement(result, _returnTypeInfo);

                default:
                {
                    // Serialize asynchronously to support potential IAsyncEnumerable responses.
                    using MemoryStream stream = new();
                    await JsonSerializer.SerializeAsync(stream, result, _returnTypeInfo, cancellationToken).ConfigureAwait(false);
                    Utf8JsonReader reader = new(stream.GetBuffer().AsSpan(0, (int)stream.Length));
                    return JsonElement.ParseValue(ref reader);
                }
            }
        }

        /// <summary>
        /// Gets a delegate for handling the marshaling of a parameter.
        /// </summary>
        private static AIFunctionParameterMetadata? GetParameterMarshaller(
            AIFunctionFactoryCreateOptions options,
            ParameterInfo parameter,
            ref bool sawAIFunctionContext,
            out Func<IReadOnlyDictionary<string, object?>, AIFunctionContext?, object?> marshaller)
        {
            if (string.IsNullOrWhiteSpace(parameter.Name))
            {
                Throw.ArgumentException(nameof(parameter), "Parameter is missing a name.");
            }

            // Special-case an AIFunctionContext parameter.
            if (parameter.ParameterType == typeof(AIFunctionContext))
            {
                if (sawAIFunctionContext)
                {
                    Throw.ArgumentException(nameof(parameter), $"Only one {nameof(AIFunctionContext)} parameter is permitted.");
                }

                sawAIFunctionContext = true;

                marshaller = static (_, ctx) =>
                {
                    Debug.Assert(ctx is not null, "Expected a non-null context object.");
                    return ctx;
                };
                return null;
            }

            // Resolve the contract used to marshal the value from JSON -- can throw if not supported or not found.
            Type parameterType = parameter.ParameterType;
            JsonTypeInfo typeInfo = options.SerializerOptions.GetTypeInfo(parameterType);

            // Create a marshaller that simply looks up the parameter by name in the arguments dictionary.
            marshaller = (IReadOnlyDictionary<string, object?> arguments, AIFunctionContext? _) =>
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
                            string json = JsonSerializer.Serialize(value, options.SerializerOptions.GetTypeInfo(value.GetType()));
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

            string? description = parameter.GetCustomAttribute<DescriptionAttribute>(inherit: true)?.Description;
            return new AIFunctionParameterMetadata(parameter.Name)
            {
                Description = description,
                HasDefaultValue = parameter.HasDefaultValue,
                DefaultValue = parameter.HasDefaultValue ? parameter.DefaultValue : null,
                IsRequired = !parameter.IsOptional,
                ParameterType = parameter.ParameterType,
            };
        }

        /// <summary>
        /// Gets a delegate for handling the result value of a method, converting it into the <see cref="Task{FunctionResult}"/> to return from the invocation.
        /// </summary>
        private static Type GetReturnMarshaller(MethodInfo method, out Func<object?, ValueTask<object?>> marshaller)
        {
            // Handle each known return type for the method
            Type returnType = method.ReturnType;

            // Task
            if (returnType == typeof(Task))
            {
                marshaller = async static result =>
                {
                    await ((Task)ThrowIfNullResult(result)).ConfigureAwait(false);
                    return null;
                };
                return typeof(void);
            }

            // ValueTask
            if (returnType == typeof(ValueTask))
            {
                marshaller = async static result =>
                {
                    await ((ValueTask)ThrowIfNullResult(result)).ConfigureAwait(false);
                    return null;
                };
                return typeof(void);
            }

            if (returnType.IsGenericType)
            {
                // Task<T>
                if (returnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    MethodInfo taskResultGetter = GetMethodFromGenericMethodDefinition(returnType, _taskGetResult);
                    marshaller = async result =>
                    {
                        await ((Task)ThrowIfNullResult(result)).ConfigureAwait(false);
                        return ReflectionInvoke(taskResultGetter, result, null);
                    };
                    return taskResultGetter.ReturnType;
                }

                // ValueTask<T>
                if (returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
                {
                    MethodInfo valueTaskAsTask = GetMethodFromGenericMethodDefinition(returnType, _valueTaskAsTask);
                    MethodInfo asTaskResultGetter = GetMethodFromGenericMethodDefinition(valueTaskAsTask.ReturnType, _taskGetResult);
                    marshaller = async result =>
                    {
                        var task = (Task)ReflectionInvoke(valueTaskAsTask, ThrowIfNullResult(result), null)!;
                        await task.ConfigureAwait(false);
                        return ReflectionInvoke(asTaskResultGetter, task, null);
                    };
                    return asTaskResultGetter.ReturnType;
                }
            }

            // For everything else, just use the result as-is.
            marshaller = result => new ValueTask<object?>(result);
            return returnType;

            // Throws an exception if a result is found to be null unexpectedly
            static object ThrowIfNullResult(object? result) => result ?? throw new InvalidOperationException("Function returned null unexpectedly.");
        }

        /// <summary>Invokes the MethodInfo with the specified target object and arguments.</summary>
        private static object? ReflectionInvoke(MethodInfo method, object? target, object?[]? arguments)
        {
#if NET
            return method.Invoke(target, BindingFlags.DoNotWrapExceptions, binder: null, arguments, culture: null);
#else
            try
            {
                return method.Invoke(target, BindingFlags.Default, binder: null, arguments, culture: null);
            }
            catch (TargetInvocationException e) when (e.InnerException is not null)
            {
                // If we're targeting .NET Framework, such that BindingFlags.DoNotWrapExceptions
                // is ignored, the original exception will be wrapped in a TargetInvocationException.
                // Unwrap it and throw that original exception, maintaining its stack information.
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                return null;
            }
#endif
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
    }
}
