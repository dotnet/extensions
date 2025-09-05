// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class HostedMcpServerToolApprovalModeTests
{
    [Fact]
    public void Singletons_Idempotent()
    {
        Assert.Same(HostedMcpServerToolApprovalMode.AlwaysRequire, HostedMcpServerToolApprovalMode.AlwaysRequire);
        Assert.Same(HostedMcpServerToolApprovalMode.NeverRequire, HostedMcpServerToolApprovalMode.NeverRequire);
    }

    [Fact]
    public void Serialization_NeverRequire_Roundtrips()
    {
        string json = JsonSerializer.Serialize(HostedMcpServerToolApprovalMode.NeverRequire, TestJsonSerializerContext.Default.HostedMcpServerToolApprovalMode);
        Assert.Equal("""{"$type":"never"}""", json);

        HostedMcpServerToolApprovalMode? result = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.HostedMcpServerToolApprovalMode);
        Assert.Equal(HostedMcpServerToolApprovalMode.NeverRequire, result);
    }

    [Fact]
    public void Serialization_AlwaysRequire_Roundtrips()
    {
        string json = JsonSerializer.Serialize(HostedMcpServerToolApprovalMode.AlwaysRequire, TestJsonSerializerContext.Default.HostedMcpServerToolApprovalMode);
        Assert.Equal("""{"$type":"always"}""", json);

        HostedMcpServerToolApprovalMode? result = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.HostedMcpServerToolApprovalMode);
        Assert.Equal(HostedMcpServerToolApprovalMode.AlwaysRequire, result);
    }

    [Fact]
    public void Serialization_RequireSpecific_Roundtrips()
    {
        var requireSpecific = HostedMcpServerToolApprovalMode.RequireSpecific(["ToolA", "ToolB"], ["ToolC"]);
        string json = JsonSerializer.Serialize(requireSpecific, TestJsonSerializerContext.Default.HostedMcpServerToolApprovalMode);
        Assert.Equal("""{"$type":"requireSpecific","alwaysRequireApprovalToolNames":["ToolA","ToolB"],"neverRequireApprovalToolNames":["ToolC"]}""", json);

        HostedMcpServerToolApprovalMode? result = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.HostedMcpServerToolApprovalMode);
        Assert.Equal(requireSpecific, result);
    }
}
