// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.AutoClient;

[Generator]
public class AutoClientGenerator : IIncrementalGenerator
{
    private static readonly HashSet<string> _attributeNames =
    [
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
        SymbolLoader.RestBodyAttribute
    ];

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
