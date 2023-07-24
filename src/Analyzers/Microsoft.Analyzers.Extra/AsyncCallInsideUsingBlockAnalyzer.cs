// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.Extensions.ExtraAnalyzers.Utilities;

namespace Microsoft.Extensions.ExtraAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AsyncCallInsideUsingBlockAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        DiagDescriptors.AsyncCallInsideUsingBlock);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            // Stryker disable all : no reasonable means to test this
            // Get the target Task / Task<T> / ValueTask / ValueTask<T> types.
            var taskType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
            var taskOfTType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
            var valueTaskType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
            var valueTaskOfTType = compilationContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");

            // If they don't exist, nothing more to do.
            if (taskType == null &&
                taskOfTType == null &&
                valueTaskType == null &&
                valueTaskOfTType == null)
            {
                return;
            }

            // Stryker restore all

            compilationContext.RegisterOperationAction(analysisContext =>
            {
                var operation = (IUsingDeclarationOperation)analysisContext.Operation;
                var disposable = GetDisposableSymbol(operation.DeclarationGroup);

                if (operation.Parent == null)
                {
                    return;
                }

                ValidateDisposable(analysisContext, disposable, operation.Parent);
            }, OperationKind.UsingDeclaration);

            compilationContext.RegisterOperationAction(analysisContext =>
            {
                var operation = (IUsingOperation)analysisContext.Operation;

                // Declaration introduced or resource held by the using
                if (operation.Resources is not IVariableDeclarationGroupOperation declarationGroup)
                {
                    return;
                }

                var disposable = GetDisposableSymbol(declarationGroup);
                ValidateDisposable(analysisContext, disposable, operation.Body);
            }, OperationKind.Using);

            void ValidateDisposable(OperationAnalysisContext analysisContext,
                ILocalSymbol disposable,
                IOperation block)
            {
                // Find all the invocations that return Task with the disposable object passed via arguments
                var invocations = block.Descendants()
                    .OfType<IInvocationOperation>()
                    .Where(IsReturnTypeTask)
                    .Where(operation => SymbolInArguments(operation, disposable));

                foreach (var invocation in invocations)
                {
                    if (invocation.Ancestors(block)
                        .Any(operation =>
                        {
                            switch (operation.Kind)
                            {
                                // check if the returned task is awaited inline
                                case OperationKind.Await:
                                    return true;

                                // check if the task.Wait is used
                                case OperationKind.Invocation:
                                    return TaskWaitInvoked((operation as IInvocationOperation)!);

                                // check if the task.Result is used
                                case OperationKind.PropertyReference:
                                    return TaskResultInvoked((operation as IPropertyReferenceOperation)!);

                                // check if the returned task is assigned to a declared variable and then awaited (async or sync)
                                case OperationKind.VariableDeclarator:
                                    return IsTaskAwaited(block, (operation as IVariableDeclaratorOperation)!.Symbol);

                                // check if the returned task is assigned to a variable declared previously and then awaited
                                case OperationKind.SimpleAssignment:
                                {
                                    var assignmentTarget = ((IAssignmentOperation)operation).Target as ILocalReferenceOperation;
                                    return assignmentTarget != null && IsTaskAwaited(block, assignmentTarget.Local);
                                }

                                // check if the invocation result is passed to lambda - ignore such cases for now
                                case OperationKind.AnonymousFunction:
                                    return true;
                            }

                            return false;
                        }))
                    {
                        continue;
                    }

                    var diagnostic =
                        Diagnostic.Create(DiagDescriptors.AsyncCallInsideUsingBlock, invocation.Syntax.GetLocation());
                    analysisContext.ReportDiagnostic(diagnostic);
                }
            }

            bool IsReturnTypeTask(IInvocationOperation operation)
            {
                var returnType = operation.Type?.OriginalDefinition;
                if (returnType == null)
                {
                    // Stryker disable once boolean : no means to test this
                    return false;
                }

                return SymbolEqualityComparer.Default.Equals(returnType, taskType) ||
                       SymbolEqualityComparer.Default.Equals(returnType, taskOfTType) ||
                       SymbolEqualityComparer.Default.Equals(returnType, valueTaskType) ||
                       SymbolEqualityComparer.Default.Equals(returnType, valueTaskOfTType);
            }
        });
    }

    private static ILocalSymbol GetDisposableSymbol(IVariableDeclarationGroupOperation declarationGroup)
    {
        // All `IVariableDeclarationGroupOperation` will have at least 1 `IVariableDeclarationOperation`,
        // even if the declaration group only declares 1 variable.
        // In C#, this will always be a single declaration
        return declarationGroup.Declarations[0].Declarators[0].Symbol;
    }

    private static bool IsTaskAwaited(IOperation block, ILocalSymbol taskSymbol)
    {
        if (block.Descendants()
            .OfType<IAwaitOperation>()
            .SelectMany(operation => operation.Descendants())
            .Any(operation => ReferencesSymbol(operation, taskSymbol)))
        {
            return true;
        }

        if (block.Descendants()
            .OfType<IInvocationOperation>()
            .Where(operation => ReferencesSymbol(operation.Instance, taskSymbol))
            .Any(TaskWaitInvoked))
        {
            return true;
        }

        if (block.Descendants()
            .OfType<IPropertyReferenceOperation>()
            .Where(operation => ReferencesSymbol(operation.Instance, taskSymbol))
            .Any(TaskResultInvoked))
        {
            return true;
        }

        return false;
    }

    private static bool ReferencesSymbol(IOperation? operation, ILocalSymbol symbol)
    {
        if (operation == null)
        {
            return false;
        }

        if (operation is not ILocalReferenceOperation localReference)
        {
            return false;
        }

        return SymbolEqualityComparer.Default.Equals(localReference.Local, symbol);
    }

    private static bool TaskWaitInvoked(IInvocationOperation invocation)
    {
        return invocation.TargetMethod.Name is "Wait" or "GetAwaiter";
    }

    private static bool TaskResultInvoked(IPropertyReferenceOperation operation)
    {
        return operation.Property.Name is "Result";
    }

    private static bool SymbolInArguments(IInvocationOperation invocation, ILocalSymbol symbol)
    {
        foreach (var argument in invocation.Arguments)
        {
            if (argument
                .Value
                .ChildOperations
                .Any(operation => ReferencesSymbol(operation, symbol)))
            {
                return true;
            }
        }

        return false;
    }
}
