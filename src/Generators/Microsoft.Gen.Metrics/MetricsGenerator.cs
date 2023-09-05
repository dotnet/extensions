// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.Metrics;

[Generator]
public class MetricsGenerator : IIncrementalGenerator
{
    private static readonly HashSet<string> _attributeNames = new()
    {
        SymbolLoader.CounterAttribute,
        SymbolLoader.CounterTAttribute.Replace("`1", "<T>"),
        SymbolLoader.HistogramAttribute,
        SymbolLoader.HistogramTAttribute.Replace("`1", "<T>"),
        SymbolLoader.GaugeAttribute
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
            context.AddSource("Metrics.g.cs", SourceText.From(metrics, Encoding.UTF8));
        }
    }
}
