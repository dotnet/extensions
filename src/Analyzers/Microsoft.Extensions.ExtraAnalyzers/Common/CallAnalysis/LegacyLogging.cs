// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.Extensions.ExtraAnalyzers.CallAnalysis;

/// <summary>
/// Recommends replacing legacy logging calls with R9 logging calls.
/// </summary>
internal sealed class LegacyLogging
{
    public LegacyLogging(CallAnalyzer.Registrar reg)
    {
        var loggerExtensions = reg.Compilation.GetTypeByMetadataName("Microsoft.Extensions.Logging.LoggerExtensions");
        if (loggerExtensions != null)
        {
            var legacyMethods = new List<IMethodSymbol>();
            legacyMethods.AddRange(loggerExtensions.GetMembers("LogTrace").OfType<IMethodSymbol>());
            legacyMethods.AddRange(loggerExtensions.GetMembers("LogDebug").OfType<IMethodSymbol>());
            legacyMethods.AddRange(loggerExtensions.GetMembers("LogInformation").OfType<IMethodSymbol>());
            legacyMethods.AddRange(loggerExtensions.GetMembers("LogWarning").OfType<IMethodSymbol>());
            legacyMethods.AddRange(loggerExtensions.GetMembers("LogError").OfType<IMethodSymbol>());
            legacyMethods.AddRange(loggerExtensions.GetMembers("LogCritical").OfType<IMethodSymbol>());
            legacyMethods.AddRange(loggerExtensions.GetMembers("Log").OfType<IMethodSymbol>());

            foreach (var method in legacyMethods)
            {
                reg.RegisterMethod(method, Handle);
            }
        }

        static void Handle(OperationAnalysisContext context, IInvocationOperation op)
        {
            var diagnostic = Diagnostic.Create(DiagDescriptors.LegacyLogging, op.Syntax.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
