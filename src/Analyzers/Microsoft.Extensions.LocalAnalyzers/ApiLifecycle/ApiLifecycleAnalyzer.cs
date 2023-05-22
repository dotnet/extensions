// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.LocalAnalyzers.ApiLifecycle.Model;
using Microsoft.Extensions.LocalAnalyzers.Utilities;

namespace Microsoft.Extensions.LocalAnalyzers.ApiLifecycle;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ApiLifecycleAnalyzer : DiagnosticAnalyzer
{
    private const string ExperimentalAttributeFullName = "System.Diagnostics.CodeAnalysis.ExperimentalAttribute";
    private const string ObsoleteAttributeFullName = "System.ObsoleteAttribute";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(
                DiagDescriptors.NewSymbolsMustBeMarkedExperimental,
                DiagDescriptors.ExperimentalSymbolsCantBeMarkedObsolete,
                DiagDescriptors.PublishedSymbolsCantBeMarkedExperimental,
                DiagDescriptors.PublishedSymbolsCantBeDeleted,
                DiagDescriptors.PublishedSymbolsCantChange);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(start =>
        {
            var compilation = start.Compilation;

            if (ModelLoader.TryLoadAssemblyModel(start, out var assemblyModel))
            {
                start.RegisterCompilationEndAction(endContext => ReportDiagnosticForModel(endContext, Analyze(endContext.Compilation, assemblyModel)));
            }
            else if (assemblyModel == null)
            {
                start.RegisterCompilationEndAction(endContext => CheckAllPublicTypesAreExperimentalAndNotObsolete(endContext));
            }
        });
    }

    private static AssemblyAnalysis Analyze(Compilation compilation, Assembly? assemblyModel)
    {
        var types = compilation
            .GetSymbolsWithName(_ => true)
            .Where(symbol => symbol.IsExternallyVisible() && symbol.Kind == SymbolKind.NamedType)
            .Cast<INamedTypeSymbol>();

        var assemblyAnalysis = new AssemblyAnalysis(assemblyModel ?? Assembly.Empty);
        foreach (var type in types)
        {
            assemblyAnalysis.AnalyzeType(type);
        }

        return assemblyAnalysis;
    }

    private static void ReportDiagnosticForModel(CompilationAnalysisContext context, AssemblyAnalysis assemblyAnalysis)
    {
        var compilation = context.Compilation;
        var obsoleteAttribute = compilation.GetTypeByMetadataName(ObsoleteAttributeFullName);
        var experimentalAttribute = compilation.GetTypeByMetadataName(ExperimentalAttributeFullName);

        // flag symbols found in the code, but not in the model
        foreach (var symbol in assemblyAnalysis.NotFoundInBaseline)
        {
            if (!symbol.IsContaminated(experimentalAttribute))
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagDescriptors.NewSymbolsMustBeMarkedExperimental, symbol.Locations.FirstOrDefault(), symbol));
            }
        }

        // flag any stable or deprecated API in the model, but not in the assembly

        foreach (var type in assemblyAnalysis.MissingTypes.Where(x => x.Stage != Stage.Experimental))
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagDescriptors.PublishedSymbolsCantBeDeleted, null, type.ModifiersAndName));
        }

        foreach (var method in assemblyAnalysis.MissingMethods.Where(x => x.Stage != Stage.Experimental))
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagDescriptors.PublishedSymbolsCantBeDeleted, null, method.Member));
        }

        foreach (var prop in assemblyAnalysis.MissingProperties.Where(x => x.Stage != Stage.Experimental))
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagDescriptors.PublishedSymbolsCantBeDeleted, null, prop.Member));
        }

        foreach (var field in assemblyAnalysis.MissingFields.Where(x => x.Stage != Stage.Experimental))
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagDescriptors.PublishedSymbolsCantBeDeleted, null, field.Member));
        }

        // now make sure attributes are applied correctly
        foreach (var (symbol, stage) in assemblyAnalysis.FoundInBaseline)
        {
            var isMarkedExperimental = symbol.IsContaminated(experimentalAttribute);
            var isMarkedObsolete = symbol.IsContaminated(obsoleteAttribute);

            if (stage == Stage.Experimental)
            {
                if (!isMarkedExperimental)
                {
                    context.ReportDiagnostic(Diagnostic.Create(DiagDescriptors.NewSymbolsMustBeMarkedExperimental, symbol.Locations.FirstOrDefault(), symbol));
                }

                if (isMarkedObsolete)
                {
                    context.ReportDiagnostic(Diagnostic.Create(DiagDescriptors.ExperimentalSymbolsCantBeMarkedObsolete, symbol.Locations.FirstOrDefault(), symbol));
                }
            }
            else
            {
                if (isMarkedExperimental)
                {
                    context.ReportDiagnostic(Diagnostic.Create(DiagDescriptors.PublishedSymbolsCantBeMarkedExperimental, symbol.Locations.FirstOrDefault(), symbol));
                }

                if (assemblyAnalysis.MissingConstraints.TryGetValue(symbol, out var missingContraintsForSymbol))
                {
                    if (missingContraintsForSymbol.Count > 0)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(DiagDescriptors.PublishedSymbolsCantChange, symbol.Locations.FirstOrDefault(), symbol));
                    }
                }

                if (assemblyAnalysis.MissingBaseTypes.TryGetValue(symbol, out var missingBaseForSymbol))
                {
                    if (missingBaseForSymbol.Count > 0)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(DiagDescriptors.PublishedSymbolsCantChange, symbol.Locations.FirstOrDefault(), symbol));
                    }
                }
            }
        }
    }

    private static void CheckAllPublicTypesAreExperimentalAndNotObsolete(CompilationAnalysisContext context)
    {
        var types = context
            .Compilation
            .GetSymbolsWithName(_ => true)
            .Where(symbol => symbol.IsExternallyVisible() && symbol.Kind == SymbolKind.NamedType)
            .Cast<INamedTypeSymbol>();

        var experimentalAttribute = context.Compilation.GetTypeByMetadataName(ExperimentalAttributeFullName);
        var obsoleteAttribute = context.Compilation.GetTypeByMetadataName(ObsoleteAttributeFullName);

        foreach (var type in types)
        {
            if (!type.IsContaminated(experimentalAttribute))
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagDescriptors.NewSymbolsMustBeMarkedExperimental, type.Locations.FirstOrDefault(), type));
            }
            else if (type.IsContaminated(obsoleteAttribute))
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagDescriptors.ExperimentalSymbolsCantBeMarkedObsolete, type.Locations.FirstOrDefault(), type));
            }
        }
    }
}
