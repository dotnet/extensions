// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.Gen.ComplianceReports;
using Microsoft.Gen.MetricsReports;
using Microsoft.Gen.Shared;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Gen.MetadataExtractor;

/// <summary>
/// Generates reports for compliance & metrics annotations.
/// </summary>
[Generator]
public sealed class MetadataReportsGenerator : ISourceGenerator
{
    private const string GenerateMetadataMSBuildProperty = "build_property.GenerateMetadataReport";
    private const string ReportOutputPathMSBuildProperty = "build_property.MetadataReportOutputPath";
    private const string RootNamespace = "build_property.rootnamespace";
    private const string FallbackFileName = "MetadataReport.json";
    private readonly string _fileName;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataReportsGenerator"/> class.
    /// </summary>
    public MetadataReportsGenerator()
    : this(FallbackFileName)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataReportsGenerator"/> class.
    /// </summary>
    /// <param name="reportFileName">The report file name.</param>
    public MetadataReportsGenerator(string reportFileName)
    {
        _fileName = reportFileName;
    }

    /// <summary>
    /// Initializes the generator.
    /// </summary>
    /// <param name="context">The generator initialization context.</param>
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(TypeDeclarationSyntaxReceiver.Create);
    }

    /// <summary>
    /// Generates reports for compliance & metrics annotations.
    /// </summary>
    /// <param name="context">The generator execution context.</param>
    public void Execute(GeneratorExecutionContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        if (context.SyntaxReceiver is not TypeDeclarationSyntaxReceiver ||
            ((TypeDeclarationSyntaxReceiver)context.SyntaxReceiver).TypeDeclarations.Count == 0 ||
            !GeneratorUtilities.ShouldGenerateReport(context, GenerateMetadataMSBuildProperty))
        {
            return;
        }

        if ((context.SyntaxReceiver is not TypeDeclarationSyntaxReceiver || ((TypeDeclarationSyntaxReceiver)context.SyntaxReceiver).TypeDeclarations.Count == 0))
        {
            // nothing to do yet
            return;
        }

        var options = context.AnalyzerConfigOptions.GlobalOptions;
        var path = GeneratorUtilities.TryRetrieveOptionsValue(options, ReportOutputPathMSBuildProperty, out var reportOutputPath)
            ? reportOutputPath!
            : GeneratorUtilities.GetDefaultReportOutputPath(options);
        if (string.IsNullOrWhiteSpace(path))
        {
            // Report diagnostic:
            var diagnostic = new DiagnosticDescriptor(
                DiagnosticIds.AuditReports.AUDREPGEN000,
                "MetricsReports generator couldn't resolve output path for the report. It won't be generated.",
                "Both <MetadataReportOutputPath> and <OutputPath> MSBuild properties are not set. The report won't be generated.",
                nameof(DiagnosticIds.AuditReports),
                DiagnosticSeverity.Info,
                isEnabledByDefault: true,
                helpLinkUri: string.Format(CultureInfo.InvariantCulture, DiagnosticIds.UrlFormat, DiagnosticIds.AuditReports.AUDREPGEN000));

            context.ReportDiagnostic(Diagnostic.Create(diagnostic, location: null));
            return;
        }

        (string metricReport, string complianceReport) metadataReport = (string.Empty, string.Empty);
        metadataReport.metricReport = HandleMetricReportGeneration(context, (TypeDeclarationSyntaxReceiver)context.SyntaxReceiver);
        metadataReport.complianceReport = HandleComplianceReportGeneration(context, (TypeDeclarationSyntaxReceiver)context.SyntaxReceiver);

        StringBuilder reportStringBuilder = new StringBuilder()
            .Append("{ \"Name\": \"")
            .Append(context.Compilation.AssemblyName!)
            .Append("\", \"ComplianceReport\": ")
            .Append((string.IsNullOrEmpty(metadataReport.complianceReport) ? "{}" : metadataReport.complianceReport))
            .Append(" ,")
            .Append(" \"MetricReport\": ")
            .Append((string.IsNullOrEmpty(metadataReport.metricReport) ? "[]" : metadataReport.metricReport) + " }");

#pragma warning disable RS1035 // Do not use APIs banned for analyzers
        File.WriteAllText(Path.Combine(path, _fileName), reportStringBuilder.ToString(), Encoding.UTF8);
#pragma warning restore RS1035 // Do not use APIs banned for analyzers

    }

    /// <summary>
    /// used to generate the report for metrics annotations.
    /// </summary>
    /// <param name="context">The generator execution context.</param>
    /// <param name="receiver">The typeDeclaration syntax receiver.</param>
    /// <returns>string report as json or String.Empty.</returns>
    private static string HandleMetricReportGeneration(GeneratorExecutionContext context, TypeDeclarationSyntaxReceiver receiver)
    {
        var meteringParser = new Metrics.Parser(context.Compilation, context.ReportDiagnostic, context.CancellationToken);
        var meteringClasses = meteringParser.GetMetricClasses(receiver.TypeDeclarations);

        if (meteringClasses.Count == 0)
        {
            return string.Empty;
        }

        _ = context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(RootNamespace, out var rootNamespace);
        var reportedMetrics = MetricsReportsHelpers.MapToCommonModel(meteringClasses, rootNamespace);
        var emitter = new MetricDefinitionEmitter();
        var report = emitter.GenerateReport(reportedMetrics, context.CancellationToken);
        return report;
    }

    /// <summary>
    /// used to generate the report for compliance annotations.
    /// </summary>
    /// <param name="context">The generator execution context.</param>
    /// <param name="receiver">The type declaration syntax receiver.</param>
    /// <returns>string report as json or String.Empty.</returns>
    private static string HandleComplianceReportGeneration(GeneratorExecutionContext context, TypeDeclarationSyntaxReceiver receiver)
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

        var emitter = new Emitter();
        string report = emitter.Emit(classifiedTypes, context.Compilation.AssemblyName!, false);

        return report;
    }
}
