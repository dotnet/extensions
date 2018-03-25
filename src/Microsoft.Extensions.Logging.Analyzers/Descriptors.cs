// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Logging.Analyzers
{
    internal class Descriptors
    {
        public static DiagnosticDescriptor MEL0001NumericsInFormatString = new DiagnosticDescriptor(
            "MEL0001", "Numerics should not be used in logging format string",
            "Numerics should not be used in logging format string", "Usage", DiagnosticSeverity.Info, true);

        public static DiagnosticDescriptor MEL0002ConcatenationInFormatString = new DiagnosticDescriptor(
            "MEL0002", "Logging format string should not be dynamically generated",
            "Logging format string should not be dynamically generated", "Usage", DiagnosticSeverity.Info, true);

        public static DiagnosticDescriptor MEL0003FormatParameterCountMismatch = new DiagnosticDescriptor(
            "MEL0003", "Logging format string parameter count mismatch",
            "Logging format string parameter count mismatch", "Usage", DiagnosticSeverity.Warning, true);

        public static DiagnosticDescriptor MEL0004UseCompiledLogMessages = new DiagnosticDescriptor(
            "MEL0004", "Use compiled log messages",
            "For improved performance, use pre-compiled log messages instead of calling '{0}' with a string message.", "Performance", DiagnosticSeverity.Info, false);

        public static DiagnosticDescriptor MEL0005UsePascalCasedLogMessageTokens = new DiagnosticDescriptor(
            "MEL0005", "Use PascalCase for log message tokens",
            "For consistency with logs emitted from other components, use 'PascalCase' for log message tokens.", "Naming", DiagnosticSeverity.Info, false);
    }
}
