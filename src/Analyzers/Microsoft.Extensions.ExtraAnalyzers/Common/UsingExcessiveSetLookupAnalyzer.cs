// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.ExtraAnalyzers.Utilities;

namespace Microsoft.Extensions.ExtraAnalyzers;

/// <summary>
/// C# analyzer that finds excessive set lookups.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UsingExcessiveSetLookupAnalyzer : DiagnosticAnalyzer
{
    private const string SetCollectionFullName = "System.Collections.Generic.ISet<T>";
    private static readonly HashSet<string> _collectionContainsMethodFullName = new(StringComparer.Ordinal)
    {
        "System.Collections.Generic.ICollection<T>.Contains(T)"
    };

    private static readonly HashSet<string> _containsMethodFullNames = new(StringComparer.Ordinal)
    {
        "System.Collections.Generic.SortedSet<T>.Contains(T)",
        "System.Collections.Generic.HashSet<T>.Contains(T)",
        "System.Collections.Immutable.ImmutableHashSet<T>.Contains(T)",
        "System.Collections.Immutable.ImmutableSortedSet<T>.Contains(T)",
        "System.Collections.Immutable.ImmutableHashSet<T>.Builder.Contains(T)",
        "System.Collections.Immutable.ImmutableSortedSet<T>.Builder.Contains(T)"
    };

    private static readonly HashSet<string> _methodsReturningBool = new(StringComparer.Ordinal)
    {
        "System.Collections.Generic.SortedSet<T>.Remove(T)",
        "System.Collections.Generic.HashSet<T>.Remove(T)",
        "System.Collections.Generic.ICollection<T>.Remove(T)",
        "System.Collections.Immutable.ImmutableHashSet<T>.Builder.Remove(T)",
        "System.Collections.Immutable.ImmutableSortedSet<T>.Builder.Remove(T)",

        "System.Collections.Generic.SortedSet<T>.TryGetValue(T, out T)",
        "System.Collections.Generic.HashSet<T>.TryGetValue(T, out T)",
        "System.Collections.Immutable.ImmutableHashSet<T>.TryGetValue(T, out T)",
        "System.Collections.Immutable.ImmutableSortedSet<T>.TryGetValue(T, out T)",

        "System.Collections.Generic.SortedSet<T>.Add(T)",
        "System.Collections.Generic.HashSet<T>.Add(T)",
        "System.Collections.Generic.ISet<T>.Add(T)",
        "System.Collections.Immutable.ImmutableHashSet<T>.Builder.Add(T)",
        "System.Collections.Immutable.ImmutableSortedSet<T>.Builder.Add(T)"
    };

    private static readonly HashSet<string> _otherMethodsFullNamesToCheck = new(_methodsReturningBool, StringComparer.Ordinal)
    {
        "System.Collections.Immutable.ImmutableHashSet<T>.Remove(T)",
        "System.Collections.Immutable.ImmutableSortedSet<T>.Remove(T)",
        "System.Collections.Immutable.ImmutableHashSet<T>.Add(T)",
        "System.Collections.Immutable.ImmutableSortedSet<T>.Add(T)",
    };

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagDescriptors.UsingExcessiveSetLookup);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        _ = context ?? throw new ArgumentNullException(nameof(context));

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(syntaxNodeContext =>
        {
            var ifStatement = (IfStatementSyntax)syntaxNodeContext.Node;
            if (ifStatement.Else != null)
            {
                return;
            }

            var invocationExpression = GetInvocationExpression(ifStatement.Condition)!;
            if (!invocationExpression.NodeHasSpecifiedMethod(syntaxNodeContext.SemanticModel, _containsMethodFullNames)
                && !(invocationExpression.NodeHasSpecifiedMethod(syntaxNodeContext.SemanticModel, _collectionContainsMethodFullName)
                    && NodeHasSpecifiedType(invocationExpression, syntaxNodeContext.SemanticModel)))
            {
                return;
            }

            string? expectedSetIdentifierNameText =
                invocationExpression.Expression is MemberAccessExpressionSyntax memberAccessExpression &&
                memberAccessExpression.Expression is IdentifierNameSyntax identifierNameSyntax
                    ? identifierNameSyntax.Identifier.Text
                    : null;

            if (expectedSetIdentifierNameText == null)
            {
                return;
            }

            var expectedContainsMethodArgument = invocationExpression.ArgumentList.Arguments[0];
            if (expectedContainsMethodArgument!.DescendantNodesAndSelf().Any(w => w is InvocationExpressionSyntax))
            {
                return;
            }

            var expectedContainsMethodArgumentText = expectedContainsMethodArgument!.GetText().ToString();

            bool isSetUsedInIfBody = ifStatement.Statement.DescendantNodes()
                .Any(w => w is IdentifierNameSyntax id && id.Identifier.Text == expectedSetIdentifierNameText);

            if (isSetUsedInIfBody)
            {
                var lineToCheck = ifStatement.Statement is BlockSyntax block ? block.Statements[0] : ifStatement.Statement;
                CheckSingleLineBlockAndReportDiagnostic(
                        lineToCheck,
                        syntaxNodeContext,
                        expectedSetIdentifierNameText,
                        expectedContainsMethodArgumentText,
                        invocationExpression.GetLocation());
            }
            else if (ifStatement.Parent is BlockSyntax parentBlock)
            {
                var next = parentBlock.Statements.SkipWhile(w => w != ifStatement).Skip(1).FirstOrDefault();
                CheckSingleLineBlockAndReportDiagnostic(
                    next,
                    syntaxNodeContext,
                    expectedSetIdentifierNameText,
                    expectedContainsMethodArgumentText,
                    null,
                    checkOnlyMethodsReturningBool: true);
            }
        }, SyntaxKind.IfStatement);
    }

    private static bool NodeHasSpecifiedType(
        InvocationExpressionSyntax? invocationExpression,
        SemanticModel semanticModel)
    {
        if (invocationExpression != null)
        {
            var memberAccessExpression = (MemberAccessExpressionSyntax)invocationExpression.Expression;
            if (memberAccessExpression!.Expression is IdentifierNameSyntax identifierNameSyntax)
            {
                var symbol = semanticModel.GetTypeInfo(identifierNameSyntax).Type;
                return symbol!.OriginalDefinition!.ToString() == SetCollectionFullName;
            }
        }

        return false;
    }

    private static InvocationExpressionSyntax? GetInvocationExpression(ExpressionSyntax expression)
    {
        if (expression is PrefixUnaryExpressionSyntax logicalExpr && logicalExpr.IsKind(SyntaxKind.LogicalNotExpression))
        {
            return logicalExpr.Operand as InvocationExpressionSyntax;
        }

        return expression as InvocationExpressionSyntax;
    }

    private static bool CheckIndentifierNameAndFirstArgument(ExpressionSyntax expression, BaseArgumentListSyntax argumentListSyntax, string expectedIndetifierName, string expectedFirstArgument)
    {
        return expression.IdentifierNameEquals(expectedIndetifierName) &&
                        argumentListSyntax.Arguments[0].GetText().ToString() == expectedFirstArgument;
    }

    private static void CheckSingleLineBlockAndReportDiagnostic(
        StatementSyntax statementSyntax,
        SyntaxNodeAnalysisContext syntaxNodeContext,
        string expectedSetIdentifierName,
        string expectedContainsMethodArgumentText,
        Location? locationToReport,
        bool checkOnlyMethodsReturningBool = false)
    {
        if (statementSyntax is ExpressionStatementSyntax expressionStatement)
        {
            if (expressionStatement.Expression is AssignmentExpressionSyntax assignmentExpression)
            {
                var invocationExpr = GetInvocationExpression(assignmentExpression.Right);
                if (assignmentExpression.Left.IdentifierNameEquals("_") &&
                    CheckSingleLineBlock(invocationExpr, syntaxNodeContext.SemanticModel, expectedSetIdentifierName, expectedContainsMethodArgumentText, _methodsReturningBool))
                {
                    CreateDiagnosticAndReport(locationToReport ?? invocationExpr!.GetLocation(), syntaxNodeContext);
                }

                return;
            }

            var invocationExpression = GetInvocationExpression(expressionStatement.Expression);
            var methodsToCheck = checkOnlyMethodsReturningBool ? _methodsReturningBool : _otherMethodsFullNamesToCheck;
            if (CheckSingleLineBlock(invocationExpression, syntaxNodeContext.SemanticModel, expectedSetIdentifierName, expectedContainsMethodArgumentText, methodsToCheck))
            {
                CreateDiagnosticAndReport(locationToReport ?? invocationExpression!.GetLocation(), syntaxNodeContext);
            }
        }
    }

    private static bool CheckSingleLineBlock(
        SyntaxNode? node,
        SemanticModel semanticModel,
        string expectedSetIdentifierName,
        string expectedContainsMethodArgumentText,
        ICollection<string> methodsToCheck)
    {
        if (node.NodeHasSpecifiedMethod(semanticModel, methodsToCheck))
        {
            var foundInvocation = (InvocationExpressionSyntax)node!;
            var memberAccessExpression = (MemberAccessExpressionSyntax)foundInvocation.Expression;
            return CheckIndentifierNameAndFirstArgument(memberAccessExpression!.Expression, foundInvocation.ArgumentList, expectedSetIdentifierName, expectedContainsMethodArgumentText);
        }

        return false;
    }

    private static void CreateDiagnosticAndReport(Location locationToReport, SyntaxNodeAnalysisContext syntaxNodeContext)
    {
        var diagnostic = Diagnostic.Create(DiagDescriptors.UsingExcessiveSetLookup, locationToReport);
        syntaxNodeContext.ReportDiagnostic(diagnostic);
    }
}
