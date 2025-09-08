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

    [Fact]
    public void Equality_RequireSpecific_WorksAsExpected()
    {
        var mode1 = HostedMcpServerToolApprovalMode.RequireSpecific(["ToolA", "ToolB"], ["ToolC"]);
        var mode2 = HostedMcpServerToolApprovalMode.RequireSpecific(["ToolA", "ToolB"], ["ToolC"]);
        Assert.Equal(mode1, mode2);
        Assert.Equal(mode1.GetHashCode(), mode2.GetHashCode());

        Assert.NotNull(mode1.AlwaysRequireApprovalToolNames);
        mode1.AlwaysRequireApprovalToolNames.Add("ToolD");
        Assert.NotEqual(mode1, mode2);
        Assert.NotEqual(mode1.GetHashCode(), mode2.GetHashCode());

        Assert.NotNull(mode2.AlwaysRequireApprovalToolNames);
        mode2.AlwaysRequireApprovalToolNames.Add("ToolD");
        Assert.Equal(mode1, mode2);
        Assert.Equal(mode1.GetHashCode(), mode2.GetHashCode());

        Assert.NotNull(mode2.NeverRequireApprovalToolNames);
        mode2.NeverRequireApprovalToolNames.Add("ToolE");
        Assert.NotEqual(mode1, mode2);
        Assert.NotEqual(mode1.GetHashCode(), mode2.GetHashCode());

        Assert.NotNull(mode1.NeverRequireApprovalToolNames);
        mode1.NeverRequireApprovalToolNames.Add("ToolE");
        Assert.Equal(mode1, mode2);
        Assert.Equal(mode1.GetHashCode(), mode2.GetHashCode());

        var mode3 = HostedMcpServerToolApprovalMode.RequireSpecific(null, null);
        Assert.Equal(mode3.GetHashCode(), mode3.GetHashCode());
        var mode4 = HostedMcpServerToolApprovalMode.RequireSpecific(["a"], null);
        Assert.Equal(mode4.GetHashCode(), mode4.GetHashCode());
        Assert.NotEqual(mode3, mode4);
        Assert.NotEqual(mode3.GetHashCode(), mode4.GetHashCode());

        var mode5 = HostedMcpServerToolApprovalMode.RequireSpecific(null, ["b"]);
        Assert.Equal(mode5.GetHashCode(), mode5.GetHashCode());
        Assert.NotEqual(mode3, mode5);
        Assert.NotEqual(mode3.GetHashCode(), mode5.GetHashCode());
        Assert.NotEqual(mode4, mode5);
        Assert.NotEqual(mode4.GetHashCode(), mode5.GetHashCode());

        var mode6 = HostedMcpServerToolApprovalMode.RequireSpecific([], []);
        Assert.Equal(mode6.GetHashCode(), mode6.GetHashCode());
        Assert.NotEqual(mode3, mode6);
    }
}
