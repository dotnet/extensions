// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

/// <summary>Provides status codes for when errors occur as part of the function calling loop.</summary>
internal enum FunctionInvocationStatus
{
    /// <summary>The operation completed successfully.</summary>
    RanToCompletion,

    /// <summary>The requested function could not be found.</summary>
    NotFound,

    /// <summary>The function call failed with an exception.</summary>
    Exception,
}
