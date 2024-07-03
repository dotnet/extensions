// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.Logging.Sampling;

/// <summary>
/// Interface for matchin log records.
/// </summary>
public interface IMatcher
{
    bool Match(LogRecordPattern pattern);
}
