// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.Analyzers.Async
{
    internal class Descriptors
    {
        public static DiagnosticDescriptor ASYNC0001SynchronouslyBlockingMethod = new DiagnosticDescriptor(
            id: "ASYNC0001",
            title: "Synchronously blocking method should not be used in async context",
            messageFormat: "Synchronously blocking method should not be used in async context",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);
    }
}
