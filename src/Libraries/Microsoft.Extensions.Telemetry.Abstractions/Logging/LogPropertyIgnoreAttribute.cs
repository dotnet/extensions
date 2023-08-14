// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Telemetry.Logging;

/// <summary>
/// Indicates that a tag should not be logged.
/// </summary>
/// <seealso cref="LoggerMessageAttribute"/>.
[AttributeUsage(AttributeTargets.Property)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class LogPropertyIgnoreAttribute : Attribute
{
}
