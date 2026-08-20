// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using OpenTelemetry.Trace;
using Xunit;

namespace Microsoft.Extensions.AI;

#pragma warning disable SA1402 // Collection definition and its tests are intentionally colocated.

[Collection(nameof(OpenTelemetryEnvironmentVariableTests))]
public class OpenTelemetryEnvironmentVariableTests
{
    [Theory]
    [InlineData(null, OpenTelemetryGenAISemanticConvention.LatestExperimental)]
    [InlineData(" ", OpenTelemetryGenAISemanticConvention.Version1_36)]
    [InlineData("http", OpenTelemetryGenAISemanticConvention.Version1_36)]
    [InlineData("http,gen_ai_latest_experimental", OpenTelemetryGenAISemanticConvention.LatestExperimental)]
    [InlineData(" http , gen_ai_latest_experimental ", OpenTelemetryGenAISemanticConvention.LatestExperimental)]
    [InlineData("GEN_AI_LATEST_EXPERIMENTAL", OpenTelemetryGenAISemanticConvention.Version1_36)]
    public void SemanticConventionResolvedFromStabilityOptIn(
        string? stabilityOptIn,
        OpenTelemetryGenAISemanticConvention expected)
    {
        const string VariableName = "OTEL_SEMCONV_STABILITY_OPT_IN";
        string? previousValue = Environment.GetEnvironmentVariable(VariableName);

        try
        {
            Environment.SetEnvironmentVariable(VariableName, stabilityOptIn);
            using var innerClient = new TestChatClient();
            using var client = new OpenTelemetryChatClient(innerClient);
            Assert.Equal(expected, client.SemanticConvention);
        }
        finally
        {
            Environment.SetEnvironmentVariable(VariableName, previousValue);
        }
    }

    [Fact]
    public void ExplicitSemanticConventionOverridesEnvironmentAndIsStableForInstance()
    {
        const string VariableName = "OTEL_SEMCONV_STABILITY_OPT_IN";
        string? previousValue = Environment.GetEnvironmentVariable(VariableName);

        try
        {
            Environment.SetEnvironmentVariable(VariableName, " ");
            using var firstInnerClient = new TestChatClient();
            using var firstClient = new OpenTelemetryChatClient(firstInnerClient);
            Assert.Equal(OpenTelemetryGenAISemanticConvention.Version1_36, firstClient.SemanticConvention);

            Environment.SetEnvironmentVariable(VariableName, "gen_ai_latest_experimental");
            Assert.Equal(OpenTelemetryGenAISemanticConvention.Version1_36, firstClient.SemanticConvention);

            firstClient.SemanticConvention = OpenTelemetryGenAISemanticConvention.LatestExperimental;
            Assert.Equal(OpenTelemetryGenAISemanticConvention.LatestExperimental, firstClient.SemanticConvention);

            using var secondInnerClient = new TestChatClient();
            using var secondClient = new OpenTelemetryChatClient(secondInnerClient);
            Assert.Equal(OpenTelemetryGenAISemanticConvention.LatestExperimental, secondClient.SemanticConvention);
        }
        finally
        {
            Environment.SetEnvironmentVariable(VariableName, previousValue);
        }
    }

    [Fact]
    public async Task FunctionInvocationUsesEnvironmentWhenNoTelemetryClientProvidesMode()
    {
        const string VariableName = "OTEL_SEMCONV_STABILITY_OPT_IN";
        string? previousValue = Environment.GetEnvironmentVariable(VariableName);

        try
        {
            Environment.SetEnvironmentVariable(VariableName, " ");

            string sourceName = Guid.NewGuid().ToString();
            var activities = new List<Activity>();
            using TracerProvider tracerProvider = OpenTelemetry.Sdk.CreateTracerProviderBuilder()
                .AddSource(sourceName)
                .AddInMemoryExporter(activities)
                .Build();

            int invocation = 0;
            using var innerClient = new TestChatClient
            {
                GetResponseAsyncCallback = (messages, options, cancellationToken) =>
                    Task.FromResult(
                        invocation++ == 0 ?
                            new ChatResponse(new ChatMessage(
                                ChatRole.Assistant,
                                [new FunctionCallContent("callId", "TestFunction")])) :
                            new ChatResponse(new ChatMessage(ChatRole.Assistant, "done"))),
            };
            using var client = new FunctionInvokingChatClient(innerClient);
            using var source = new ActivitySource(sourceName);

            using (Activity? activity = source.StartActivity("invoke_agent"))
            {
                Assert.NotNull(activity);
                _ = await client.GetResponseAsync(
                    [new ChatMessage(ChatRole.User, "run")],
                    new ChatOptions
                    {
                        Tools = [AIFunctionFactory.Create(() => "result", "TestFunction")],
                    });
            }

            Activity executeTool = Assert.Single(
                activities,
                activity => activity.DisplayName == "execute_tool TestFunction");
            Assert.Null(executeTool.GetTagItem("gen_ai.tool.type"));
            Assert.DoesNotContain(executeTool.Tags, tag => tag.Key == "gen_ai.tool.call.arguments");
            Assert.DoesNotContain(executeTool.Tags, tag => tag.Key == "gen_ai.tool.call.result");
        }
        finally
        {
            Environment.SetEnvironmentVariable(VariableName, previousValue);
        }
    }

    [CollectionDefinition(nameof(OpenTelemetryEnvironmentVariableTests), DisableParallelization = true)]
    public sealed class OpenTelemetryEnvironmentVariableTestCollection;
}
