// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Http.Logging.Bench;

internal sealed class DropMessageLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new DropMessageLogger();

    public void Dispose()
    {
    }
}
