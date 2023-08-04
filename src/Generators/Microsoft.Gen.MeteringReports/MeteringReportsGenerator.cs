// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Gen.Metering.Model;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.MeteringReports;

[Generator]
public class MeteringReportsGenerator : ISourceGenerator
{
    private const string GenerateMetricDefinitionReport = "build_property.GenerateMeteringReport";
    private const string RootNamespace = "build_property.rootnamespace";
    private const string ReportOutputPath = "build_property.MeteringReportOutputPath";
    private const string CompilationOutputPath = "build_property.outputpath";
    private const string CurrentProjectPath = "build_property.projectdir";
    private const string FileName = "MeteringReport.json";

    private string? _compilationOutputPath;
    private string? _currentProjectPath;
    private string? _reportOutputPath;

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(ClassDeclarationSyntaxReceiver.Create);
    }

    public void Execute(GeneratorExecutionContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        var receiver = context.SyntaxReceiver as ClassDeclarationSyntaxReceiver;
        if (receiver == null || receiver.ClassDeclarations.Count == 0 || !GeneratorUtilities.ShouldGenerateReport(context, GenerateMetricDefinitionReport))
        {
            return;
        }

        var meteringParser = new Metering.Parser(context.Compilation, context.ReportDiagnostic, context.CancellationToken);

        var meteringClasses = meteringParser.GetMetricClasses(receiver.ClassDeclarations);

        if (meteringClasses.Count == 0)
        {
            return;
        }

        var options = context.AnalyzerConfigOptions.GlobalOptions;

        var path = (_reportOutputPath != null || options.TryGetValue(ReportOutputPath, out _reportOutputPath))
            ? _reportOutputPath
            : GetDefaultReportOutputPath(options);

        if (string.IsNullOrWhiteSpace(path))
        {
            // Report diagnostic. Tell that it is either <MetricDefinitionReportOutputPath> missing or <CompilerVisibleProperty Include="OutputPath"/> visibility to compiler.
            return;
        }

        _ = options.TryGetValue(RootNamespace, out var rootNamespace);

        var emitter = new MetricDefinitionEmitter();
        var reportedMetrics = MapToCommonModel(meteringClasses, rootNamespace);
        var report = emitter.GenerateReport(reportedMetrics, context.CancellationToken);

#pragma warning disable R9A017 // Switch to an asynchronous metricMethod for increased performance; Cannot because it is void metricMethod, and generators dont support tasks.
        File.WriteAllText(Path.Combine(path, FileName), report, Encoding.UTF8);
#pragma warning restore R9A017 // Switch to an asynchronous metricMethod for increased performance; Cannot because it is void metricMethod, and generators dont support tasks.
    }

    private static ReportedMetricClass[] MapToCommonModel(IReadOnlyList<MetricType> meteringClasses, string? rootNamespace)
    {
        var reportedMetrics = meteringClasses
            .Select(meteringClass => new ReportedMetricClass(
                Name: meteringClass.Name,
                RootNamespace: rootNamespace ?? meteringClass.Namespace,
                Constraints: meteringClass.Constraints,
                Modifiers: meteringClass.Modifiers,
                Methods: meteringClass.Methods.Select(meteringMethod => new ReportedMetricMethod(
                    MetricName: meteringMethod.MetricName ?? "(Missing Name)",
                    Summary: meteringMethod.XmlDefinition ?? "(Missing Summary)",
                    Kind: meteringMethod.InstrumentKind,
                    Dimensions: meteringMethod.TagKeys,
                    DimensionsDescriptions: meteringMethod.TagDescriptionDictionary))
                .ToArray()));

        return reportedMetrics.ToArray();
    }

    private string GetDefaultReportOutputPath(AnalyzerConfigOptions options)
    {
        if (_currentProjectPath != null && _compilationOutputPath != null)
        {
            return _currentProjectPath + _compilationOutputPath;
        }

        _ = options.TryGetValue(CompilationOutputPath, out _compilationOutputPath);
        _ = options.TryGetValue(CurrentProjectPath, out _currentProjectPath);

        return string.IsNullOrWhiteSpace(_currentProjectPath) || string.IsNullOrWhiteSpace(_compilationOutputPath)
            ? string.Empty
            : _currentProjectPath + _compilationOutputPath;
    }
}
