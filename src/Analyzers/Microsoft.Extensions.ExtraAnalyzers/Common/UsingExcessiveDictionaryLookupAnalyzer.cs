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
/// C# analyzer that finds excessive dictionary lookups.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UsingExcessiveDictionaryLookupAnalyzer : DiagnosticAnalyzer
{
    private static readonly HashSet<string> _containsKeyMethodFullNames = new(StringComparer.Ordinal)
    {
        "System.Collections.Generic.IDictionary<TKey, TValue>.ContainsKey(TKey)",
        "System.Collections.Generic.Dictionary<TKey, TValue>.ContainsKey(TKey)",
        "System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>.ContainsKey(TKey)"
    };

    private static readonly HashSet<string> _addMethodFullNames = new(StringComparer.Ordinal)
    {
        "System.Collections.Generic.IDictionary<TKey, TValue>.Add(TKey, TValue)",
        "System.Collections.Generic.IDictionary<TKey, TValue>.TryAdd<TKey, TValue>(TKey, TValue)",
        "System.Collections.Generic.Dictionary<TKey, TValue>.TryAdd(TKey, TValue)",
        "System.Collections.Generic.Dictionary<TKey, TValue>.Add(TKey, TValue)"
    };

    private static readonly HashSet<string> _otherMethodsFullNamesToCheck = new(_addMethodFullNames, StringComparer.Ordinal)
    {
        "System.Collections.Generic.IDictionary<TKey, TValue>.Remove<TKey, TValue>(TKey, out TValue)",
        "System.Collections.Generic.IDictionary<TKey, TValue>.Remove(TKey)",
        "System.Collections.Generic.IDictionary<TKey, TValue>.TryGetValue(TKey, out TValue)",
        "System.Collections.Generic.Dictionary<TKey, TValue>.Remove(TKey, out TValue)",
        "System.Collections.Generic.Dictionary<TKey, TValue>.Remove(TKey)",
        "System.Collections.Generic.Dictionary<TKey, TValue>.TryGetValue(TKey, out TValue)",
        "System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>.TryGetValue(TKey, out TValue)"
    };

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagDescriptors.UsingExcessiveDictionaryLookup);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(syntaxNodeContext =>
        {
            var ifStatement = (IfStatementSyntax)syntaxNodeContext.Node;
            var invocationExpression = GetInvocationExpression(ifStatement.Condition)!;
            if (!invocationExpression.NodeHasSpecifiedMethod(syntaxNodeContext.SemanticModel, _containsKeyMethodFullNames))
            {
                return;
            }

            string? expectedDictionaryIdentifierNameText =
                invocationExpression.Expression is MemberAccessExpressionSyntax memberAccessExpression &&
                memberAccessExpression.Expression is IdentifierNameSyntax identifierNameSyntax
                    ? identifierNameSyntax.Identifier.Text
                    : null;

            if (expectedDictionaryIdentifierNameText == null)
            {
                return;
            }

            var expectedContainsMethodArgument = invocationExpression.ArgumentList.Arguments[0];
            if (expectedContainsMethodArgument!.DescendantNodesAndSelf().Any(w => w is InvocationExpressionSyntax))
            {
                return;
            }

            var expectedContainsMethodArgumentText = expectedContainsMethodArgument!.GetText().ToString();

            if (ifStatement.Else != null)
            {
                string? valueAddedToDictionaryInElse = ProcessStatementSyntaxAndGetAssignedValue(
                                                                    ifStatement.Else.Statement,
                                                                    syntaxNodeContext.SemanticModel,
                                                                    expectedDictionaryIdentifierNameText,
                                                                    expectedContainsMethodArgumentText);

                if (!string.IsNullOrEmpty(valueAddedToDictionaryInElse) &&
                    valueAddedToDictionaryInElse!.Equals(
                        ProcessStatementSyntaxAndGetAssignedValue(
                            ifStatement.Statement,
                            syntaxNodeContext.SemanticModel,
                            expectedDictionaryIdentifierNameText,
                            expectedContainsMethodArgumentText),
                        StringComparison.OrdinalIgnoreCase))
                {
                    CreateDiagnosticAndReport(invocationExpression.GetLocation(), syntaxNodeContext);
                }

                return;
            }

            bool isDictionaryUsedInIfBody = ifStatement.Statement.DescendantNodes()
                .Any(w => w is IdentifierNameSyntax id && id.Identifier.Text == expectedDictionaryIdentifierNameText);

            if (isDictionaryUsedInIfBody)
            {
                if (ifStatement.Statement is BlockSyntax block)
                {
                    if (block.Statements.Count > 1)
                    {
                        CheckMultilineBlockAndReportDiagnostic(
                            block,
                            syntaxNodeContext,
                            expectedDictionaryIdentifierNameText,
                            expectedContainsMethodArgumentText,
                            invocationExpression.GetLocation());
                    }
                    else
                    {
                        CheckSingleLineBlockAndReportDiagnostic(
                            block.Statements[0],
                            syntaxNodeContext,
                            expectedDictionaryIdentifierNameText,
                            expectedContainsMethodArgumentText,
                            invocationExpression.GetLocation());
                    }
                }
                else
                {
                    CheckSingleLineBlockAndReportDiagnostic(
                        ifStatement.Statement,
                        syntaxNodeContext,
                        expectedDictionaryIdentifierNameText,
                        expectedContainsMethodArgumentText,
                        invocationExpression.GetLocation());
                }
            }
            else if (ifStatement.Parent is BlockSyntax parentBlock)
            {
                var next = parentBlock.Statements.SkipWhile(w => w != ifStatement).Skip(1).FirstOrDefault();
                CheckSingleLineBlockAndReportDiagnostic(
                    next,
                    syntaxNodeContext,
                    expectedDictionaryIdentifierNameText,
                    expectedContainsMethodArgumentText,
                    null);
            }
        }, SyntaxKind.IfStatement);
    }

    private static string? ProcessStatementSyntaxAndGetAssignedValue(
        StatementSyntax statement,
        SemanticModel semanticModel,
        string expectedDictionaryIdentifierName,
        string expectedContainsMethodArgumentText)
    {
        if (statement is BlockSyntax block)
        {
            if (block.Statements.Count == 1 && block.Statements[0] is ExpressionStatementSyntax expressionStatement)
            {
                return ProcessExpressionStatementSyntaxAndGetAssignedValue(expressionStatement, semanticModel, expectedDictionaryIdentifierName, expectedContainsMethodArgumentText);
            }
        }
        else if (statement is ExpressionStatementSyntax expressionStatement)
        {
            return ProcessExpressionStatementSyntaxAndGetAssignedValue(expressionStatement, semanticModel, expectedDictionaryIdentifierName, expectedContainsMethodArgumentText);
        }

        return null;
    }

    private static string? ProcessExpressionStatementSyntaxAndGetAssignedValue(
        ExpressionStatementSyntax expressionStatement,
        SemanticModel semanticModel,
        string expectedDictionaryIdentifierName,
        string expectedContainsMethodArgumentText)
    {
        if (expressionStatement.Expression.NodeHasSpecifiedMethod(semanticModel, _addMethodFullNames))
        {
            var foundInvocation = (InvocationExpressionSyntax)expressionStatement.Expression;
            if (CheckIndentifierNameAndFirstArgument(
                (foundInvocation.Expression as MemberAccessExpressionSyntax)!.Expression,
                foundInvocation.ArgumentList,
                expectedDictionaryIdentifierName,
                expectedContainsMethodArgumentText))
            {
                return foundInvocation.ArgumentList.Arguments[1].Expression.GetText().ToString();
            }
        }

        if (expressionStatement.Expression is AssignmentExpressionSyntax assignmentExpression)
        {
            if (assignmentExpression.Left is ElementAccessExpressionSyntax elementAccessExpr &&
                CheckIndentifierNameAndFirstArgument(
                    elementAccessExpr.Expression,
                    elementAccessExpr.ArgumentList,
                    expectedDictionaryIdentifierName,
                    expectedContainsMethodArgumentText))
            {
                return assignmentExpression.Right.GetText().ToString();
            }
        }

        return null;
    }

    private static void CheckMultilineBlockAndReportDiagnostic(
        SyntaxNode block,
        SyntaxNodeAnalysisContext syntaxNodeContext,
        string expectedDictionaryIdentifierNameText,
        string expectedContainsMethodArgumentText,
        Location invocationExpressionLocation)
    {
        var dictionaryItemAccessExpressions =
                                block
                                .DescendantNodes()
                                .Where(w => w is ElementAccessExpressionSyntax elementAccessExpr &&
                                        CheckIndentifierNameAndFirstArgument(
                                            elementAccessExpr.Expression,
                                            elementAccessExpr.ArgumentList,
                                            expectedDictionaryIdentifierNameText,
                                            expectedContainsMethodArgumentText));

        bool isInefficientDictionaryUsageFound = dictionaryItemAccessExpressions.Any(
            w =>
            {
                var isRightPartOfAssignmentExpression = w.Parent is AssignmentExpressionSyntax a && a.Right == w;
                var isRightPartOfEqualsValueClause = w.Parent is EqualsValueClauseSyntax e && e.Value == w;
                var isUsedAsArgument = w.Parent is ArgumentSyntax arg && arg.Expression == w;

                return isRightPartOfAssignmentExpression || isRightPartOfEqualsValueClause || isUsedAsArgument;
            });

        if (isInefficientDictionaryUsageFound)
        {
            CreateDiagnosticAndReport(invocationExpressionLocation, syntaxNodeContext);
        }
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
        string expectedDictionaryIdentifierName,
        string expectedContainsMethodArgumentText,
        Location? locationToReport)
    {
        if (statementSyntax is ExpressionStatementSyntax expressionStatement)
        {
            if (CheckSingleLineBlock(expressionStatement.Expression, syntaxNodeContext.SemanticModel, expectedDictionaryIdentifierName, expectedContainsMethodArgumentText))
            {
                CreateDiagnosticAndReport(locationToReport ?? expressionStatement.Expression.GetLocation(), syntaxNodeContext);
                return;
            }

            if (expressionStatement.Expression is AssignmentExpressionSyntax assignmentExpression)
            {
                if (assignmentExpression.Left is ElementAccessExpressionSyntax elementAccessExpr &&
                    CheckIndentifierNameAndFirstArgument(elementAccessExpr.Expression, elementAccessExpr.ArgumentList, expectedDictionaryIdentifierName, expectedContainsMethodArgumentText))
                {
                    CreateDiagnosticAndReport(locationToReport ?? assignmentExpression.Left.GetLocation(), syntaxNodeContext);
                    return;
                }

                bool shouldReport = assignmentExpression.Right is ElementAccessExpressionSyntax rightElementAccessExpr &&
                        CheckIndentifierNameAndFirstArgument(
                            rightElementAccessExpr.Expression,
                            rightElementAccessExpr.ArgumentList,
                            expectedDictionaryIdentifierName,
                            expectedContainsMethodArgumentText);
                if (shouldReport || CheckSingleLineBlock(assignmentExpression.Right, syntaxNodeContext.SemanticModel, expectedDictionaryIdentifierName, expectedContainsMethodArgumentText))
                {
                    CreateDiagnosticAndReport(locationToReport ?? assignmentExpression.Right.GetLocation(), syntaxNodeContext);
                }
            }
        }
    }

    private static void CreateDiagnosticAndReport(Location locationToReport, SyntaxNodeAnalysisContext syntaxNodeContext)
    {
        var diagnostic = Diagnostic.Create(DiagDescriptors.UsingExcessiveDictionaryLookup, locationToReport);
        syntaxNodeContext.ReportDiagnostic(diagnostic);
    }

    private static bool CheckSingleLineBlock(
        SyntaxNode node,
        SemanticModel semanticModel,
        string expectedDictionaryIdentifierName,
        string expectedContainsMethodArgumentText)
    {
        if (node.NodeHasSpecifiedMethod(semanticModel, _otherMethodsFullNamesToCheck))
        {
            var foundInvocation = (InvocationExpressionSyntax)node;
            var memberAccessExpression = foundInvocation.Expression as MemberAccessExpressionSyntax;
            return CheckIndentifierNameAndFirstArgument(memberAccessExpression!.Expression, foundInvocation.ArgumentList, expectedDictionaryIdentifierName, expectedContainsMethodArgumentText);
        }

        return false;
    }
}
