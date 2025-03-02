// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Gen.ComplianceReports;
using Microsoft.Gen.Metrics.Model;
using Microsoft.Gen.MetricsReports;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.MetadataExtractor;
internal sealed class MetadataEmitter : JsonEmitterBase
{
    private const int IndentationLevel = 2;
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

        var receiver = context.SyntaxReceiver as TypeDeclarationSyntaxReceiver;
        if (receiver is not null)
        {
            metadataReport.metricReport = HandleMetricReportGeneration(context, receiver);
            metadataReport.complianceReport = HandleComplianceReportGeneration(context, receiver);
        }

        OutOpenBrace(isRoot: true);
        OutNameValue("Name", context.Compilation.AssemblyName!, isSingle: true);
        OutIndent();
        Out("\"ComplianceReport\": ");
        Out($"{(string.IsNullOrEmpty(metadataReport.complianceReport) ? "{}" : metadataReport.complianceReport)},");
        OutLn();
        OutIndent();
        Out("\"MetricReport\": ");
        Out((string.IsNullOrEmpty(metadataReport.metricReport) ? "[]" : metadataReport.metricReport));
        OutLn();
        OutCloseBrace(isRoot: true);

        return Capture();
    }

    /// <summary>
    /// used to generate the report for metrics annotations.
    /// </summary>
    /// <param name="context">The generator execution context.</param>
    /// <param name="receiver">The typeDeclaration syntax receiver.</param>
    /// <returns>string report as json or String.Empty.</returns>
    private string HandleMetricReportGeneration(GeneratorExecutionContext context, TypeDeclarationSyntaxReceiver receiver)
    {
        Metrics.Parser meteringParser = new Metrics.Parser(context.Compilation, context.ReportDiagnostic, context.CancellationToken);
        IReadOnlyList<MetricType> meteringClasses = meteringParser.GetMetricClasses(receiver.TypeDeclarations);

        if (meteringClasses.Count == 0)
        {
            return string.Empty;
        }

        _ = context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(_rootNamespace, out var rootNamespace);
        ReportedMetricClass[] reportedMetrics = MetricsReportsHelpers.MapToCommonModel(meteringClasses, rootNamespace);
        return _metricDefinitionEmitter.GenerateReport(reportedMetrics, context.CancellationToken, IndentationLevel);
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

        Parser parser = new Parser(context.Compilation, symbolHolder!, context.CancellationToken);
        IReadOnlyList<ClassifiedType> classifiedTypes = parser.GetClassifiedTypes(receiver.TypeDeclarations);
        if (classifiedTypes.Count == 0)
        {
            // nothing to do
            return string.Empty;
        }

        return _complianceReportEmitter.Emit(classifiedTypes, context.Compilation.AssemblyName!, false, IndentationLevel);
    }
}
