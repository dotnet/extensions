// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
