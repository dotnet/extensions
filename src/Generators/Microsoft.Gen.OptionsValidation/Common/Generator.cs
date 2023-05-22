// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if ROSLYN_4_0_OR_GREATER

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.OptionsValidation;

[Generator]
[ExcludeFromCodeCoverage]
public class Generator : IIncrementalGenerator
{
    private static readonly HashSet<string> _attributeNames = new()
    {
        SymbolLoader.OptionsValidatorAttribute,
    };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        GeneratorUtilities.Initialize(context, _attributeNames, HandleAnnotatedTypes);
    }

    private static void HandleAnnotatedTypes(Compilation compilation, IEnumerable<SyntaxNode> nodes, SourceProductionContext context)
    {
        if (!SymbolLoader.TryLoad(compilation, out var symbolHolder))
        {
            // Not eligible compilation
            return;
        }

        var parser = new Parser(compilation, context.ReportDiagnostic, symbolHolder!, context.CancellationToken);

        var validatorTypes = parser.GetValidatorTypes(nodes.OfType<TypeDeclarationSyntax>());
        if (validatorTypes.Count > 0)
        {
            var emitter = new Emitter();
            var result = emitter.Emit(validatorTypes, context.CancellationToken);

            context.AddSource("Validators.g.cs", SourceText.From(result, Encoding.UTF8));
        }
    }
}

#else

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.OptionsValidation;

/// <summary>
/// Generates <see cref="Extensions.Options.IValidateOptions{T}" /> for classes that are marked with <see cref="R9.Extensions.Options.OptionsValidatorAttribute" />.
/// </summary>
[Generator]
[ExcludeFromCodeCoverage]
public class Generator : ISourceGenerator
{
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

        if (!SymbolLoader.TryLoad(context.Compilation, out var symbolHolder))
        {
            // Not eligible compilation
            return;
        }

        var parser = new Parser(context.Compilation, context.ReportDiagnostic, symbolHolder!, context.CancellationToken);
        var validatorTypes = parser.GetValidatorTypes(receiver.TypeDeclarations);
        if (validatorTypes.Count > 0)
        {
            var emitter = new Emitter();
            var result = emitter.Emit(validatorTypes, context.CancellationToken);
            context.AddSource("Validators.g.cs", SourceText.From(result, Encoding.UTF8));
        }
    }
}

#endif
