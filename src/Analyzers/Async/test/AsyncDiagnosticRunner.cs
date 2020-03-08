// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.DotNet.Analyzers.Async.Tests
{
    public class AsyncDiagnosticRunner : DiagnosticAnalyzerRunner
    {
        public AsyncDiagnosticRunner(DiagnosticAnalyzer analyzer)
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
