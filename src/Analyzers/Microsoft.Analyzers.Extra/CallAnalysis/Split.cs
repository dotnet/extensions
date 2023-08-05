// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.Extensions.ExtraAnalyzers.CallAnalysis;

/// <summary>
/// Recommends allocation-free string split functionality instead of String.Split.
/// </summary>
internal sealed class Split
{
    public Split(CallAnalyzer.Registrar reg)
    {
        var memExt = reg.Compilation.GetTypeByMetadataName("System.MemoryExtensions");
        if (memExt == null || memExt.GetMembers("Split").IsEmpty)
        {
            // Split function not available, so punt
            return;
        }

        reg.RegisterMethods("System.String", "Split", Handle);

        static void Handle(OperationAnalysisContext context, IInvocationOperation op)
        {
            var diagnostic = Diagnostic.Create(DiagDescriptors.Split, op.Syntax.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
