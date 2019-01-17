// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Logging.Analyzers
{
    internal class Descriptors
    {
        public static DiagnosticDescriptor MEL0001SynchronouslyBlockingMethod = new DiagnosticDescriptor(
            "ASYNC0001", "Synchronously blocking method should not be used in async context",
            "Synchronously blocking method should not be used in async context", "Usage", DiagnosticSeverity.Info, true);
    }
}
