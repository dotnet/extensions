// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using Xunit;

namespace Microsoft.Extensions.AI;

public class OpenAIRealtimeTests
{
    [Fact]
    public void ConvertsAIFunctionToConversationFunctionTool_Basics()
    {
        var input = AIFunctionFactory.Create(() => { }, "MyFunction", "MyDescription");
        var result = input.ToConversationFunctionTool();

        Assert.Equal("MyFunction", result.Name);
        Assert.Equal("MyDescription", result.Description);
    }

    [Fact]
    public void ConvertsAIFunctionToConversationFunctionTool_Parameters()
    {
        var input = AIFunctionFactory.Create(MyFunction);
        var result = input.ToConversationFunctionTool();

        Assert.Equal(nameof(MyFunction), result.Name);
        Assert.Equal("This is a description", result.Description);
        Assert.Equal("""
            {
              "type": "object",
              "properties": {
                "a": {
                  "type": "integer"
                },
                "b": {
                  "description": "Another param",
                  "type": "string"
                },
                "c": {
                  "type": "object",
                  "properties": {
                    "a": {
                      "type": "integer"
                    }
                  },
                  "additionalProperties": false,
                  "required": [
                    "a"
                  ],
                  "default": "null"
                }
              },
              "required": [
                "a",
                "b"
              ]
            }
            """, result.Parameters.ToString());
    }

    [Description("This is a description")]
    private MyType MyFunction(int a, [Description("Another param")] string b, MyType? c = null)
        => throw new NotSupportedException();

    public class MyType
    {
        public int A { get; set; }
    }
}
