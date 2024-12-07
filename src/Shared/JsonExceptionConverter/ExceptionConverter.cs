// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

#pragma warning disable CA1716
namespace Microsoft.Shared.JsonExceptionConverter;
#pragma warning restore CA1716

internal sealed class ExceptionConverter : JsonConverter<Exception>
{
#pragma warning disable CA2201 // Do not raise reserved exception types
    private static Exception _failedDeserialization = new Exception("Failed to deserialize the exception object.");
#pragma warning restore CA2201 // Do not raise reserved exception types

    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
    public override Exception Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            return _failedDeserialization;
        }

        return DeserializeException(ref reader);
    }

    public override void Write(Utf8JsonWriter writer, Exception exception, JsonSerializerOptions options)
    {
        HandleException(writer, exception);
    }

    public static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        Converters = { new ExceptionConverter() }
    };

    public override bool CanConvert(Type typeToConvert) => typeof(Exception).IsAssignableFrom(typeToConvert);

    private static Exception DeserializeException(ref Utf8JsonReader reader)
    {
        string? type = null;
        string? message = null;
        string? source = null;
        Exception? innerException = null;
        List<Exception>? innerExceptions = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string? propertyName = reader.GetString();
                _ = reader.Read();

                switch (propertyName)
                {
                    case "Type":
                        type = reader.GetString();
                        break;
                    case "Message":
                        message = reader.GetString();
                        break;
                    case "Source":
                        source = reader.GetString();
                        break;
                    case "InnerException":
                        if (reader.TokenType == JsonTokenType.StartObject)
                        {
                            innerException = DeserializeException(ref reader);
                        }

                        break;
                    case "InnerExceptions":
                        innerExceptions = new List<Exception>();
                        if (reader.TokenType == JsonTokenType.StartArray)
                        {
                            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                            {
                                if (reader.TokenType == JsonTokenType.StartObject)
                                {
                                    var innerEx = DeserializeException(ref reader);
                                    innerExceptions.Add(innerEx);
                                }
                            }
                        }

                        break;
                }
            }
            else if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }
        }

        if (type is null)
        {
            return _failedDeserialization;
        }

#pragma warning disable IL2057 // Unrecognized value passed to the parameter of method. It's not possible to guarantee the availability of the target type.
        Type? deserializedType = Type.GetType(type);
#pragma warning restore IL2057 // Unrecognized value passed to the parameter of method. It's not possible to guarantee the availability of the target type.
        if (deserializedType is null)
        {
            return _failedDeserialization;
        }

        Exception? exception;

        if (innerExceptions != null && innerExceptions.Count > 0)
        {
            if (deserializedType == typeof(AggregateException))
            {
                exception = new AggregateException(message, innerExceptions);
            }
            else
            {
                exception = Activator.CreateInstance(deserializedType, message, innerExceptions.First()) as Exception;
            }
        }
        else if (innerException != null)
        {
            exception = Activator.CreateInstance(deserializedType, message, innerException) as Exception;
        }
        else
        {
            exception = Activator.CreateInstance(deserializedType, message) as Exception;
        }

        if (exception == null)
        {
            return _failedDeserialization;
        }

        exception.Source = source;

        return exception;
    }

    private static void HandleException(Utf8JsonWriter writer, Exception exception)
    {
        writer.WriteStartObject();
        writer.WriteString("Type", exception.GetType().FullName);
        writer.WriteString("Message", exception.Message);
        writer.WriteString("Source", exception.Source);
        if (exception is AggregateException aggregateException)
        {
            writer.WritePropertyName("InnerExceptions");
            writer.WriteStartArray();
            foreach (var ex in aggregateException.InnerExceptions)
            {
                HandleException(writer, ex);
            }

            writer.WriteEndArray();
        }
        else if (exception.InnerException != null)
        {
            writer.WritePropertyName("InnerException");
            HandleException(writer, exception.InnerException);
        }

        writer.WriteEndObject();
    }
}
#endif
