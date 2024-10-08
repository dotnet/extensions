// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Collections;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides factory methods for creating commonly-used implementations of <see cref="AIFunction"/>.</summary>
public static
#if NET
    partial
#endif
    class AIFunctionFactory
{
    internal const string UsesReflectionJsonSerializerMessage =
        "This method uses the reflection-based JsonSerializer which can break in trimmed or AOT applications.";

    /// <summary>Lazily-initialized default options instance.</summary>
    private static AIFunctionFactoryCreateOptions? _defaultOptions;

    /// <summary>Creates an <see cref="AIFunction"/> instance for a method, specified via a delegate.</summary>
    /// <param name="method">The method to be represented via the created <see cref="AIFunction"/>.</param>
    /// <returns>The created <see cref="AIFunction"/> for invoking <paramref name="method"/>.</returns>
    [RequiresUnreferencedCode(UsesReflectionJsonSerializerMessage)]
    [RequiresDynamicCode(UsesReflectionJsonSerializerMessage)]
    public static AIFunction Create(Delegate method) => Create(method, _defaultOptions ??= new());

    /// <summary>Creates an <see cref="AIFunction"/> instance for a method, specified via a delegate.</summary>
    /// <param name="method">The method to be represented via the created <see cref="AIFunction"/>.</param>
    /// <param name="options">Metadata to use to override defaults inferred from <paramref name="method"/>.</param>
    /// <returns>The created <see cref="AIFunction"/> for invoking <paramref name="method"/>.</returns>
    [RequiresUnreferencedCode("Reflection is used to access types from the supplied MethodInfo.")]
    [RequiresDynamicCode("Reflection is used to access types from the supplied MethodInfo.")]
    public static AIFunction Create(Delegate method, AIFunctionFactoryCreateOptions options)
    {
        _ = Throw.IfNull(method);
        _ = Throw.IfNull(options);
        return new ReflectionAIFunction(method.Method, method.Target, options);
    }

    /// <summary>Creates an <see cref="AIFunction"/> instance for a method, specified via a delegate.</summary>
    /// <param name="method">The method to be represented via the created <see cref="AIFunction"/>.</param>
    /// <param name="name">The name to use for the <see cref="AIFunction"/>.</param>
    /// <param name="description">The description to use for the <see cref="AIFunction"/>.</param>
    /// <returns>The created <see cref="AIFunction"/> for invoking <paramref name="method"/>.</returns>
    [RequiresUnreferencedCode("Reflection is used to access types from the supplied Delegate.")]
    [RequiresDynamicCode("Reflection is used to access types from the supplied Delegate.")]
    public static AIFunction Create(Delegate method, string? name, string? description = null)
        => Create(method, (_defaultOptions ??= new()).SerializerOptions, name, description);

    /// <summary>Creates an <see cref="AIFunction"/> instance for a method, specified via a delegate.</summary>
    /// <param name="method">The method to be represented via the created <see cref="AIFunction"/>.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> used to marshal function parameters.</param>
    /// <param name="name">The name to use for the <see cref="AIFunction"/>.</param>
    /// <param name="description">The description to use for the <see cref="AIFunction"/>.</param>
    /// <returns>The created <see cref="AIFunction"/> for invoking <paramref name="method"/>.</returns>
    [RequiresUnreferencedCode("Reflection is used to access types from the supplied MethodInfo.")]
    [RequiresDynamicCode("Reflection is used to access types from the supplied MethodInfo.")]
    public static AIFunction Create(Delegate method, JsonSerializerOptions options, string? name = null, string? description = null)
    {
        _ = Throw.IfNull(method);
        _ = Throw.IfNull(options);
        return new ReflectionAIFunction(method.Method, method.Target, new(options) { Name = name, Description = description });
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
    /// <returns>The created <see cref="AIFunction"/> for invoking <paramref name="method"/>.</returns>
    [RequiresUnreferencedCode("Reflection is used to access types from the supplied MethodInfo.")]
    [RequiresDynamicCode("Reflection is used to access types from the supplied MethodInfo.")]
    public static AIFunction Create(MethodInfo method, object? target = null)
        => Create(method, target, _defaultOptions ??= new());

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
    [RequiresUnreferencedCode("Reflection is used to access types from the supplied MethodInfo.")]
    [RequiresDynamicCode("Reflection is used to access types from the supplied MethodInfo.")]
    public static AIFunction Create(MethodInfo method, object? target, AIFunctionFactoryCreateOptions options)
    {
        _ = Throw.IfNull(method);
        _ = Throw.IfNull(options);
        return new ReflectionAIFunction(method, target, options);
    }

    private sealed
#if NET
        partial
#endif
        class ReflectionAIFunction : AIFunction
    {
        private readonly MethodInfo _method;
        private readonly object? _target;
        private readonly Func<IReadOnlyDictionary<string, object?>, AIFunctionContext?, object?>[] _parameterMarshalers;
        private readonly Func<object?, ValueTask<object?>> _returnMarshaler;
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
        [RequiresUnreferencedCode("Reflection is used to access types from the supplied MethodInfo.")]
        [RequiresDynamicCode("Reflection is used to access types from the supplied MethodInfo.")]
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
                functionName = SanitizeMetadataName(method.Name!);

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
            _parameterMarshalers = new Func<IReadOnlyDictionary<string, object?>, AIFunctionContext?, object?>[parameters.Length];
            bool sawAIContextParameter = false;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (GetParameterMarshaler(options.SerializerOptions, parameters[i], ref sawAIContextParameter, out _parameterMarshalers[i]) is AIFunctionParameterMetadata parameterView)
                {
                    parameterMetadata?.Add(parameterView);
                }
            }

            _needsAIFunctionContext = sawAIContextParameter;

            // Get the return type and a marshaling func for the return value.
            Type returnType = GetReturnMarshaler(method, out _returnMarshaler);
            _returnTypeInfo = returnType != typeof(void) ? options.SerializerOptions.GetTypeInfo(returnType) : null;

            Metadata = new AIFunctionMetadata(functionName)
            {
                Description = options.Description ?? method.GetCustomAttribute<DescriptionAttribute>(inherit: true)?.Description ?? string.Empty,
                Parameters = options.Parameters ?? parameterMetadata!,
                ReturnParameter = options.ReturnParameter ?? new()
                {
                    ParameterType = returnType,
                    Description = method.ReturnParameter.GetCustomAttribute<DescriptionAttribute>(inherit: true)?.Description,
                    Schema = FunctionCallHelpers.InferReturnParameterJsonSchema(returnType, options.SerializerOptions),
                },
                AdditionalProperties = options.AdditionalProperties ?? EmptyReadOnlyDictionary<string, object?>.Instance,
                JsonSerializerOptions = options.SerializerOptions,
            };
        }

        /// <inheritdoc />
        public override AIFunctionMetadata Metadata { get; }

        /// <inheritdoc />
        protected override async Task<object?> InvokeCoreAsync(
            IEnumerable<KeyValuePair<string, object?>>? arguments,
            CancellationToken cancellationToken)
        {
            var paramMarshalers = _parameterMarshalers;
            object?[] args = paramMarshalers.Length != 0 ? new object?[paramMarshalers.Length] : [];

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
                args[i] = paramMarshalers[i](argDict, context);
            }

            object? result = await _returnMarshaler(ReflectionInvoke(_method, _target, args)).ConfigureAwait(false);

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
        private static AIFunctionParameterMetadata? GetParameterMarshaler(
            JsonSerializerOptions options,
            ParameterInfo parameter,
            ref bool sawAIFunctionContext,
            out Func<IReadOnlyDictionary<string, object?>, AIFunctionContext?, object?> marshaler)
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

                marshaler = static (_, ctx) =>
                {
                    Debug.Assert(ctx is not null, "Expected a non-null context object.");
                    return ctx;
                };
                return null;
            }

            // Resolve the contract used to marshall the value from JSON -- can throw if not supported or not found.
            Type parameterType = parameter.ParameterType;
            JsonTypeInfo typeInfo = options.GetTypeInfo(parameterType);

            // Create a marshaler that simply looks up the parameter by name in the arguments dictionary.
            marshaler = (IReadOnlyDictionary<string, object?> arguments, AIFunctionContext? _) =>
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
                            string json = JsonSerializer.Serialize(value, options.GetTypeInfo(value.GetType()));
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
                Schema = FunctionCallHelpers.InferParameterJsonSchema(
                    parameter.ParameterType,
                    parameter.Name,
                    description,
                    parameter.HasDefaultValue,
                    parameter.DefaultValue,
                    options)
            };
        }

        /// <summary>
        /// Gets a delegate for handling the result value of a method, converting it into the <see cref="Task{FunctionResult}"/> to return from the invocation.
        /// </summary>
        [RequiresUnreferencedCode("Reflection is used to access types from the supplied MethodInfo.")]
        [RequiresDynamicCode("Reflection is used to access types from the supplied MethodInfo.")]
        private static Type GetReturnMarshaler(MethodInfo method, out Func<object?, ValueTask<object?>> marshaler)
        {
            // Handle each known return type for the method
            Type returnType = method.ReturnType;

            // Task
            if (returnType == typeof(Task))
            {
                marshaler = async static result =>
                {
                    await ((Task)ThrowIfNullResult(result)).ConfigureAwait(false);
                    return null;
                };
                return typeof(void);
            }

            // ValueTask
            if (returnType == typeof(ValueTask))
            {
                marshaler = async static result =>
                {
                    await ((ValueTask)ThrowIfNullResult(result)).ConfigureAwait(false);
                    return null;
                };
                return typeof(void);
            }

            if (returnType.IsGenericType)
            {
                // Task<T>
                if (returnType.GetGenericTypeDefinition() == typeof(Task<>) &&
                    returnType.GetProperty(nameof(Task<int>.Result), BindingFlags.Public | BindingFlags.Instance)?.GetGetMethod() is MethodInfo taskResultGetter)
                {
                    marshaler = async result =>
                    {
                        await ((Task)ThrowIfNullResult(result)).ConfigureAwait(false);
                        return ReflectionInvoke(taskResultGetter, result, null);
                    };
                    return taskResultGetter.ReturnType;
                }

                // ValueTask<T>
                if (returnType.GetGenericTypeDefinition() == typeof(ValueTask<>) &&
                    returnType.GetMethod(nameof(ValueTask<int>.AsTask), BindingFlags.Public | BindingFlags.Instance) is MethodInfo valueTaskAsTask &&
                    valueTaskAsTask.ReturnType.GetProperty(nameof(ValueTask<int>.Result), BindingFlags.Public | BindingFlags.Instance)?.GetGetMethod() is MethodInfo asTaskResultGetter)
                {
                    marshaler = async result =>
                    {
                        var task = (Task)ReflectionInvoke(valueTaskAsTask, ThrowIfNullResult(result), null)!;
                        await task.ConfigureAwait(false);
                        return ReflectionInvoke(asTaskResultGetter, task, null);
                    };
                    return asTaskResultGetter.ReturnType;
                }
            }

            // For everything else, just use the result as-is.
            marshaler = result => new ValueTask<object?>(result);
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

        /// <summary>
        /// Remove characters from method name that are valid in metadata but shouldn't be used in a method name.
        /// This is primarily intended to remove characters emitted by for compiler-generated method name mangling.
        /// </summary>
        private static string SanitizeMetadataName(string methodName) =>
            InvalidNameCharsRegex().Replace(methodName, "_");

        /// <summary>Regex that flags any character other than ASCII digits or letters or the underscore.</summary>
#if NET
        [GeneratedRegex("[^0-9A-Za-z_]")]
        private static partial Regex InvalidNameCharsRegex();
#else
        private static Regex InvalidNameCharsRegex() => _invalidNameCharsRegex;
        private static readonly Regex _invalidNameCharsRegex = new("[^0-9A-Za-z_]", RegexOptions.Compiled);
#endif
    }
}
