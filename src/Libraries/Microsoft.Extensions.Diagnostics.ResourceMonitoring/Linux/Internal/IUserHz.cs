// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;

/// <summary>
/// Mock Linux OS call to run tests on windows.
/// </summary>
internal interface IUserHz
{
    /// <summary>
    /// Gets value of Linux UserHz.
    /// </summary>
    long Value { get; }
}
