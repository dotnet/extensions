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

namespace Microsoft.Gen.Metering;

[Generator]
[ExcludeFromCodeCoverage]
public class Generator : IIncrementalGenerator
{
    private static readonly HashSet<string> _attributeNames = new()
    {
        SymbolLoader.CounterAttribute,
        SymbolLoader.CounterTAttribute.Replace("`1", "<T>"),
        SymbolLoader.HistogramAttribute,
        SymbolLoader.HistogramTAttribute.Replace("`1", "<T>")
    };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        GeneratorUtilities.Initialize(context, _attributeNames, m => m.Parent as TypeDeclarationSyntax, HandleAnnotatedTypes);
    }

    private static void HandleAnnotatedTypes(Compilation compilation, IEnumerable<SyntaxNode> nodes, SourceProductionContext context)
    {
        var p = new Parser(compilation, context.ReportDiagnostic, context.CancellationToken);

        var metricClasses = p.GetMetricClasses(nodes.OfType<TypeDeclarationSyntax>());
        if (metricClasses.Count > 0)
        {
            var factoryEmitter = new MetricFactoryEmitter();
            var factory = factoryEmitter.Emit(metricClasses, context.CancellationToken);
            context.AddSource("Factory.g.cs", SourceText.From(factory, Encoding.UTF8));

            var emitter = new Emitter();
            var metrics = emitter.EmitMetrics(metricClasses, context.CancellationToken);
            context.AddSource("Metering.g.cs", SourceText.From(metrics, Encoding.UTF8));
        }
    }
}

#else

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.Metering;

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

        var parser = new Parser(context.Compilation, context.ReportDiagnostic, context.CancellationToken);

        var metricClasses = parser.GetMetricClasses(receiver.TypeDeclarations);
        if (metricClasses.Count > 0)
        {
            var factoryEmitter = new MetricFactoryEmitter();
            var factory = factoryEmitter.Emit(metricClasses, context.CancellationToken);
            context.AddSource("Factory.g.cs", SourceText.From(factory, Encoding.UTF8));

            var emitter = new Emitter();
            var metrics = emitter.EmitMetrics(metricClasses, context.CancellationToken);
            context.AddSource("Metering.g.cs", SourceText.From(metrics, Encoding.UTF8));
        }
    }
}

#endif
