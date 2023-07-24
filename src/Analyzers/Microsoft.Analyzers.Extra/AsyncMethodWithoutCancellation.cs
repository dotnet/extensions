// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Extensions.ExtraAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AsyncMethodWithoutCancellation : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        DiagDescriptors.AsyncMethodWithoutCancellation);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            // Stryker disable all : no reasonable means to test this
            // Get the target types.
            var taskType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
            var taskOfTType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
            var valueTaskType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
            var valueTaskOfTType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");

            // If task types don't exist, nothing more to do.
            if (taskType == null &&
                taskOfTType == null &&
                valueTaskType == null &&
                valueTaskOfTType == null)
            {
                return;
            }

            var cancellationTokenType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.CancellationToken");
            var httpContextType = compilationContext.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Http.HttpContext");
            var connectionContextType =
                compilationContext.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Connections.ConnectionContext");
            var obsoleteAttribute =
                compilationContext.Compilation.GetTypeByMetadataName("System.ObsoleteAttribute");

            var knownTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
            if (cancellationTokenType != null)
            {
                _ = knownTypes.Add(cancellationTokenType);
            }

            if (httpContextType != null)
            {
                _ = knownTypes.Add(httpContextType);
            }

            if (connectionContextType != null)
            {
                _ = knownTypes.Add(connectionContextType);
            }

            if (knownTypes.Count == 0)
            {
                return;
            }

            // Stryker restore all
            compilationContext.RegisterSyntaxNodeAction(analysisContext =>
            {
                var methodSymbol = (IMethodSymbol)analysisContext.ContainingSymbol!;

                // ignore overrides
                if (methodSymbol.IsOverride)
                {
                    return;
                }

                // ignore obsoleted methods
                if (IsObsoleteSymbol(methodSymbol))
                {
                    return;
                }

                // ignore obsoleted types
                if (IsObsoleteSymbol(methodSymbol.ContainingType))
                {
                    return;
                }

                if (!IsReturnTypeTask(methodSymbol))
                {
                    return;
                }

                if (MethodParametersContainKnownTypes(methodSymbol, knownTypes))
                {
                    return;
                }

                // ignore interface implementations
                if (IsImplementationOfInterface(methodSymbol))
                {
                    return;
                }

                var diagnostic =
                    Diagnostic.Create(DiagDescriptors.AsyncMethodWithoutCancellation, analysisContext.Node.GetLocation());
                analysisContext.ReportDiagnostic(diagnostic);
            }, SyntaxKind.MethodDeclaration);

            bool IsReturnTypeTask(IMethodSymbol method)
            {
                var returnType = method.ReturnType.OriginalDefinition;
                return SymbolEqualityComparer.Default.Equals(returnType, taskType) ||
                       SymbolEqualityComparer.Default.Equals(returnType, taskOfTType) ||
                       SymbolEqualityComparer.Default.Equals(returnType, valueTaskType) ||
                       SymbolEqualityComparer.Default.Equals(returnType, valueTaskOfTType);
            }

            bool IsObsoleteSymbol(ISymbol symbol)
            {
                if (obsoleteAttribute == null)
                {
                    return false;
                }

                return symbol
                    .GetAttributes()
                    .Any(data =>
                        SymbolEqualityComparer.Default.Equals(data.AttributeClass, obsoleteAttribute));
            }
        });
    }

    private static bool IsImplementationOfInterface(IMethodSymbol method)
    {
        var containingType = method.ContainingType;
        foreach (var @interface in containingType.AllInterfaces)
        {
            if (@interface.GetMembers().OfType<IMethodSymbol>()
                .Select(interfaceSymbol => containingType.FindImplementationForInterfaceMember(interfaceSymbol))
                .Any(implementation => SymbolEqualityComparer.Default.Equals(implementation, method)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MethodParametersContainKnownTypes(IMethodSymbol method, HashSet<ITypeSymbol> typeSymbols)
    {
        foreach (var argument in method.Parameters)
        {
            if (typeSymbols.Contains(argument.Type))
            {
                return true;
            }
        }

        return false;
    }
}
