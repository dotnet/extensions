// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.Gen.ComplianceReports;
using Microsoft.Gen.MetricsReports;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.MetadataExtractor;
internal sealed class MetadataEmitter : EmitterBase
{
    private readonly MetricDefinitionEmitter _metricDefinitionEmitter;
    private readonly ComplianceReportEmitter _complianceReportEmitter;
    private readonly string _rootNamespace;

    public MetadataEmitter(string rootNamespace)
        : base(emitPreamble: false)
    {
        _metricDefinitionEmitter = new MetricDefinitionEmitter();
        _complianceReportEmitter = new ComplianceReportEmitter();
        _rootNamespace = rootNamespace;
    }

    [SuppressMessage("Performance", "LA0002:Use 'Microsoft.Extensions.Text.NumericExtensions.ToInvariantString' for improved performance", Justification = "Can't use that in a generator")]
    public string Emit(GeneratorExecutionContext context)
    {
        (string metricReport, string complianceReport) metadataReport = (string.Empty, string.Empty);
        metadataReport.metricReport = HandleMetricReportGeneration(context, (TypeDeclarationSyntaxReceiver)context.SyntaxReceiver);
        metadataReport.complianceReport = HandleComplianceReportGeneration(context, (TypeDeclarationSyntaxReceiver)context.SyntaxReceiver);

        OutLn("{");
        Out("\"Name\":" + $"\"{context.Compilation.AssemblyName!}\"");
        OutLn(",");
        OutIndent();
        Out("\"ComplianceReport\": ");
        Out((string.IsNullOrEmpty(metadataReport.complianceReport) ? "{}" : metadataReport.complianceReport));
        OutLn(",");
        Out("\"MetricReport\": ");
        OutLn((string.IsNullOrEmpty(metadataReport.metricReport) ? "[]" : metadataReport.metricReport));
        OutLn("}");

        var xx = Capture();
        return xx;
    }

    /// <summary>
    /// used to generate the report for metrics annotations.
    /// </summary>
    /// <param name="context">The generator execution context.</param>
    /// <param name="receiver">The typeDeclaration syntax receiver.</param>
    /// <returns>string report as json or String.Empty.</returns>
    private string HandleMetricReportGeneration(GeneratorExecutionContext context, TypeDeclarationSyntaxReceiver receiver)
    {
        var meteringParser = new Metrics.Parser(context.Compilation, context.ReportDiagnostic, context.CancellationToken);
        var meteringClasses = meteringParser.GetMetricClasses(receiver.TypeDeclarations);

        if (meteringClasses.Count == 0)
        {
            return string.Empty;
        }

        _ = context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(_rootNamespace, out var rootNamespace);
        var reportedMetrics = MetricsReportsHelpers.MapToCommonModel(meteringClasses, rootNamespace);
        var report = _metricDefinitionEmitter.GenerateReport(reportedMetrics, context.CancellationToken, indentationLevel: 4);
        return report;
    }

    /// <summary>
    /// used to generate the report for compliance annotations.
    /// </summary>
    /// <param name="context">The generator execution context.</param>
    /// <param name="receiver">The type declaration syntax receiver.</param>
    /// <returns>string report as json or String.Empty.</returns>
    private string HandleComplianceReportGeneration(GeneratorExecutionContext context, TypeDeclarationSyntaxReceiver receiver)
    {
        if (!SymbolLoader.TryLoad(context.Compilation, out var symbolHolder))
        {
            return string.Empty;
        }

        var parser = new Parser(context.Compilation, symbolHolder!, context.CancellationToken);
        var classifiedTypes = parser.GetClassifiedTypes(receiver.TypeDeclarations);
        if (classifiedTypes.Count == 0)
        {
            // nothing to do
            return string.Empty;
        }

        string report = _complianceReportEmitter.Emit(classifiedTypes, context.Compilation.AssemblyName!, false, indentationLevel: 4);
        return report;
    }
}
