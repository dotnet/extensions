// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.AI;

#pragma warning disable S125 // Sections of code should not be commented out

/// <summary>Shared utility methods for integration tests.</summary>
internal static class IntegrationTestHelpers
{
    /// <summary>Gets a <see cref="Uri"/> to use for testing, or null if the associated tests should be disabled.</summary>
    public static Uri? GetOllamaUri()
    {
        return TestRunnerConfiguration.Instance["Ollama:Endpoint"] is string endpoint
            ? new Uri(endpoint)
            : null;
    }
}
