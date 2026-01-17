// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.Gen.Shared;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Gen.ComplianceReports;

/// <summary>
/// Generates reports for compliance annotations.
/// </summary>
[Generator]
public sealed class ComplianceReportsGenerator : ISourceGenerator
{
    private const string GenerateComplianceReportsMSBuildProperty = "build_property.GenerateComplianceReport";
    private const string ComplianceReportOutputPathMSBuildProperty = "build_property.ComplianceReportOutputPath";

    private const string FallbackFileName = "ComplianceReport.json";

    private readonly string _fileName;
    private string? _directory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComplianceReportsGenerator"/> class.
    /// </summary>
    public ComplianceReportsGenerator()
        : this(filePath: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ComplianceReportsGenerator"/> class.
    /// </summary>
    /// <param name="filePath">The report path and name.</param>
    public ComplianceReportsGenerator(string? filePath)
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

        if (!GeneratorUtilities.ShouldGenerateReport(context, GenerateComplianceReportsMSBuildProperty))
        {
            // By default, compliance reports are generated only during build time and not during design time to prevent the file being written on every keystroke in VS.
            return;
        }

        if (!SymbolLoader.TryLoad(context.Compilation, out var symbolHolder))
        {
            // Not eligible compilation
            return;
        }

        var parser = new Parser(context.Compilation, symbolHolder!, context.CancellationToken);
        var classifiedTypes = parser.GetClassifiedTypes(receiver.TypeDeclarations);
        if (classifiedTypes.Count == 0)
        {
            // nothing to do
            return;
        }

        var emitter = new ComplianceReportEmitter();
        string report = emitter.Emit(classifiedTypes, context.Compilation.AssemblyName!);

        context.CancellationToken.ThrowIfCancellationRequested();

        var options = context.AnalyzerConfigOptions.GlobalOptions;
        _directory ??= GeneratorUtilities.TryRetrieveOptionsValue(options, ComplianceReportOutputPathMSBuildProperty, out var reportOutputPath)
            ? reportOutputPath!
            : GeneratorUtilities.GetDefaultReportOutputPath(options);

        if (string.IsNullOrWhiteSpace(_directory))
        {
            // Report diagnostic:
            var diagnostic = new DiagnosticDescriptor(
                DiagnosticIds.AuditReports.AUDREPGEN001,
                "ComplianceReports generator couldn't resolve output path for the report. It won't be generated.",
                "Both <ComplianceReportOutputPath> and <OutputPath> MSBuild properties are not set. The report won't be generated.",
                nameof(DiagnosticIds.AuditReports),
                DiagnosticSeverity.Info,
                isEnabledByDefault: true,
                helpLinkUri: string.Format(CultureInfo.InvariantCulture, DiagnosticIds.UrlFormat, DiagnosticIds.AuditReports.AUDREPGEN001));

            context.ReportDiagnostic(Diagnostic.Create(diagnostic, location: null));
            return;
        }

        // File IO has been marked as banned for use in analyzers, and an alternate should be used instead
        // Suppressing until this issue is addressed in https://github.com/dotnet/extensions/issues/5390

#pragma warning disable RS1035 // Do not use APIs banned for analyzers
        _ = Directory.CreateDirectory(_directory);

        // Write report as JSON file.
        File.WriteAllText(Path.Combine(_directory, _fileName), report, Encoding.UTF8);
#pragma warning restore RS1035 // Do not use APIs banned for analyzers
    }
}
