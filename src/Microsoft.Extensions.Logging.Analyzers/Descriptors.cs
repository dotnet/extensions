// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Logging.Analyzers
{
    internal class Descriptors
    {
        public static DiagnosticDescriptor MEL1NumericsInFormatString = new DiagnosticDescriptor(
            "MEL1", "Numerics should not be used in logging format string",
            "Numerics should not be used in logging format string", "Usage", DiagnosticSeverity.Info, true);

        public static DiagnosticDescriptor MEL2ConcatenationInFormatString = new DiagnosticDescriptor(
            "MEL2", "Logging format string should not be dynamically generated",
            "Logging format string should not be dynamically generated", "Usage", DiagnosticSeverity.Info, true);

        public static DiagnosticDescriptor MEL3FormatParameterCountMismatch = new DiagnosticDescriptor(
            "MEL3", "Logging format string parameter count mismatch",
            "Logging format string parameter count mismatch", "Usage", DiagnosticSeverity.Warning, true);
    }
}