// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if ROSLYN_4_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.AutoClient;

[Generator]
[ExcludeFromCodeCoverage]
public class Generator : IIncrementalGenerator
{
    private static readonly HashSet<string> _attributeNames = new()
    {
        SymbolLoader.RestApiAttribute,

        SymbolLoader.RestGetAttribute,
        SymbolLoader.RestPostAttribute,
        SymbolLoader.RestPutAttribute,
        SymbolLoader.RestDeleteAttribute,
        SymbolLoader.RestPatchAttribute,
        SymbolLoader.RestOptionsAttribute,
        SymbolLoader.RestHeadAttribute,

        SymbolLoader.RestStaticHeaderAttribute,
        SymbolLoader.RestHeaderAttribute,
        SymbolLoader.RestQueryAttribute,
        SymbolLoader.RestBodyAttribute,
        SymbolLoader.RestRequestNameAttribute
    };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        GeneratorUtilities.Initialize(context, _attributeNames, HandleAnnotatedTypes);
    }

    private static void HandleAnnotatedTypes(Compilation compilation, IEnumerable<SyntaxNode> nodes, SourceProductionContext context)
    {
        var p = new Parser(compilation, context.ReportDiagnostic, context.CancellationToken);

        var restApiClasses = p.GetRestApiClasses(nodes.OfType<InterfaceDeclarationSyntax>());

        if (restApiClasses.Count > 0)
        {
            var emitter = new Emitter();

            var restApiCode = emitter.EmitRestApis(restApiClasses, context.CancellationToken);
            context.AddSource($"AutoClients.g.cs", SourceText.From(restApiCode, Encoding.UTF8));
        }
    }
}

#else

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.AutoClient;

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

        var parser = new Parser(context.Compilation, context.ReportDiagnostic, context.CancellationToken);

        var restApiClasses = parser.GetRestApiClasses(receiver.TypeDeclarations.OfType<InterfaceDeclarationSyntax>());

        if (restApiClasses.Count > 0)
        {
            var emitter = new Emitter();

            var restApiCode = emitter.EmitRestApis(restApiClasses, context.CancellationToken);
            context.AddSource($"AutoClients.g.cs", SourceText.From(restApiCode, Encoding.UTF8));
        }
    }
}

#endif
