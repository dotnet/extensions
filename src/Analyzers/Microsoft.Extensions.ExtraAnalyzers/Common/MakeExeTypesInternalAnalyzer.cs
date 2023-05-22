// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.ExtraAnalyzers.Utilities;

namespace Microsoft.Extensions.ExtraAnalyzers;

/// <summary>
/// C# analyzer that recommends making an executable's types internal.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MakeExeTypesInternalAnalyzer : DiagnosticAnalyzer
{
    // if any member of the discovered public types are annotated with these attributes, then the type is known
    // to need to be public, so we don't report the type
    private static readonly string[] _disqualifyingMemberAttributes = new[]
    {
        "Xunit.FactAttribute",
        "Xunit.TheoryAttribute",
        "BenchmarkDotNet.Attributes.BenchmarkAttribute",
        "Microsoft.AspNetCore.Mvc.HttpGetAttribute",
        "System.Text.Json.Serialization.JsonConstructorAttribute",
        "System.Text.Json.Serialization.JsonExtensionDataAttribute",
        "System.Text.Json.Serialization.JsonIgnoreAttribute",
        "System.Text.Json.Serialization.JsonIncludeAttribute",
        "System.Text.Json.Serialization.JsonNumberHandlingAttribute",
        "System.Text.Json.Serialization.JsonPropertyNameAttribute",
        "System.Text.Json.Serialization.JsonPropertyOrderAttribute",
    };

    private static readonly string[] _disqualifyingTypeAttributes = new[]
    {
        "MessagePack.MessagePackObjectAttribute",
        "Microsoft.AspNetCore.Mvc.ApiControllerAttribute",
    };

    // if any of the discovered public types derive from the given base classes, we know the use requires the types to
    // be public, so we don't report the type
    private static readonly string[] _disqualifyingBaseClasses = new[]
    {
        "Microsoft.AspNetCore.Mvc.ControllerBase",
        "System.Web.Http.ApiController",
        "System.Web.Mvc.Controller",
    };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagDescriptors.MakeExeTypesInternal);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationStartContext =>
        {
            var disqualifyingMemberAttributes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
            foreach (var name in _disqualifyingMemberAttributes)
            {
                var type = compilationStartContext.Compilation.GetTypeByMetadataName(name);
                if (type != null)
                {
                    _ = disqualifyingMemberAttributes.Add(type);
                }
            }

            var disqualifyingTypeAttributes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
            foreach (var name in _disqualifyingTypeAttributes)
            {
                var type = compilationStartContext.Compilation.GetTypeByMetadataName(name);
                if (type != null)
                {
                    _ = disqualifyingTypeAttributes.Add(type);
                }
            }

            var disqualifyingBaseClasses = new List<ITypeSymbol>();
            foreach (var name in _disqualifyingBaseClasses)
            {
                var type = compilationStartContext.Compilation.GetTypeByMetadataName(name);
                if (type != null)
                {
                    disqualifyingBaseClasses.Add(type);
                }
            }

            if (compilationStartContext.Compilation.Options.OutputKind == OutputKind.ConsoleApplication)
            {
                compilationStartContext.RegisterSymbolAction(symbolActionContext =>
                {
                    var type = (ITypeSymbol)symbolActionContext.Symbol;
                    if (type.DeclaredAccessibility == Accessibility.Public && type.ContainingType == null)
                    {
                        // see if the type is annotated with one of the disqualifying attributes
                        foreach (var attr in type.GetAttributes())
                        {
                            if (attr.AttributeClass != null)
                            {
                                if (disqualifyingTypeAttributes.Contains(attr.AttributeClass))
                                {
                                    return;
                                }
                            }
                        }

                        // see if the type derives from one of the disqualifying base types
                        foreach (var c in disqualifyingBaseClasses)
                        {
                            if (c.IsAncestorOf(type))
                            {
                                return;
                            }
                        }

                        // see if any members are annotated with disqualifying attributes
                        var members = type.GetMembers();
                        foreach (var member in members)
                        {
                            var attrs = member.GetAttributes();
                            foreach (var attr in attrs)
                            {
                                if (attr.AttributeClass != null)
                                {
                                    if (disqualifyingMemberAttributes.Contains(attr.AttributeClass))
                                    {
                                        return;
                                    }
                                }
                            }
                        }

                        var diagnostic = Diagnostic.Create(DiagDescriptors.MakeExeTypesInternal, type.Locations[0], type.Name);
                        symbolActionContext.ReportDiagnostic(diagnostic);
                    }
                }, SymbolKind.NamedType);
            }
        });
    }
}
