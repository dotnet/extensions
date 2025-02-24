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
    private string? _directory;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataReportsGenerator"/> class.
    /// </summary>
    public MetadataReportsGenerator()
    : this(filePath: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataReportsGenerator"/> class.
    /// </summary>
    /// <param name="filePath">The report path and name.</param>
    public MetadataReportsGenerator(string? filePath)
    {
        if (filePath is not null)
        {
            _directory = Path.GetDirectoryName(filePath);
            _fileName = Path.GetFileName(filePath);
        }
        else
        {
            _fileName = FallbackFileName;
        }
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
            // nothing to do yet
            return;
        }

        var options = context.AnalyzerConfigOptions.GlobalOptions;
        _directory ??= GeneratorUtilities.TryRetrieveOptionsValue(options, ReportOutputPathMSBuildProperty, out var reportOutputPath)
            ? reportOutputPath!
            : GeneratorUtilities.GetDefaultReportOutputPath(options);

        if (string.IsNullOrWhiteSpace(_directory))
        {
            // Report diagnostic:
            var diagnostic = new DiagnosticDescriptor(
                DiagnosticIds.AuditReports.AUDREPGEN000,
                "MetadataReport generator couldn't resolve output path for the report. It won't be generated.",
                "Both <MetadataReportOutputPath> and <OutputPath> MSBuild properties are not set. The report won't be generated.",
                nameof(DiagnosticIds.AuditReports),
                DiagnosticSeverity.Info,
                isEnabledByDefault: true,
                helpLinkUri: string.Format(CultureInfo.InvariantCulture, DiagnosticIds.UrlFormat, DiagnosticIds.AuditReports.AUDREPGEN000));

            context.ReportDiagnostic(Diagnostic.Create(diagnostic, location: null));
            return;
        }

        MetadataEmitter Emitter = new MetadataEmitter(RootNamespace);

#pragma warning disable RS1035 // Do not use APIs banned for analyzers
        _ = Directory.CreateDirectory(_directory);

        File.WriteAllText(Path.Combine(_directory, _fileName), Emitter.Emit(context), Encoding.UTF8);
#pragma warning restore RS1035 // Do not use APIs banned for analyzers

    }
}
