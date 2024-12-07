// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Shared.JsonExceptionConverter;
using Xunit;

namespace Microsoft.Extensions.Logging.Test;

public class ExceptionConverterTests
{
    private static readonly JsonSerializerOptions _options = new()
    {
        Converters = { new ExceptionConverter() }
    };

    [Fact]
    public void SerializeAndDeserialize_SimpleException()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new ExceptionConverter());

        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        var json = JsonSerializer.Serialize(exception, options);
        var deserializedException = JsonSerializer.Deserialize<Exception>(json, options);

        // Assert
        Assert.NotNull(deserializedException);
        Assert.IsType<InvalidOperationException>(deserializedException);
        Assert.Equal(exception.Message, deserializedException.Message);
    }

    [Fact]
    public void SerializeAndDeserialize_ExceptionWithInnerException()
    {
        // Arrange
        var innerException = new ArgumentNullException("paramName", "Inner exception message");
        var exception = new InvalidOperationException("Test exception with inner exception", innerException);

        // Act
        var json = JsonSerializer.Serialize(exception, _options);
        var deserializedException = JsonSerializer.Deserialize<Exception>(json, _options);

        // Assert
        Assert.NotNull(deserializedException);
        Assert.IsType<InvalidOperationException>(deserializedException);
        Assert.Equal(exception.Message, deserializedException.Message);

        Assert.NotNull(deserializedException.InnerException);
        Assert.IsType<ArgumentNullException>(deserializedException.InnerException);
        Assert.Contains(innerException.Message, deserializedException.InnerException.Message);
    }

    [Fact]
    public void SerializeAndDeserialize_AggregateException()
    {
        // Arrange
        var innerException1 = new ArgumentException("First inner exception");
#pragma warning disable CA2201 // Do not raise reserved exception types
        var innerException2 = new NullReferenceException("Second inner exception");
#pragma warning restore CA2201 // Do not raise reserved exception types
        var exception = new AggregateException("Aggregate exception message", innerException1, innerException2);

        // Act
        var json = JsonSerializer.Serialize(exception, _options);
        var deserializedException = JsonSerializer.Deserialize<Exception>(json, _options);

        // Assert
        Assert.NotNull(deserializedException);
        Assert.IsType<AggregateException>(deserializedException);
        Assert.Contains(exception.Message, deserializedException.Message);

        var aggregateException = (AggregateException)deserializedException;
        Assert.NotNull(aggregateException.InnerExceptions);
        Assert.Equal(2, aggregateException.InnerExceptions.Count);

        Assert.IsType<ArgumentException>(aggregateException.InnerExceptions[0]);
        Assert.Equal(innerException1.Message, aggregateException.InnerExceptions[0].Message);

        Assert.IsType<NullReferenceException>(aggregateException.InnerExceptions[1]);
        Assert.Equal(innerException2.Message, aggregateException.InnerExceptions[1].Message);
    }
}
#endif
