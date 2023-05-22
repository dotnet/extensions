// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.ComplianceReports;

/// <summary>
/// Generates reports for compliance annotations.
/// </summary>
[Generator]
[ExcludeFromCodeCoverage]
public sealed class Generator : ISourceGenerator
{
    private const string GenerateComplianceReportsMSBuildProperty = "build_property.GenerateComplianceReport";
    private const string ComplianceReportOutputPathMSBuildProperty = "build_property.ComplianceReportOutputPath";

    private string? _reportOutputPath;

    public Generator()
        : this(null)
    {
    }

    public Generator(string? reportOutputPath)
    {
        _reportOutputPath = reportOutputPath;
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
            // By default, compliance reports are only generated only during build time and not during design time to prevent the file being written on every keystroke in VS.
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

        var emitter = new Emitter();
        string report = emitter.Emit(classifiedTypes, context.Compilation.AssemblyName!);

        context.CancellationToken.ThrowIfCancellationRequested();

        if (_reportOutputPath == null)
        {
            _ = context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(ComplianceReportOutputPathMSBuildProperty, out _reportOutputPath);
            if (string.IsNullOrWhiteSpace(_reportOutputPath))
            {
                // no valid output path
                return;
            }
        }

#pragma warning disable R9A017 // Switch to an asynchronous method for increased performance.
        _ = Directory.CreateDirectory(Path.GetDirectoryName(_reportOutputPath));

        // Write properties to CSV file.
        File.WriteAllText(_reportOutputPath, report);
#pragma warning restore R9A017 // Switch to an asynchronous method for increased performance.
    }
}
