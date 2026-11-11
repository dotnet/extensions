// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ErrorContentTests
{
    [Fact]
    public void Constructor_NormalizesNullToEmpty()
    {
        ErrorContent content = new(null!);
        Assert.Empty(content.Message);

        content.Message = "test";
        Assert.Equal("test", content.Message);

        content.Message = null!;
        Assert.Empty(content.Message);
    }

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange
        string errorMessage = "Error occurred";
        string errorCode = "ERR001";
        string errorDetails = "Something went wrong";

        // Act
        var errorContent = new ErrorContent(errorMessage)
        {
            ErrorCode = errorCode,
            Details = errorDetails
        };

        // Assert
        Assert.Equal(errorMessage, errorContent.Message);
        Assert.Equal(errorCode, errorContent.ErrorCode);
        Assert.Equal(errorDetails, errorContent.Details);
    }

    [Fact]
    public void JsonSerialization_ShouldSerializeAndDeserializeCorrectly()
    {
        // Arrange
        var errorContent = new ErrorContent("Error occurred")
        {
            ErrorCode = "ERR001",
            Details = "Something went wrong"
        };
        JsonSerializerOptions options = new(AIJsonUtilities.DefaultOptions) { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // Act
        var json = JsonSerializer.Serialize(errorContent, options);
        var deserializedErrorContent = JsonSerializer.Deserialize<ErrorContent>(json, options);

        // Assert
        Assert.NotNull(deserializedErrorContent);
        Assert.Equal(errorContent.Message, deserializedErrorContent!.Message);
        Assert.Equal(errorContent.ErrorCode, deserializedErrorContent.ErrorCode);
        Assert.Equal(errorContent.Details, deserializedErrorContent.Details);
    }

    [Fact]
    public void JsonDeserialization_KnownPayload()
    {
        const string Json = """
            {
              "$type": "error",
              "message": "Error occurred",
              "errorCode": "ERR001",
              "details": "Something went wrong",
              "additionalProperties": {
                "severity": "high"
              }
            }
            """;

        AIContent? result = JsonSerializer.Deserialize<AIContent>(Json, AIJsonUtilities.DefaultOptions);

        Assert.NotNull(result);
        var errorContent = Assert.IsType<ErrorContent>(result);
        Assert.Equal("Error occurred", errorContent.Message);
        Assert.Equal("ERR001", errorContent.ErrorCode);
        Assert.Equal("Something went wrong", errorContent.Details);
        Assert.NotNull(errorContent.AdditionalProperties);
        Assert.Equal("high", errorContent.AdditionalProperties["severity"]?.ToString());
    }
}
