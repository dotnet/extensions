﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Shared.Diagnostics;

internal static partial class Throw
{
    /// <summary>Throws an exception indicating that a required service is not available.</summary>
    public static InvalidOperationException CreateMissingServiceException(Type serviceType, object? serviceKey) =>
        new InvalidOperationException(serviceKey is null ?
            $"No service of type '{serviceType}' is available." :
            $"No service of type '{serviceType}' for the key '{serviceKey}' is available.");
}
