// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ShellToolTests
{
    [Fact]
    public void Constructor_Roundtrips()
    {
        var tool = new TestShellTool();
        Assert.Equal("local_shell", tool.Name);
        Assert.Equal("Executes a shell command and returns stdout, stderr, and exit code.", tool.Description);
        Assert.Empty(tool.AdditionalProperties);
        Assert.Equal(tool.Name, tool.ToString());
    }

    [Fact]
    public void Constructor_AdditionalProperties_Roundtrips()
    {
        var props = new Dictionary<string, object?> { ["key"] = "value" };
        var tool = new TestShellTool(props);

        Assert.Equal("local_shell", tool.Name);
        Assert.Same(props, tool.AdditionalProperties);
    }

    [Fact]
    public async Task InvokeCoreAsync_ReturnsShellResultContent()
    {
        var tool = new TestShellTool();
        var arguments = new AIFunctionArguments { ["command"] = "echo hello" };

        var result = await tool.InvokeAsync(arguments);

        var shellResult = Assert.IsType<ShellResultContent>(result);
        Assert.Equal("test-call-id", shellResult.CallId);
        Assert.NotNull(shellResult.Output);
        Assert.Single(shellResult.Output);
        Assert.Equal("hello\n", shellResult.Output[0].Stdout);
        Assert.Equal(0, shellResult.Output[0].ExitCode);
    }

    private sealed class TestShellTool : ShellTool
    {
        public TestShellTool()
        {
        }

        public TestShellTool(IReadOnlyDictionary<string, object?>? additionalProperties)
            : base(additionalProperties)
        {
        }

        protected override ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
        {
            return new ValueTask<object?>(new ShellResultContent("test-call-id")
            {
                Output =
                [
                    new ShellCommandOutput
                    {
                        Stdout = "hello\n",
                        ExitCode = 0,
                    }
                ]
            });
        }
    }
}
