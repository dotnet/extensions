// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if ROSLYN_4_0_OR_GREATER

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.Logging;

[Generator]
[ExcludeFromCodeCoverage]
public class Generator : IIncrementalGenerator
{
    private static readonly HashSet<string> _attributeNames = new()
    {
        Parsing.SymbolLoader.LogMethodAttribute,
    };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        GeneratorUtilities.Initialize(context, _attributeNames, m => m.Parent as TypeDeclarationSyntax, HandleAnnotatedTypes);
    }

    private static void HandleAnnotatedTypes(Compilation compilation, IEnumerable<SyntaxNode> nodes, SourceProductionContext context)
    {
        var p = new Parsing.Parser(compilation, context.ReportDiagnostic, context.CancellationToken);

        var logTypes = p.GetLogTypes(nodes.OfType<TypeDeclarationSyntax>());
        if (logTypes.Count > 0)
        {
            var e = new Emission.Emitter();
            var result = e.Emit(logTypes, context.CancellationToken);

            context.AddSource("Logging.g.cs", SourceText.From(result, Encoding.UTF8));
        }
    }
}

#else

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.Logging;

[Generator]
[ExcludeFromCodeCoverage]
public class Generator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(TypeDeclarationSyntaxReceiver.Create);
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var receiver = context.SyntaxReceiver as TypeDeclarationSyntaxReceiver;
        if (receiver == null || receiver.TypeDeclarations.Count == 0)
        {
            // nothing to do yet
            return;
        }

        var p = new Parsing.Parser(context.Compilation, context.ReportDiagnostic, context.CancellationToken);
        var logTypes = p.GetLogTypes(receiver.TypeDeclarations);
        if (logTypes.Count > 0)
        {
            var e = new Emission.Emitter();
            var result = e.Emit(logTypes, context.CancellationToken);

            context.AddSource("Logging.g.cs", SourceText.From(result, Encoding.UTF8));
        }
    }
}

#endif
