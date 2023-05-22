// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.Extensions.ExtraAnalyzers.CallAnalysis;

/// <summary>
/// Recommends replacing legacy collections with generic ones.
/// </summary>
internal sealed class LegacyCollection
{
    private static readonly string[] _collectionTypes = new[]
    {
        "System.Collections.ArrayList",
        "System.Collections.Hashtable",
        "System.Collections.Queue",
        "System.Collections.Stack",
        "System.Collections.SortedList",
        "System.Collections.Specialized.HybridDictionary",
        "System.Collections.Specialized.ListDictionary",
        "System.Collections.Specialized.OrderedDictionary",
    };

    public LegacyCollection(CallAnalyzer.Registrar reg)
    {
        reg.RegisterConstructors(_collectionTypes, HandleConstructor);

        static void HandleConstructor(OperationAnalysisContext context, IObjectCreationOperation op)
        {
            var diagnostic = Diagnostic.Create(DiagDescriptors.LegacyCollection, op.Syntax.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
