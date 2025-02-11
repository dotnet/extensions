// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the response to a chat request with structured output.</summary>
/// <typeparam name="T">The type of value expected from the chat response.</typeparam>
/// <remarks>
/// Language models are not guaranteed to honor the requested schema. If the model's output is not
/// parseable as the expected type, then <see cref="TryGetResult(out T)"/> will return <see langword="false"/>.
/// You can access the underlying JSON response on the <see cref="ChatResponse.Message"/> property.
/// </remarks>
public class ChatResponse<T> : ChatResponse
{
    private static readonly JsonReaderOptions _allowMultipleValuesJsonReaderOptions = new() { AllowMultipleValues = true };
    private readonly JsonSerializerOptions _serializerOptions;

    private T? _deserializedResult;
    private bool _hasDeserializedResult;

    /// <summary>Initializes a new instance of the <see cref="ChatResponse{T}"/> class.</summary>
    /// <param name="response">The unstructured <see cref="ChatResponse"/> that is being wrapped.</param>
    /// <param name="serializerOptions">The <see cref="JsonSerializerOptions"/> to use when deserializing the result.</param>
    public ChatResponse(ChatResponse response, JsonSerializerOptions serializerOptions)
        : base(Throw.IfNull(response).Choices)
    {
        _serializerOptions = Throw.IfNull(serializerOptions);
        AdditionalProperties = response.AdditionalProperties;
        CreatedAt = response.CreatedAt;
        FinishReason = response.FinishReason;
        ModelId = response.ModelId;
        RawRepresentation = response.RawRepresentation;
        ResponseId = response.ResponseId;
        Usage = response.Usage;
    }

    /// <summary>
    /// Gets the result value of the chat response as an instance of <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// If the response did not contain JSON, or if deserialization fails, this property will throw.
    /// To avoid exceptions, use <see cref="TryGetResult(out T)"/> instead.
    /// </remarks>
    public T Result
    {
        get
        {
            var result = GetResultCore(out var failureReason);
            return failureReason switch
            {
                FailureReason.ResultDidNotContainJson => throw new InvalidOperationException("The response did not contain text to be deserialized"),
                FailureReason.DeserializationProducedNull => throw new InvalidOperationException("The deserialized response is null"),
                FailureReason.ResultDidNotContainDataProperty => throw new InvalidOperationException("The response did not contain the expected 'data' property"),
                _ => result!,
            };
        }
    }

    /// <summary>
    /// Attempts to deserialize the result to produce an instance of <typeparamref name="T"/>.
    /// </summary>
    /// <param name="result">When this method returns, contains the result.</param>
    /// <returns><see langword="true"/> if the result was produced, otherwise <see langword="false"/>.</returns>
    public bool TryGetResult([NotNullWhen(true)] out T? result)
    {
        try
        {
            result = GetResultCore(out var failureReason);
            return failureReason is null;
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch
        {
            result = default;
            return false;
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }

    private static T? DeserializeFirstTopLevelObject(string json, JsonTypeInfo<T> typeInfo)
    {
        // We need to deserialize only the first top-level object as a workaround for a common LLM backend
        // issue. GPT 3.5 Turbo commonly returns multiple top-level objects after doing a function call.
        // See https://community.openai.com/t/2-json-objects-returned-when-using-function-calling-and-json-mode/574348
        var utf8ByteLength = Encoding.UTF8.GetByteCount(json);
        var buffer = ArrayPool<byte>.Shared.Rent(utf8ByteLength);
        try
        {
            var utf8SpanLength = Encoding.UTF8.GetBytes(json, 0, json.Length, buffer, 0);
            var utf8Span = new ReadOnlySpan<byte>(buffer, 0, utf8SpanLength);
            var reader = new Utf8JsonReader(utf8Span, _allowMultipleValuesJsonReaderOptions);
            return JsonSerializer.Deserialize(ref reader, typeInfo);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the JSON schema has an extra object wrapper.
    /// </summary>
    /// <remarks>
    /// The wrapper is required for any non-JSON-object-typed values such as numbers, enum values, and arrays.
    /// </remarks>
    internal bool IsWrappedInObject { get; set; }

    private string? GetResultAsJson()
    {
        var choice = Choices.Count == 1 ? Choices[0] : null;
        var content = choice?.Contents.Count == 1 ? choice.Contents[0] : null;
        return (content as TextContent)?.Text;
    }

    private T? GetResultCore(out FailureReason? failureReason)
    {
        if (_hasDeserializedResult)
        {
            failureReason = default;
            return _deserializedResult;
        }

        var json = GetResultAsJson();
        if (string.IsNullOrEmpty(json))
        {
            failureReason = FailureReason.ResultDidNotContainJson;
            return default;
        }

        T? deserialized = default;

        // If there's an exception here, we want it to propagate, since the Result property is meant to throw directly

        if (IsWrappedInObject)
        {
            if (JsonDocument.Parse(json!).RootElement.TryGetProperty("data", out var data))
            {
                json = data.GetRawText();
            }
            else
            {
                failureReason = FailureReason.ResultDidNotContainDataProperty;
                return default;
            }
        }

        deserialized = DeserializeFirstTopLevelObject(json!, (JsonTypeInfo<T>)_serializerOptions.GetTypeInfo(typeof(T)));

        if (deserialized is null)
        {
            failureReason = FailureReason.DeserializationProducedNull;
            return default;
        }

        _deserializedResult = deserialized;
        _hasDeserializedResult = true;
        failureReason = default;
        return deserialized;
    }

    private enum FailureReason
    {
        ResultDidNotContainJson,
        DeserializationProducedNull,
        ResultDidNotContainDataProperty,
    }
}
