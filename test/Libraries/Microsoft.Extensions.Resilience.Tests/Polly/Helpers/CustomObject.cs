// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Resilience.Polly.Test.Helpers;

public sealed class CustomObject : IDisposable
{
    public string? Content { get; private set; }

    public CustomObject(string content)
    {
        Content = content;
    }

    public void Dispose()
    {
        Content = null;
    }
}
