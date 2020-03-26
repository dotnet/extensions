// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Extensions.Logging.Analyzer
{
    public class LoggingDiagnosticRunner : DiagnosticAnalyzerRunner
    {
        public LoggingDiagnosticRunner(DiagnosticAnalyzer analyzer)
        {
            Analyzer = analyzer;
        }

        public DiagnosticAnalyzer Analyzer { get; }

        public Task<Diagnostic[]> GetDiagnosticsAsync(string source, string[] additionalEnabledDiagnostics)
        {
            return GetDiagnosticsAsync(sources: new[] { source }, Analyzer, additionalEnabledDiagnostics);
        }
    }
}
