// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.DotNet.Analyzers.Async
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AsyncMethodAnalyzer : DiagnosticAnalyzer
    {
        private static readonly AsyncAnalysisData Store = new AsyncAnalysisData();

        public AsyncMethodAnalyzer()
        {
            SupportedDiagnostics = ImmutableArray.Create(new[]
            {
                Descriptors.ASYNC0001SynchronouslyBlockingMethod
            });
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(analysisContext =>
            {
                analysisContext.RegisterSyntaxNodeAction(syntaxContext => AnalyzeInvocation(syntaxContext, Store), SyntaxKind.InvocationExpression, SyntaxKind.SimpleMemberAccessExpression);
            });
        }

        private void AnalyzeInvocation(SyntaxNodeAnalysisContext syntaxContext, AsyncAnalysisData store)
        {
            // Skip non-async methods
            if (!(syntaxContext.ContainingSymbol is IMethodSymbol methodSymbol) || !methodSymbol.IsAsync)
            {
                return;
            }

            SymbolInfo symbolInfo;
            switch (syntaxContext.Node)
            {
                case InvocationExpressionSyntax invocation:
                    symbolInfo = syntaxContext.SemanticModel.GetSymbolInfo(invocation, syntaxContext.CancellationToken);
                    if (symbolInfo.Symbol?.Kind != SymbolKind.Method)
                    {
                        return;
                    }

                    break;
                case MemberAccessExpressionSyntax memberAccess:
                    symbolInfo = syntaxContext.SemanticModel.GetSymbolInfo(memberAccess, syntaxContext.CancellationToken);
                    if (symbolInfo.Symbol?.Kind != SymbolKind.Property)
                    {
                        return;
                    }

                    break;

                default:
                    return;
            }

            // Format type name separately to handle closed generic types correctly
            // For example for Task<int> we would like to look for System.Threading.Tasks.Task<TResult> key
            var originalType = symbolInfo.Symbol.ContainingType.ConstructedFrom.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
            if (store.Contains(originalType, symbolInfo.Symbol.Name))
            {
                syntaxContext.ReportDiagnostic(Diagnostic.Create(Descriptors.ASYNC0001SynchronouslyBlockingMethod, syntaxContext.Node.GetLocation()));
            }
        }
    }
}
