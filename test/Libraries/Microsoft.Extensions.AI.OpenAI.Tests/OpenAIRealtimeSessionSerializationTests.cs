// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates.
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable S103 // Lines should not be too long

namespace Microsoft.Extensions.AI;

/// <summary>
/// Tests for the JSON serialization and deserialization logic used by <see cref="OpenAIRealtimeSession"/>.
/// Uses a channel-backed WebSocket pair to exercise message processing without a real network connection.
/// </summary>
public class OpenAIRealtimeSessionSerializationTests : IAsyncLifetime
{
    private readonly ChannelWebSocket _serverWebSocket;
    private readonly ChannelWebSocket _clientWebSocket;
    private readonly OpenAIRealtimeSession _session;
    private readonly CancellationTokenSource _cts = new();

    public OpenAIRealtimeSessionSerializationTests()
    {
        var clientToServer = Channel.CreateUnbounded<(byte[] data, WebSocketMessageType messageType, bool endOfMessage)>();
        var serverToClient = Channel.CreateUnbounded<(byte[] data, WebSocketMessageType messageType, bool endOfMessage)>();

        _clientWebSocket = new ChannelWebSocket(serverToClient.Reader, clientToServer.Writer);
        _serverWebSocket = new ChannelWebSocket(clientToServer.Reader, serverToClient.Writer);
        _session = new OpenAIRealtimeSession("test-key", "test-model");
    }

    public async Task InitializeAsync()
    {
        await _session.ConnectWithWebSocketAsync(_clientWebSocket, _cts.Token);
    }

    public Task DisposeAsync()
    {
        _cts.Cancel();
        _session.Dispose();
        _clientWebSocket.Dispose();
        _serverWebSocket.Dispose();
        _cts.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ProcessServerEvent_ErrorMessage_ParsedCorrectly()
    {
        var errorJson = """{"type":"error","event_id":"evt_001","error":{"message":"Something went wrong","code":"invalid_request","param":"model"}}""";

        await SendServerMessageAsync(errorJson);
        var msg = await ReadNextServerMessageAsync();

        var errorMsg = Assert.IsType<RealtimeServerErrorMessage>(msg);
        Assert.Equal(RealtimeServerMessageType.Error, errorMsg.Type);
        Assert.Equal("evt_001", errorMsg.EventId);
        Assert.Equal("Something went wrong", errorMsg.Error?.Message);
        Assert.Equal("invalid_request", errorMsg.Error?.ErrorCode);
        Assert.Equal("model", errorMsg.Parameter);
    }

    [Fact]
    public async Task ProcessServerEvent_InputAudioTranscriptionDelta_ParsedCorrectly()
    {
        var json = """{"type":"conversation.item.input_audio_transcription.delta","event_id":"evt_002","item_id":"item_001","content_index":0,"delta":"Hello world"}""";

        await SendServerMessageAsync(json);
        var msg = await ReadNextServerMessageAsync();

        var transcription = Assert.IsType<RealtimeServerInputAudioTranscriptionMessage>(msg);
        Assert.Equal(RealtimeServerMessageType.InputAudioTranscriptionDelta, transcription.Type);
        Assert.Equal("evt_002", transcription.EventId);
        Assert.Equal("item_001", transcription.ItemId);
        Assert.Equal(0, transcription.ContentIndex);
        Assert.Equal("Hello world", transcription.Transcription);
    }

    [Fact]
    public async Task ProcessServerEvent_InputAudioTranscriptionCompleted_UsesTranscriptField()
    {
        var json = """{"type":"conversation.item.input_audio_transcription.completed","event_id":"evt_003","item_id":"item_002","content_index":1,"transcript":"The full transcription"}""";

        await SendServerMessageAsync(json);
        var msg = await ReadNextServerMessageAsync();

        var transcription = Assert.IsType<RealtimeServerInputAudioTranscriptionMessage>(msg);
        Assert.Equal(RealtimeServerMessageType.InputAudioTranscriptionCompleted, transcription.Type);
        Assert.Equal("The full transcription", transcription.Transcription);
    }

    [Fact]
    public async Task ProcessServerEvent_InputAudioTranscriptionFailed_ParsedCorrectly()
    {
        var json = """{"type":"conversation.item.input_audio_transcription.failed","event_id":"evt_004","item_id":"item_003","error":{"message":"Transcription failed","code":"transcription_error","param":"audio"}}""";

        await SendServerMessageAsync(json);
        var msg = await ReadNextServerMessageAsync();

        var transcription = Assert.IsType<RealtimeServerInputAudioTranscriptionMessage>(msg);
        Assert.Equal(RealtimeServerMessageType.InputAudioTranscriptionFailed, transcription.Type);
        Assert.NotNull(transcription.Error);
        Assert.Equal("Transcription failed", transcription.Error.Message);
        Assert.Equal("transcription_error", transcription.Error.ErrorCode);
    }

    [Fact]
    public async Task ProcessServerEvent_OutputAudioTranscriptDelta_ParsedCorrectly()
    {
        var json = """{"type":"response.output_audio_transcript.delta","event_id":"evt_005","response_id":"resp_001","item_id":"item_004","output_index":0,"content_index":0,"delta":"Hello"}""";

        await SendServerMessageAsync(json);
        var msg = await ReadNextServerMessageAsync();

        var outputMsg = Assert.IsType<RealtimeServerOutputTextAudioMessage>(msg);
        Assert.Equal(RealtimeServerMessageType.OutputAudioTranscriptionDelta, outputMsg.Type);
        Assert.Equal("evt_005", outputMsg.EventId);
        Assert.Equal("resp_001", outputMsg.ResponseId);
        Assert.Equal("item_004", outputMsg.ItemId);
        Assert.Equal(0, outputMsg.OutputIndex);
        Assert.Equal(0, outputMsg.ContentIndex);
        Assert.Equal("Hello", outputMsg.Text);
    }

    [Fact]
    public async Task ProcessServerEvent_OutputAudioDelta_ParsedCorrectly()
    {
        var json = """{"type":"response.output_audio.delta","event_id":"evt_006","response_id":"resp_002","item_id":"item_005","output_index":0,"content_index":0,"delta":"base64audiodata"}""";

        await SendServerMessageAsync(json);
        var msg = await ReadNextServerMessageAsync();

        var outputMsg = Assert.IsType<RealtimeServerOutputTextAudioMessage>(msg);
        Assert.Equal(RealtimeServerMessageType.OutputAudioDelta, outputMsg.Type);
        Assert.Equal("base64audiodata", outputMsg.Text);
    }

    [Fact]
    public async Task ProcessServerEvent_ResponseCreated_ParsedCorrectly()
    {
        var json = """{"type":"response.created","event_id":"evt_007","response":{"id":"resp_003","conversation_id":"conv_001","status":"in_progress","max_output_tokens":4096,"output_modalities":["text","audio"],"metadata":{"key1":"value1"},"audio":{"output":{"format":{"type":"audio/pcm","rate":24000},"voice":"alloy"}}}}""";

        await SendServerMessageAsync(json);
        var msg = await ReadNextServerMessageAsync();

        var responseMsg = Assert.IsType<RealtimeServerResponseCreatedMessage>(msg);
        Assert.Equal(RealtimeServerMessageType.ResponseCreated, responseMsg.Type);
        Assert.Equal("evt_007", responseMsg.EventId);
        Assert.Equal("resp_003", responseMsg.ResponseId);
        Assert.Equal("conv_001", responseMsg.ConversationId);
        Assert.Equal("in_progress", responseMsg.Status);
        Assert.Equal(4096, responseMsg.MaxOutputTokens);
        Assert.NotNull(responseMsg.OutputModalities);
        Assert.Equal(new[] { "text", "audio" }, responseMsg.OutputModalities);
        Assert.NotNull(responseMsg.Metadata);
        Assert.Equal("value1", responseMsg.Metadata["key1"]);
        Assert.NotNull(responseMsg.OutputAudioOptions);
        Assert.Equal("audio/pcm", responseMsg.OutputAudioOptions.Type);
        Assert.Equal(24000, responseMsg.OutputAudioOptions.SampleRate);
        Assert.Equal("alloy", responseMsg.OutputVoice);
    }

    [Fact]
    public async Task ProcessServerEvent_ResponseDone_WithUsageAndOutput_ParsedCorrectly()
    {
        var json = """{"type":"response.done","event_id":"evt_008","response":{"id":"resp_004","status":"completed","usage":{"input_tokens":100,"output_tokens":50,"total_tokens":150},"output":[{"type":"message","id":"msg_001","role":"assistant","content":[{"type":"input_text","text":"Hello there!"}]}]}}""";

        await SendServerMessageAsync(json);
        var msg = await ReadNextServerMessageAsync();

        var responseMsg = Assert.IsType<RealtimeServerResponseCreatedMessage>(msg);
        Assert.Equal(RealtimeServerMessageType.ResponseDone, responseMsg.Type);
        Assert.Equal("resp_004", responseMsg.ResponseId);
        Assert.Equal("completed", responseMsg.Status);
        Assert.NotNull(responseMsg.Usage);
        Assert.Equal(100, responseMsg.Usage.InputTokenCount);
        Assert.Equal(50, responseMsg.Usage.OutputTokenCount);
        Assert.Equal(150, responseMsg.Usage.TotalTokenCount);
        Assert.NotNull(responseMsg.Items);
        Assert.Single(responseMsg.Items);
        Assert.Equal("msg_001", responseMsg.Items[0].Id);
        Assert.Equal(ChatRole.Assistant, responseMsg.Items[0].Role);
        var textContent = Assert.IsType<TextContent>(responseMsg.Items[0].Contents[0]);
        Assert.Equal("Hello there!", textContent.Text);
    }

    [Fact]
    public async Task ProcessServerEvent_OutputItemAdded_WithFunctionCall_ParsedCorrectly()
    {
        var json = """{"type":"response.output_item.added","event_id":"evt_010","response_id":"resp_006","output_index":0,"item":{"type":"function_call","id":"fc_001","name":"get_weather","call_id":"call_001","arguments":"{\"city\":\"Seattle\"}"}}""";

        await SendServerMessageAsync(json);
        var msg = await ReadNextServerMessageAsync();

        var outputItemMsg = Assert.IsType<RealtimeServerResponseOutputItemMessage>(msg);
        Assert.Equal(RealtimeServerMessageType.ResponseOutputItemAdded, outputItemMsg.Type);
        Assert.Equal("resp_006", outputItemMsg.ResponseId);
        Assert.Equal(0, outputItemMsg.OutputIndex);
        Assert.NotNull(outputItemMsg.Item);
        Assert.Equal("fc_001", outputItemMsg.Item.Id);
        var functionCall = Assert.IsType<FunctionCallContent>(outputItemMsg.Item.Contents[0]);
        Assert.Equal("call_001", functionCall.CallId);
        Assert.Equal("get_weather", functionCall.Name);
        Assert.NotNull(functionCall.Arguments);
        Assert.Equal("Seattle", functionCall.Arguments["city"]?.ToString());
    }

    [Fact]
    public async Task ProcessServerEvent_SessionCreated_UpdatesOptions()
    {
        var json = """{"type":"session.created","event_id":"evt_011","session":{"type":"realtime","model":"gpt-realtime","instructions":"Be helpful","max_output_tokens":2048,"output_modalities":["text"],"audio":{"input":{"format":{"type":"audio/pcm","rate":16000},"noise_reduction":{"type":"near_field"},"transcription":{"language":"en","model":"whisper-1"},"turn_detection":{"type":"server_vad","create_response":true,"interrupt_response":true,"idle_timeout_ms":5000,"prefix_padding_ms":300,"silence_duration_ms":500,"threshold":0.5}},"output":{"format":{"type":"audio/pcm","rate":24000},"speed":1.0,"voice":"alloy"}}}}""";

        await SendServerMessageAsync(json);
        var msg = await ReadNextServerMessageAsync();

        Assert.Equal(RealtimeServerMessageType.RawContentOnly, msg.Type);
        Assert.NotNull(msg.RawRepresentation);

        var options = _session.Options;
        Assert.NotNull(options);
        Assert.Equal(RealtimeSessionKind.Realtime, options.SessionKind);
        Assert.Equal("gpt-realtime", options.Model);
        Assert.Equal("Be helpful", options.Instructions);
        Assert.Equal(2048, options.MaxOutputTokens);
        Assert.NotNull(options.InputAudioFormat);
        Assert.Equal("audio/pcm", options.InputAudioFormat.Type);
        Assert.Equal(16000, options.InputAudioFormat.SampleRate);
        Assert.Equal(NoiseReductionOptions.NearField, options.NoiseReductionOptions);
        Assert.NotNull(options.TranscriptionOptions);
        Assert.Equal("en", options.TranscriptionOptions.SpeechLanguage);
        Assert.Equal("whisper-1", options.TranscriptionOptions.ModelId);

        var serverVad = Assert.IsType<ServerVoiceActivityDetection>(options.VoiceActivityDetection);
        Assert.True(serverVad.CreateResponse);
        Assert.True(serverVad.InterruptResponse);
        Assert.Equal(5000, serverVad.IdleTimeoutInMilliseconds);
        Assert.Equal(300, serverVad.PrefixPaddingInMilliseconds);
        Assert.Equal(500, serverVad.SilenceDurationInMilliseconds);
        Assert.Equal(0.5, serverVad.Threshold);

        Assert.NotNull(options.OutputAudioFormat);
        Assert.Equal("audio/pcm", options.OutputAudioFormat.Type);
        Assert.Equal(24000, options.OutputAudioFormat.SampleRate);
        Assert.Equal("alloy", options.Voice);
    }

    [Fact]
    public async Task ProcessServerEvent_SessionUpdated_WithSemanticVad()
    {
        var json = """{"type":"session.updated","event_id":"evt_012","session":{"type":"transcription","audio":{"input":{"turn_detection":{"type":"semantic_vad","create_response":false,"interrupt_response":true,"eagerness":"high"}}}}}""";

        await SendServerMessageAsync(json);
        var msg = await ReadNextServerMessageAsync();

        Assert.Equal(RealtimeServerMessageType.RawContentOnly, msg.Type);
        var options = _session.Options;
        Assert.NotNull(options);
        Assert.Equal(RealtimeSessionKind.Transcription, options.SessionKind);
        var semanticVad = Assert.IsType<SemanticVoiceActivityDetection>(options.VoiceActivityDetection);
        Assert.False(semanticVad.CreateResponse);
        Assert.True(semanticVad.InterruptResponse);
        Assert.Equal(SemanticEagerness.High, semanticVad.Eagerness);
    }

    [Fact]
    public async Task ProcessServerEvent_UnknownEventType_ParsedAsRawContentOnly()
    {
        var json = """{"type":"some.unknown.event","event_id":"evt_013","data":{"key":"value"}}""";

        await SendServerMessageAsync(json);
        var msg = await ReadNextServerMessageAsync();

        Assert.Equal(RealtimeServerMessageType.RawContentOnly, msg.Type);
        Assert.NotNull(msg.RawRepresentation);
    }

    [Fact]
    public async Task InjectClientMessage_ResponseCreate_SerializedCorrectly()
    {
        var message = new RealtimeClientResponseCreateMessage
        {
            EventId = "client_evt_001",
            Instructions = "Be concise",
            MaxOutputTokens = 1024,
            OutputModalities = new List<string> { "text", "audio" },
            ExcludeFromConversation = true,
            OutputVoice = "alloy",
            OutputAudioOptions = new RealtimeAudioFormat("audio/pcm", 24000),
            Metadata = new AdditionalPropertiesDictionary { ["key1"] = "value1" },
            ToolMode = ChatToolMode.Auto,
        };

        await _session.InjectClientMessageAsync(message);
        var sent = await ReadSentMessageAsync();

        Assert.Equal("response.create", sent["type"]?.GetValue<string>());
        Assert.Equal("client_evt_001", sent["event_id"]?.GetValue<string>());
        var response = sent["response"]!.AsObject();
        Assert.Equal("Be concise", response["instructions"]?.GetValue<string>());
        Assert.Equal(1024, response["max_output_tokens"]?.GetValue<int>());
        Assert.Equal("none", response["conversation"]?.GetValue<string>());
        Assert.Equal("auto", response["tool_choice"]?.GetValue<string>());
        var modalities = response["output_modalities"]!.AsArray();
        Assert.Equal(2, modalities.Count);
        Assert.Equal("text", modalities[0]?.GetValue<string>());
        Assert.Equal("audio", modalities[1]?.GetValue<string>());
        Assert.Equal("value1", response["metadata"]!.AsObject()["key1"]?.GetValue<string>());
    }

    [Fact]
    public async Task InjectClientMessage_ConversationItemCreate_Message_SerializedCorrectly()
    {
        var contents = new List<AIContent> { new TextContent("Hello") };
        var item = new RealtimeContentItem(contents, "item_001", ChatRole.User);
        var message = new RealtimeClientConversationItemCreateMessage(item, "prev_item_001");

        await _session.InjectClientMessageAsync(message);
        var sent = await ReadSentMessageAsync();

        Assert.Equal("conversation.item.create", sent["type"]?.GetValue<string>());
        Assert.Equal("prev_item_001", sent["previous_item_id"]?.GetValue<string>());
        var itemObj = sent["item"]!.AsObject();
        Assert.Equal("message", itemObj["type"]?.GetValue<string>());
        Assert.Equal("item_001", itemObj["id"]?.GetValue<string>());
        Assert.Equal("user", itemObj["role"]?.GetValue<string>());
        var contentArray = itemObj["content"]!.AsArray();
        Assert.Single(contentArray);
        Assert.Equal("input_text", contentArray[0]!["type"]?.GetValue<string>());
        Assert.Equal("Hello", contentArray[0]!["text"]?.GetValue<string>());
    }

    [Fact]
    public async Task InjectClientMessage_ConversationItemCreate_FunctionResult_SerializedCorrectly()
    {
        var functionResult = new FunctionResultContent("call_001", "Sunny, 72F");
        var item = new RealtimeContentItem(new List<AIContent> { functionResult }, "item_002");
        var message = new RealtimeClientConversationItemCreateMessage(item);

        await _session.InjectClientMessageAsync(message);
        var sent = await ReadSentMessageAsync();

        Assert.Equal("conversation.item.create", sent["type"]?.GetValue<string>());
        var itemObj = sent["item"]!.AsObject();
        Assert.Equal("function_call_output", itemObj["type"]?.GetValue<string>());
        Assert.Equal("call_001", itemObj["call_id"]?.GetValue<string>());
    }

    [Fact]
    public async Task InjectClientMessage_ConversationItemCreate_FunctionCall_SerializedCorrectly()
    {
        var functionCall = new FunctionCallContent("call_002", "get_weather", new Dictionary<string, object?> { ["city"] = "Seattle" });
        var item = new RealtimeContentItem(new List<AIContent> { functionCall }, "item_003");
        var message = new RealtimeClientConversationItemCreateMessage(item);

        await _session.InjectClientMessageAsync(message);
        var sent = await ReadSentMessageAsync();

        var itemObj = sent["item"]!.AsObject();
        Assert.Equal("function_call", itemObj["type"]?.GetValue<string>());
        Assert.Equal("call_002", itemObj["call_id"]?.GetValue<string>());
        Assert.Equal("get_weather", itemObj["name"]?.GetValue<string>());
        Assert.NotNull(itemObj["arguments"]);
    }

    [Fact]
    public async Task InjectClientMessage_AudioBufferCommit_SerializedCorrectly()
    {
        await _session.InjectClientMessageAsync(new RealtimeClientInputAudioBufferCommitMessage());
        var sent = await ReadSentMessageAsync();
        Assert.Equal("input_audio_buffer.commit", sent["type"]?.GetValue<string>());
    }

    [Fact]
    public async Task InjectClientMessage_AudioBufferAppend_SerializedCorrectly()
    {
        var audioBytes = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var dataContent = new DataContent(audioBytes, "audio/pcm");
        var message = new RealtimeClientInputAudioBufferAppendMessage(dataContent);

        await _session.InjectClientMessageAsync(message);
        var sent = await ReadSentMessageAsync();

        Assert.Equal("input_audio_buffer.append", sent["type"]?.GetValue<string>());
        var audioBase64 = sent["audio"]!.GetValue<string>();
        Assert.NotNull(audioBase64);
        var decoded = Convert.FromBase64String(audioBase64);
        Assert.Equal(audioBytes, decoded);
    }

    [Fact]
    public async Task InjectClientMessage_RawRepresentation_String_SerializedCorrectly()
    {
        var rawJson = """{"type":"custom.event","data":"test"}""";
        var message = new RealtimeClientMessage { RawRepresentation = rawJson };

        await _session.InjectClientMessageAsync(message);
        var sent = await ReadSentMessageAsync();

        Assert.Equal("custom.event", sent["type"]?.GetValue<string>());
        Assert.Equal("test", sent["data"]?.GetValue<string>());
    }

    [Fact]
    public async Task InjectClientMessage_RawRepresentation_String_WithEventId_PreservesEventId()
    {
        var rawJson = """{"type":"custom.event","data":"test"}""";
        var message = new RealtimeClientMessage { RawRepresentation = rawJson, EventId = "evt_custom_001" };

        await _session.InjectClientMessageAsync(message);
        var sent = await ReadSentMessageAsync();

        Assert.Equal("custom.event", sent["type"]?.GetValue<string>());
        Assert.Equal("evt_custom_001", sent["event_id"]?.GetValue<string>());
    }

    [Fact]
    public async Task InjectClientMessage_RawRepresentation_String_WithEventIdInJson_DoesNotOverwrite()
    {
        var rawJson = """{"type":"custom.event","event_id":"evt_from_json"}""";
        var message = new RealtimeClientMessage { RawRepresentation = rawJson, EventId = "evt_from_property" };

        await _session.InjectClientMessageAsync(message);
        var sent = await ReadSentMessageAsync();

        // The event_id already present in the raw JSON should take precedence.
        Assert.Equal("evt_from_json", sent["event_id"]?.GetValue<string>());
    }

    [Fact]
    public async Task InjectClientMessage_RawRepresentation_JsonObject_SerializedCorrectly()
    {
        var rawObj = new JsonObject { ["type"] = "custom.event2", ["payload"] = "data" };
        var message = new RealtimeClientMessage { RawRepresentation = rawObj };

        await _session.InjectClientMessageAsync(message);
        var sent = await ReadSentMessageAsync();

        Assert.Equal("custom.event2", sent["type"]?.GetValue<string>());
        Assert.Equal("data", sent["payload"]?.GetValue<string>());
    }

    #region MCP Tool Serialization Tests

    [Fact]
    public async Task SessionUpdate_HostedMcpServerTool_WithUrl_SerializedCorrectly()
    {
        var mcpTool = new HostedMcpServerTool("my-server", new Uri("https://mcp.example.com/api"))
        {
            ServerDescription = "A test MCP server",
            AllowedTools = new List<string> { "search", "lookup" },
            ApprovalMode = HostedMcpServerToolApprovalMode.NeverRequire,
        };

        var options = new RealtimeSessionOptions
        {
            SessionKind = RealtimeSessionKind.Realtime,
            Tools = [mcpTool],
        };

        await _session.UpdateAsync(options);
        var sent = await ReadSentMessageAsync();

        var sessionObj = sent["session"]!.AsObject();
        var toolsArray = sessionObj["tools"]!.AsArray();
        Assert.Single(toolsArray);

        var toolObj = toolsArray[0]!.AsObject();
        Assert.Equal("mcp", toolObj["type"]?.GetValue<string>());
        Assert.Equal("my-server", toolObj["server_label"]?.GetValue<string>());
        Assert.Equal("https://mcp.example.com/api", toolObj["server_url"]?.GetValue<string>());
        Assert.Equal("A test MCP server", toolObj["server_description"]?.GetValue<string>());
        Assert.Equal("never", toolObj["require_approval"]?.GetValue<string>());
        Assert.Null(toolObj["connector_id"]);

        var allowedTools = toolObj["allowed_tools"]!.AsArray();
        Assert.Equal(2, allowedTools.Count);
        Assert.Equal("search", allowedTools[0]?.GetValue<string>());
        Assert.Equal("lookup", allowedTools[1]?.GetValue<string>());
    }

    [Fact]
    public async Task SessionUpdate_HostedMcpServerTool_WithConnectorId_SerializedCorrectly()
    {
        var mcpTool = new HostedMcpServerTool("connector-server", "my-connector-id")
        {
            AuthorizationToken = "test-token-123",
            ApprovalMode = HostedMcpServerToolApprovalMode.AlwaysRequire,
        };

        var options = new RealtimeSessionOptions
        {
            SessionKind = RealtimeSessionKind.Realtime,
            Tools = [mcpTool],
        };

        await _session.UpdateAsync(options);
        var sent = await ReadSentMessageAsync();

        var sessionObj = sent["session"]!.AsObject();
        var toolObj = sessionObj["tools"]!.AsArray()[0]!.AsObject();
        Assert.Equal("mcp", toolObj["type"]?.GetValue<string>());
        Assert.Equal("connector-server", toolObj["server_label"]?.GetValue<string>());
        Assert.Equal("my-connector-id", toolObj["connector_id"]?.GetValue<string>());
        Assert.Null(toolObj["server_url"]);
        Assert.Equal("always", toolObj["require_approval"]?.GetValue<string>());

        var authObj = toolObj["authorization"]!.AsObject();
        Assert.Equal("test-token-123", authObj["token"]?.GetValue<string>());
    }

    [Fact]
    public async Task SessionUpdate_HostedMcpServerTool_WithHeaders_SerializedCorrectly()
    {
        var mcpTool = new HostedMcpServerTool("header-server", new Uri("https://mcp.example.com/"));
        mcpTool.Headers["X-Custom"] = "custom-value";
        mcpTool.Headers["Authorization"] = "Bearer my-token";

        var options = new RealtimeSessionOptions
        {
            SessionKind = RealtimeSessionKind.Realtime,
            Tools = [mcpTool],
        };

        await _session.UpdateAsync(options);
        var sent = await ReadSentMessageAsync();

        var toolObj = sent["session"]!.AsObject()["tools"]!.AsArray()[0]!.AsObject();
        var headersObj = toolObj["headers"]!.AsObject();
        Assert.Equal("custom-value", headersObj["X-Custom"]?.GetValue<string>());
        Assert.Equal("Bearer my-token", headersObj["Authorization"]?.GetValue<string>());
    }

    [Fact]
    public async Task SessionUpdate_HostedMcpServerTool_SpecificApproval_SerializedCorrectly()
    {
        var mcpTool = new HostedMcpServerTool("specific-server", new Uri("https://mcp.example.com/"))
        {
            ApprovalMode = HostedMcpServerToolApprovalMode.RequireSpecific(
                alwaysRequireApprovalToolNames: new List<string> { "delete_file" },
                neverRequireApprovalToolNames: new List<string> { "read_file", "list_files" }),
        };

        var options = new RealtimeSessionOptions
        {
            SessionKind = RealtimeSessionKind.Realtime,
            Tools = [mcpTool],
        };

        await _session.UpdateAsync(options);
        var sent = await ReadSentMessageAsync();

        var toolObj = sent["session"]!.AsObject()["tools"]!.AsArray()[0]!.AsObject();
        var approvalObj = toolObj["require_approval"]!.AsObject();

        var alwaysObj = approvalObj["always"]!.AsObject();
        var alwaysNames = alwaysObj["tool_names"]!.AsArray();
        Assert.Single(alwaysNames);
        Assert.Equal("delete_file", alwaysNames[0]?.GetValue<string>());

        var neverObj = approvalObj["never"]!.AsObject();
        var neverNames = neverObj["tool_names"]!.AsArray();
        Assert.Equal(2, neverNames.Count);
        Assert.Equal("read_file", neverNames[0]?.GetValue<string>());
        Assert.Equal("list_files", neverNames[1]?.GetValue<string>());
    }

    [Fact]
    public async Task SessionUpdate_MixedTools_AIFunctionAndMcpTool_SerializedCorrectly()
    {
        var aiFunction = AIFunctionFactory.Create(() => "result", "test_func", "A test function");
        var mcpTool = new HostedMcpServerTool("mcp-server", new Uri("https://mcp.example.com/"));

        var options = new RealtimeSessionOptions
        {
            SessionKind = RealtimeSessionKind.Realtime,
            Tools = [aiFunction, mcpTool],
        };

        await _session.UpdateAsync(options);
        var sent = await ReadSentMessageAsync();

        var toolsArray = sent["session"]!.AsObject()["tools"]!.AsArray();
        Assert.Equal(2, toolsArray.Count);
        Assert.Equal("function", toolsArray[0]!["type"]?.GetValue<string>());
        Assert.Equal("mcp", toolsArray[1]!["type"]?.GetValue<string>());
    }

    [Fact]
    public async Task ResponseCreate_HostedMcpServerTool_InToolsList_SerializedCorrectly()
    {
        var mcpTool = new HostedMcpServerTool("resp-server", new Uri("https://mcp.example.com/"))
        {
            ServerDescription = "Response-level MCP",
            ApprovalMode = HostedMcpServerToolApprovalMode.NeverRequire,
        };

        var message = new RealtimeClientResponseCreateMessage
        {
            Tools = [mcpTool],
        };

        await _session.InjectClientMessageAsync(message);
        var sent = await ReadSentMessageAsync();

        var responseObj = sent["response"]!.AsObject();
        var toolsArray = responseObj["tools"]!.AsArray();
        Assert.Single(toolsArray);
        var toolObj = toolsArray[0]!.AsObject();
        Assert.Equal("mcp", toolObj["type"]?.GetValue<string>());
        Assert.Equal("resp-server", toolObj["server_label"]?.GetValue<string>());
        Assert.Equal("https://mcp.example.com/", toolObj["server_url"]?.GetValue<string>());
        Assert.Equal("Response-level MCP", toolObj["server_description"]?.GetValue<string>());
        Assert.Equal("never", toolObj["require_approval"]?.GetValue<string>());
    }

    #endregion

    #region MCP Server Event Parsing Tests

    [Fact]
    public async Task ProcessServerEvent_McpCallCompleted_ParsedCorrectly()
    {
        var json = """{"type":"mcp_call.completed","event_id":"evt_mcp_001","response_id":"resp_010","output_index":0,"item":{"type":"mcp_call","id":"mcp_call_001","name":"search","server_label":"my-mcp","arguments":"{\"query\":\"weather\"}","output":"Sunny, 72F"}}""";

        await SendServerMessageAsync(json);
        var msg = await ReadNextServerMessageAsync();

        var outputMsg = Assert.IsType<RealtimeServerResponseOutputItemMessage>(msg);
        Assert.Equal(RealtimeServerMessageType.McpCallCompleted, outputMsg.Type);
        Assert.Equal("evt_mcp_001", outputMsg.EventId);
        Assert.Equal("resp_010", outputMsg.ResponseId);
        Assert.Equal(0, outputMsg.OutputIndex);

        Assert.NotNull(outputMsg.Item);
        Assert.Equal("mcp_call_001", outputMsg.Item.Id);
        Assert.Equal(2, outputMsg.Item.Contents.Count);

        var callContent = Assert.IsType<McpServerToolCallContent>(outputMsg.Item.Contents[0]);
        Assert.Equal("mcp_call_001", callContent.CallId);
        Assert.Equal("search", callContent.ToolName);
        Assert.Equal("my-mcp", callContent.ServerName);
        Assert.NotNull(callContent.Arguments);
        Assert.Equal("weather", callContent.Arguments["query"]?.ToString());

        var resultContent = Assert.IsType<McpServerToolResultContent>(outputMsg.Item.Contents[1]);
        Assert.Equal("mcp_call_001", resultContent.CallId);
        Assert.NotNull(resultContent.Output);
        var textOutput = Assert.IsType<TextContent>(resultContent.Output[0]);
        Assert.Equal("Sunny, 72F", textOutput.Text);
    }

    [Fact]
    public async Task ProcessServerEvent_McpCallFailed_ParsedWithError()
    {
        var json = """{"type":"mcp_call.failed","event_id":"evt_mcp_002","item":{"type":"mcp_call","id":"mcp_call_002","name":"delete_file","server_label":"file-server","arguments":"{\"path\":\"/tmp/x\"}","error":{"type":"tool_execution_error","message":"Permission denied"}}}""";

        await SendServerMessageAsync(json);
        var msg = await ReadNextServerMessageAsync();

        var outputMsg = Assert.IsType<RealtimeServerResponseOutputItemMessage>(msg);
        Assert.Equal(RealtimeServerMessageType.McpCallFailed, outputMsg.Type);

        Assert.NotNull(outputMsg.Item);
        Assert.Equal(2, outputMsg.Item.Contents.Count);

        var callContent = Assert.IsType<McpServerToolCallContent>(outputMsg.Item.Contents[0]);
        Assert.Equal("delete_file", callContent.ToolName);
        Assert.Equal("file-server", callContent.ServerName);

        var resultContent = Assert.IsType<McpServerToolResultContent>(outputMsg.Item.Contents[1]);
        var errorContent = Assert.IsType<ErrorContent>(resultContent.Output![0]);
        Assert.Contains("Permission denied", errorContent.Message);
    }

    [Fact]
    public async Task ProcessServerEvent_McpCallInProgress_ParsedCorrectly()
    {
        var json = """{"type":"mcp_call.in_progress","event_id":"evt_mcp_003","item_id":"mcp_call_003"}""";

        await SendServerMessageAsync(json);
        var msg = await ReadNextServerMessageAsync();

        var outputMsg = Assert.IsType<RealtimeServerResponseOutputItemMessage>(msg);
        Assert.Equal(RealtimeServerMessageType.McpCallInProgress, outputMsg.Type);
        Assert.Equal("evt_mcp_003", outputMsg.EventId);
        Assert.NotNull(outputMsg.Item);
        Assert.Equal("mcp_call_003", outputMsg.Item.Id);
    }

    [Fact]
    public async Task ProcessServerEvent_McpListToolsCompleted_ParsedWithToolsList()
    {
        var json = """{"type":"mcp_list_tools.completed","event_id":"evt_mcp_004","item_id":"list_001","item":{"type":"mcp_list_tools","id":"list_001","server_label":"my-mcp","tools":[{"name":"search","description":"Search the web","input_schema":{"type":"object"}},{"name":"lookup","description":"Lookup a value"}]}}""";

        await SendServerMessageAsync(json);
        var msg = await ReadNextServerMessageAsync();

        var outputMsg = Assert.IsType<RealtimeServerResponseOutputItemMessage>(msg);
        Assert.Equal(RealtimeServerMessageType.McpListToolsCompleted, outputMsg.Type);
        Assert.Equal("evt_mcp_004", outputMsg.EventId);
        Assert.NotNull(outputMsg.RawRepresentation);

        Assert.NotNull(outputMsg.Item);
        Assert.Equal("list_001", outputMsg.Item.Id);
        Assert.Equal(2, outputMsg.Item.Contents.Count);

        var tool1 = Assert.IsType<McpServerToolCallContent>(outputMsg.Item.Contents[0]);
        Assert.Equal("search", tool1.ToolName);
        Assert.Equal("my-mcp", tool1.ServerName);

        var tool2 = Assert.IsType<McpServerToolCallContent>(outputMsg.Item.Contents[1]);
        Assert.Equal("lookup", tool2.ToolName);
        Assert.Equal("my-mcp", tool2.ServerName);
    }

    [Fact]
    public async Task ProcessServerEvent_McpListToolsInProgress_ParsedWithItemId()
    {
        var json = """{"type":"mcp_list_tools.in_progress","event_id":"evt_mcp_005","item_id":"list_002"}""";

        await SendServerMessageAsync(json);
        var msg = await ReadNextServerMessageAsync();

        var outputMsg = Assert.IsType<RealtimeServerResponseOutputItemMessage>(msg);
        Assert.Equal(RealtimeServerMessageType.McpListToolsInProgress, outputMsg.Type);
        Assert.NotNull(outputMsg.Item);
        Assert.Equal("list_002", outputMsg.Item.Id);
    }

    [Fact]
    public async Task ProcessServerEvent_McpListToolsFailed_ParsedWithItemId()
    {
        var json = """{"type":"mcp_list_tools.failed","event_id":"evt_mcp_006","item_id":"list_003"}""";

        await SendServerMessageAsync(json);
        var msg = await ReadNextServerMessageAsync();

        var outputMsg = Assert.IsType<RealtimeServerResponseOutputItemMessage>(msg);
        Assert.Equal(RealtimeServerMessageType.McpListToolsFailed, outputMsg.Type);
        Assert.NotNull(outputMsg.Item);
        Assert.Equal("list_003", outputMsg.Item.Id);
    }

    [Fact]
    public async Task ProcessServerEvent_McpApprovalRequest_ParsedCorrectly()
    {
        var json = """{"type":"conversation.item.added","event_id":"evt_mcp_007","item":{"type":"mcp_approval_request","id":"approval_001","name":"charge_card","server_label":"payment-mcp","arguments":"{\"amount\":99.99}"}}""";

        await SendServerMessageAsync(json);
        var msg = await ReadNextServerMessageAsync();

        var outputMsg = Assert.IsType<RealtimeServerResponseOutputItemMessage>(msg);
        Assert.NotNull(outputMsg.Item);
        Assert.Single(outputMsg.Item.Contents);

        var approvalRequest = Assert.IsType<McpServerToolApprovalRequestContent>(outputMsg.Item.Contents[0]);
        Assert.Equal("approval_001", approvalRequest.Id);
        Assert.Equal("charge_card", approvalRequest.ToolCall.ToolName);
        Assert.Equal("payment-mcp", approvalRequest.ToolCall.ServerName);
        Assert.NotNull(approvalRequest.ToolCall.Arguments);
    }

    [Fact]
    public async Task ProcessServerEvent_ConversationItemDone_WithMcpCall_ParsedCorrectly()
    {
        var json = """{"type":"conversation.item.done","event_id":"evt_mcp_008","item":{"type":"mcp_call","id":"mcp_done_001","name":"read_file","server_label":"fs-server","arguments":"{\"path\":\"/readme.md\"}","output":"# Hello"}}""";

        await SendServerMessageAsync(json);
        var msg = await ReadNextServerMessageAsync();

        var outputMsg = Assert.IsType<RealtimeServerResponseOutputItemMessage>(msg);
        Assert.Equal(RealtimeServerMessageType.ResponseOutputItemDone, outputMsg.Type);
        Assert.NotNull(outputMsg.Item);

        var callContent = Assert.IsType<McpServerToolCallContent>(outputMsg.Item.Contents[0]);
        Assert.Equal("read_file", callContent.ToolName);

        var resultContent = Assert.IsType<McpServerToolResultContent>(outputMsg.Item.Contents[1]);
        var textOutput = Assert.IsType<TextContent>(resultContent.Output![0]);
        Assert.Equal("# Hello", textOutput.Text);
    }

    [Fact]
    public async Task ProcessServerEvent_ConversationItemAdded_WithRegularMessage_ParsedCorrectly()
    {
        var json = """{"type":"conversation.item.added","event_id":"evt_conv_001","item":{"type":"message","id":"msg_conv_001","role":"user","content":[{"type":"input_text","text":"Hello"}]}}""";

        await SendServerMessageAsync(json);
        var msg = await ReadNextServerMessageAsync();

        var outputMsg = Assert.IsType<RealtimeServerResponseOutputItemMessage>(msg);
        Assert.Equal(RealtimeServerMessageType.ResponseOutputItemAdded, outputMsg.Type);
        Assert.NotNull(outputMsg.Item);
        Assert.Equal("msg_conv_001", outputMsg.Item.Id);
        Assert.Equal(ChatRole.User, outputMsg.Item.Role);
        var textContent = Assert.IsType<TextContent>(outputMsg.Item.Contents[0]);
        Assert.Equal("Hello", textContent.Text);
    }

    [Fact]
    public async Task ProcessServerEvent_ConversationItemAdded_WithUnknownType_ReturnsRawContent()
    {
        var json = """{"type":"conversation.item.added","event_id":"evt_conv_002","item":{"type":"unknown_item_type","id":"unknown_001"}}""";

        await SendServerMessageAsync(json);
        var msg = await ReadNextServerMessageAsync();

        var outputMsg = Assert.IsType<RealtimeServerResponseOutputItemMessage>(msg);
        Assert.Equal(RealtimeServerMessageType.RawContentOnly, outputMsg.Type);
        Assert.NotNull(outputMsg.RawRepresentation);
    }

    #endregion

    #region MCP Approval Response Sending Tests

    [Fact]
    public async Task InjectClientMessage_McpApprovalResponse_Approved_SerializedCorrectly()
    {
        var approvalResponse = new McpServerToolApprovalResponseContent("approval_001", approved: true);
        var item = new RealtimeContentItem(new List<AIContent> { approvalResponse }, "resp_item_001");
        var message = new RealtimeClientConversationItemCreateMessage(item);

        await _session.InjectClientMessageAsync(message);
        var sent = await ReadSentMessageAsync();

        Assert.Equal("conversation.item.create", sent["type"]?.GetValue<string>());
        var itemObj = sent["item"]!.AsObject();
        Assert.Equal("mcp_approval_response", itemObj["type"]?.GetValue<string>());
        Assert.Equal("approval_001", itemObj["approval_request_id"]?.GetValue<string>());
        Assert.True(itemObj["approve"]?.GetValue<bool>());
    }

    [Fact]
    public async Task InjectClientMessage_McpApprovalResponse_Rejected_SerializedCorrectly()
    {
        var approvalResponse = new McpServerToolApprovalResponseContent("approval_002", approved: false);
        var item = new RealtimeContentItem(new List<AIContent> { approvalResponse }, "resp_item_002");
        var message = new RealtimeClientConversationItemCreateMessage(item);

        await _session.InjectClientMessageAsync(message);
        var sent = await ReadSentMessageAsync();

        var itemObj = sent["item"]!.AsObject();
        Assert.Equal("mcp_approval_response", itemObj["type"]?.GetValue<string>());
        Assert.Equal("approval_002", itemObj["approval_request_id"]?.GetValue<string>());
        Assert.False(itemObj["approve"]?.GetValue<bool>());
    }

    #endregion

    private async Task SendServerMessageAsync(string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        await _serverWebSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private async Task<JsonObject> ReadSentMessageAsync()
    {
        var buffer = new byte[1024 * 16];
        var result = await _serverWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
        return JsonSerializer.Deserialize<JsonObject>(json)!;
    }

    private async Task<RealtimeServerMessage> ReadNextServerMessageAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var enumerator = _session.GetStreamingResponseAsync(EmptyUpdatesAsync(), cts.Token)
            .GetAsyncEnumerator(cts.Token);
        try
        {
            if (await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                return enumerator.Current;
            }
        }
        finally
        {
            await enumerator.DisposeAsync().ConfigureAwait(false);
        }

        throw new InvalidOperationException("No server message received within timeout");
    }

    private static async IAsyncEnumerable<RealtimeClientMessage> EmptyUpdatesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
        yield break;
    }

    /// <summary>
    /// A WebSocket implementation backed by channels, used for in-process testing without a real network connection.
    /// </summary>
    internal sealed class ChannelWebSocket : WebSocket
    {
        private readonly ChannelReader<(byte[] data, WebSocketMessageType messageType, bool endOfMessage)> _reader;
        private readonly ChannelWriter<(byte[] data, WebSocketMessageType messageType, bool endOfMessage)> _writer;
        private WebSocketState _state = WebSocketState.Open;

        public ChannelWebSocket(
            ChannelReader<(byte[] data, WebSocketMessageType messageType, bool endOfMessage)> reader,
            ChannelWriter<(byte[] data, WebSocketMessageType messageType, bool endOfMessage)> writer)
        {
            _reader = reader;
            _writer = writer;
        }

        public override WebSocketCloseStatus? CloseStatus => _state == WebSocketState.Closed ? WebSocketCloseStatus.NormalClosure : null;
        public override string? CloseStatusDescription => null;
        public override WebSocketState State => _state;
        public override string? SubProtocol => null;

        public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            var (data, type, endOfMessage) = await _reader.ReadAsync(cancellationToken).ConfigureAwait(false);

            if (type == WebSocketMessageType.Close)
            {
                _state = WebSocketState.CloseReceived;
                return new WebSocketReceiveResult(0, WebSocketMessageType.Close, true, WebSocketCloseStatus.NormalClosure, null);
            }

            var count = Math.Min(data.Length, buffer.Count);
            Array.Copy(data, 0, buffer.Array!, buffer.Offset, count);
            return new WebSocketReceiveResult(count, type, endOfMessage);
        }

        public override async Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            var data = new byte[buffer.Count];
            Array.Copy(buffer.Array!, buffer.Offset, data, 0, buffer.Count);
            await _writer.WriteAsync((data, messageType, endOfMessage), cancellationToken).ConfigureAwait(false);
        }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            _state = WebSocketState.Closed;
            _writer.TryComplete();
            return Task.CompletedTask;
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            _state = WebSocketState.CloseSent;
            _writer.TryComplete();
            return Task.CompletedTask;
        }

        public override void Abort()
        {
            _state = WebSocketState.Aborted;
            _writer.TryComplete();
        }

        public override void Dispose()
        {
            _state = WebSocketState.Closed;
            _writer.TryComplete();
        }
    }
}
