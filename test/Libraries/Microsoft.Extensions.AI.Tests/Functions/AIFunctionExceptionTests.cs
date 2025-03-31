// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.AI.Functions;

public class AIFunctionExceptionTests
{
    [Fact]
    public void MessageForModel_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>("messageForModel", () => new AIFunctionException(null!));
    }

    [Fact]
    public void Message_Null()
    {
        string messageForModel = "messageForModel";

        var exception = new AIFunctionException(messageForModel, null);
        Assert.Equal(messageForModel, exception.Message);
        Assert.Contains(messageForModel, exception.ToString());
    }

    [Fact]
    public void Message_NotNull()
    {
        string messageForModel = "messageForModel";
        string baseMessage = "baseMessage";

        var exception = new AIFunctionException(messageForModel, baseMessage);
        Assert.Equal($"{baseMessage} Message for model: {messageForModel}", exception.Message);
        Assert.Contains($"{baseMessage} Message for model: {messageForModel}", exception.ToString());
    }
}
