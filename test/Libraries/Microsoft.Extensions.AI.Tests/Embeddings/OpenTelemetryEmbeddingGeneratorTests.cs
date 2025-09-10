// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using OpenTelemetry.Trace;
using Xunit;

namespace Microsoft.Extensions.AI;

public class OpenTelemetryEmbeddingGeneratorTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("replacementmodel", false)]
    [InlineData("replacementmodel", true)]
    public async Task ExpectedInformationLogged_Async(string? perRequestModelId, bool enableSensitiveData)
    {
        var sourceName = Guid.NewGuid().ToString();
        var activities = new List<Activity>();
        using var tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
            .AddSource(sourceName)
            .AddInMemoryExporter(activities)
            .Build();

        var collector = new FakeLogCollector();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.AddProvider(new FakeLoggerProvider(collector)));

        using var innerGenerator = new TestEmbeddingGenerator
        {
            GenerateAsyncCallback = async (values, options, cancellationToken) =>
            {
                await Task.Yield();
                return new GeneratedEmbeddings<Embedding<float>>([new Embedding<float>(new float[] { 1, 2, 3 })])
                {
                    Usage = new()
                    {
                        InputTokenCount = 10,
                        TotalTokenCount = 10,
                    },
                    AdditionalProperties = new()
                    {
                        ["system_fingerprint"] = "abcdefgh",
                        ["AndSomethingElse"] = "value3",
                    }
                };
            },
            GetServiceCallback = (serviceType, serviceKey) =>
                serviceType == typeof(EmbeddingGeneratorMetadata) ? new EmbeddingGeneratorMetadata("testservice", new Uri("http://localhost:12345/something"), "defaultmodel", 1234) :
                null,
        };

        using var generator = innerGenerator
            .AsBuilder()
            .UseOpenTelemetry(loggerFactory, sourceName, configure: g => g.EnableSensitiveData = enableSensitiveData)
            .Build();

        var options = new EmbeddingGenerationOptions
        {
            ModelId = perRequestModelId,
            AdditionalProperties = new()
            {
                ["service_tier"] = "value1",
                ["SomethingElse"] = "value2",
            },
        };

        await generator.GenerateVectorAsync("hello", options);

        var activity = Assert.Single(activities);
        var expectedModelName = perRequestModelId ?? "defaultmodel";

        Assert.NotNull(activity.Id);
        Assert.NotEmpty(activity.Id);

        Assert.Equal("localhost", activity.GetTagItem("server.address"));
        Assert.Equal(12345, (int)activity.GetTagItem("server.port")!);

        Assert.Equal($"embeddings {expectedModelName}", activity.DisplayName);
        Assert.Equal("testservice", activity.GetTagItem("gen_ai.provider.name"));

        Assert.Equal(expectedModelName, activity.GetTagItem("gen_ai.request.model"));
        Assert.Equal(1234, activity.GetTagItem("gen_ai.request.embedding.dimensions"));
        Assert.Equal(enableSensitiveData ? "value1" : null, activity.GetTagItem("service_tier"));
        Assert.Equal(enableSensitiveData ? "value2" : null, activity.GetTagItem("SomethingElse"));

        Assert.Equal(10, activity.GetTagItem("gen_ai.usage.input_tokens"));
        Assert.Equal(enableSensitiveData ? "abcdefgh" : null, activity.GetTagItem("system_fingerprint"));
        Assert.Equal(enableSensitiveData ? "value3" : null, activity.GetTagItem("AndSomethingElse"));

        Assert.True(activity.Duration.TotalMilliseconds > 0);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("false", false)]
    [InlineData("FALSE", false)]
    [InlineData("True", true)]
    [InlineData("true", true)]
    [InlineData("TRUE", true)]
    [InlineData("yes", false)] // Should only respond to "true"
    [InlineData("1", false)] // Should only respond to "true"
    public void EnableSensitiveData_DefaultsBasedOnEnvironmentVariable(string? envVarValue, bool expectedDefault)
    {
        // Arrange: Set up environment variable
        string originalValue = Environment.GetEnvironmentVariable("OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT") ?? "";
        
        try
        {
            if (envVarValue is null)
            {
                Environment.SetEnvironmentVariable("OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT", null);
            }
            else
            {
                Environment.SetEnvironmentVariable("OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT", envVarValue);
            }

            // Act: Create a new instance
            using var innerGenerator = new TestEmbeddingGenerator<string, Embedding<float>>();
            using var generator = new OpenTelemetryEmbeddingGenerator<string, Embedding<float>>(innerGenerator);

            // Assert: Check the default value
            Assert.Equal(expectedDefault, generator.EnableSensitiveData);
        }
        finally
        {
            // Cleanup: Restore original environment variable
            Environment.SetEnvironmentVariable("OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT", 
                string.IsNullOrEmpty(originalValue) ? null : originalValue);
        }
    }

    [Fact]
    public void EnableSensitiveData_ExplicitSettingOverridesEnvironmentVariable()
    {
        // Arrange: Set environment variable to true
        string originalValue = Environment.GetEnvironmentVariable("OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT") ?? "";
        
        try
        {
            Environment.SetEnvironmentVariable("OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT", "true");

            // Act: Create instance and explicitly set to false
            using var innerGenerator = new TestEmbeddingGenerator<string, Embedding<float>>();
            using var generator = new OpenTelemetryEmbeddingGenerator<string, Embedding<float>>(innerGenerator);
            
            // Verify it defaults to true from environment variable
            Assert.True(generator.EnableSensitiveData);
            
            // Explicitly set to false
            generator.EnableSensitiveData = false;

            // Assert: Explicit setting should override environment variable
            Assert.False(generator.EnableSensitiveData);
        }
        finally
        {
            // Cleanup: Restore original environment variable
            Environment.SetEnvironmentVariable("OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT", 
                string.IsNullOrEmpty(originalValue) ? null : originalValue);
        }
    }
}
