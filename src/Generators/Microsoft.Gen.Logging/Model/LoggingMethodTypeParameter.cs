// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Gen.Logging.Model;

/// <summary>
/// A type parameter of a generic logging method.
/// </summary>
internal sealed class LoggingMethodTypeParameter
{
    public string Name = string.Empty;
    public string? Constraints;
}
