// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.DependencyInjection.Pools.Test.TestResources;

public class TestDependency
{
    public const string Message = "I'm here!";

#pragma warning disable CA1822
    public string ReadMessage() => Message;
#pragma warning restore CA1822
}
