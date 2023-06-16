// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ObjectPool.Test.TestResources;

public class TestDependency
{
    public const string DefaultMessage = "I'm here!";
    public string Message { get; } = DefaultMessage;

    public string ReadMessage() => Message;
}
