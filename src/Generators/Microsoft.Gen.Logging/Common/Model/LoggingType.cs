// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Gen.Logging.Model;

/// <summary>
/// A logger class/struct/record holding a bunch of logger methods.
/// </summary>
internal sealed class LoggingType
{
    public readonly List<LoggingMethod> Methods = new();
    public string Keyword = string.Empty;
    public string Namespace = string.Empty;
    public string Name = string.Empty;
    public LoggingType? Parent;
}
