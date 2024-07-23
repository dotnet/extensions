// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Interop;

/// <summary>
/// Blittable version of Windows BOOL type. It is convenient in situations where
/// manual marshalling is required, or to avoid overhead of regular bool marshalling.
/// </summary>
/// <remarks>
/// Some Windows APIs return arbitrary integer values although the return type is defined
/// as BOOL. It is best to never compare BOOL to TRUE. Always use bResult != BOOL.FALSE
/// or bResult == BOOL.FALSE .
/// </remarks>
#pragma warning disable S1939 // Inheritance list should not be redundant
internal enum BOOL : int
#pragma warning restore S1939 // Inheritance list should not be redundant
{
    FALSE = 0,
    TRUE = 1,
}

