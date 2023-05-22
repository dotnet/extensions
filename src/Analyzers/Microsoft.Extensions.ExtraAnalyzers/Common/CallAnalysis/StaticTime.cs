// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.Extensions.ExtraAnalyzers.CallAnalysis;

/// <summary>
/// Recommends using the System.TimeProvider abstraction.
/// </summary>
internal sealed class StaticTime
{
    private static readonly Dictionary<string, string[]> _timeMethods = new()
    {
        ["System.Threading.Tasks.Task"] = new[]
        {
            "Delay",
        },

        ["System.Threading.Thread"] = new[]
        {
            "Sleep",
        },
    };

    private static readonly Dictionary<string, string[]> _timeProperties = new()
    {
        ["System.DateTime"] = new[]
        {
            "Now",
            "Today",
            "UtcNow",
        },

        ["System.DateTimeOffset"] = new[]
        {
            "Now",
            "UtcNow",
        },
    };

    public StaticTime(CallAnalyzer.Registrar reg)
    {
        reg.RegisterMethods(_timeMethods, HandleMethod);
        reg.RegisterProperties(_timeProperties, HandleProperty);

        static void HandleMethod(OperationAnalysisContext context, IInvocationOperation op)
        {
            var diagnostic = Diagnostic.Create(DiagDescriptors.StaticTime, op.Syntax.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }

        static void HandleProperty(OperationAnalysisContext context, IPropertyReferenceOperation op)
        {
            var diagnostic = Diagnostic.Create(DiagDescriptors.StaticTime, op.Syntax.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
