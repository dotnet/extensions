// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if ROSLYN_4_0_OR_GREATER

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.EnumStrings;

[Generator]
[ExcludeFromCodeCoverage]
public class EnumStringsGenerator : IIncrementalGenerator
{
    private static readonly HashSet<string> _attributeNames = new()
    {
        SymbolLoader.EnumStringsAttribute,
    };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        GeneratorUtilities.Initialize(context, _attributeNames, HandleAnnotatedNodes);
    }

    private static void HandleAnnotatedNodes(Compilation compilation, IEnumerable<SyntaxNode> nodes, SourceProductionContext context)
    {
        if (!SymbolLoader.TryLoad(compilation, out var symbolHolder))
        {
            // Not eligible compilation
            return;
        }

        var parser = new Parser(compilation, context.ReportDiagnostic, symbolHolder!, context.CancellationToken);

        var toStringMethods = parser.GetToStringMethods(nodes);
        if (toStringMethods.Count > 0)
        {
            var emitter = new Emitter();
            var result = emitter.Emit(toStringMethods, symbolHolder!.FreezerSymbol != null, context.CancellationToken);

            context.AddSource("EnumStrings.g.cs", SourceText.From(result, Encoding.UTF8));
        }
    }
}

#else

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Gen.EnumStrings;

[Generator]
[ExcludeFromCodeCoverage]
public class EnumStringsGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new Receiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var receiver = context.SyntaxReceiver as Receiver;
        if (receiver == null || receiver.Nodes.Count == 0)
        {
            // nothing to do yet
            return;
        }

        if (!SymbolLoader.TryLoad(context.Compilation, out var symbolHolder))
        {
            // Not eligible compilation
            return;
        }

        var parser = new Parser(context.Compilation, context.ReportDiagnostic, symbolHolder!, context.CancellationToken);
        var toStringMethods = parser.GetToStringMethods(receiver.Nodes);
        if (toStringMethods.Count > 0)
        {
            var emitter = new Emitter();
            var result = emitter.Emit(toStringMethods, symbolHolder!.FreezerSymbol != null, context.CancellationToken);
            context.AddSource("EnumStrings.g.cs", SourceText.From(result, Encoding.UTF8));
        }
    }

    private sealed class Receiver : ISyntaxReceiver
    {
        public ICollection<SyntaxNode> Nodes { get; } = new List<SyntaxNode>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is EnumDeclarationSyntax enumSyntax)
            {
                Nodes.Add(enumSyntax);
            }
            else if (syntaxNode is CompilationUnitSyntax compUnitSyntax)
            {
                Nodes.Add(compUnitSyntax);
            }
        }
    }
}

#endif
