// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.Extensions.ExtraAnalyzers.Utilities;

namespace Microsoft.Extensions.ExtraAnalyzers;

/// <summary>
/// Removes excessive dictionary lookups.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UsingExcessiveDictionaryLookupFixer))]
[Shared]
public sealed class UsingExcessiveDictionaryLookupFixer : CodeFixProvider
{
    private const string ContainsKeyMethodName = "ContainsKey";
    private static readonly SimpleNameSyntax _tryGetValueMethod = (SimpleNameSyntax)SyntaxFactory.ParseName("TryGetValue");
    private static readonly SimpleNameSyntax _tryAddMethod = (SimpleNameSyntax)SyntaxFactory.ParseName("TryAdd");
    private static readonly HashSet<string> _removeAndTryGetMethodFullNames = new()
    {
        "System.Collections.Generic.IDictionary<TKey, TValue>.Remove<TKey, TValue>(TKey, out TValue)",
        "System.Collections.Generic.IDictionary<TKey, TValue>.Remove(TKey)",
        "System.Collections.Generic.Dictionary<TKey, TValue>.Remove(TKey)",
        "System.Collections.Generic.Dictionary<TKey, TValue>.Remove(TKey, out TValue)",
        "System.Collections.Generic.IDictionary<TKey, TValue>.TryGetValue(TKey, out TValue)",
        "System.Collections.Generic.Dictionary<TKey, TValue>.TryGetValue(TKey, out TValue)",
        "System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>.TryGetValue(TKey, out TValue)"
    };

    private static readonly HashSet<string> _methodsFullNamesToBeHanled = new(_removeAndTryGetMethodFullNames)
    {
        "System.Collections.Generic.IDictionary<TKey, TValue>.Add(TKey, TValue)",
        "System.Collections.Generic.IDictionary<TKey, TValue>.TryAdd<TKey, TValue>(TKey, TValue)",
        "System.Collections.Generic.Dictionary<TKey, TValue>.TryAdd(TKey, TValue)",
        "System.Collections.Generic.Dictionary<TKey, TValue>.Add(TKey, TValue)"
    };

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagDescriptors.UsingExcessiveDictionaryLookup.Id);

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc/>
    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        context.RegisterCodeFix(
               CodeAction.Create(
                   title: Resources.UsingExcessiveDictionaryLookupTitle,
                   createChangedDocument: cancellationToken => ApplyFixAsync(context.Document, context.Diagnostics.First().Location, cancellationToken),
                   equivalenceKey: nameof(Resources.UsingExcessiveDictionaryLookupTitle)),
               context.Diagnostics);

        return Task.CompletedTask;
    }

    private static async Task<Document> ApplyFixAsync(Document document, Location diagnosticLocation, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        var invocationExpression = editor.OriginalRoot.FindNode(diagnosticLocation.SourceSpan) as InvocationExpressionSyntax;
        var memberAccessExpression = invocationExpression?.Expression as MemberAccessExpressionSyntax;
        if (memberAccessExpression == null || memberAccessExpression.Name.GetText().ToString() != ContainsKeyMethodName)
        {
            return document;
        }

        var expectedDictionaryIdentifier = (IdentifierNameSyntax)memberAccessExpression.Expression;

        var expectedKeyArgument = invocationExpression!.ArgumentList.Arguments[0];
        var ifStatementSyntax = (IfStatementSyntax)invocationExpression.GetFirstAncestorOfSyntaxKind(SyntaxKind.IfStatement)!;

        if (ifStatementSyntax.Else != null)
        {
            var separatedList = SyntaxFactory.SeparatedList(new[] { expectedKeyArgument });
            var bracketedArgumentList = SyntaxFactory.BracketedArgumentList(SyntaxFactory.ParseToken("["), separatedList, SyntaxFactory.ParseToken("]"));
            var elementAccessExpression = SyntaxFactory.ElementAccessExpression(expectedDictionaryIdentifier, bracketedArgumentList);

            var expressionStatementSyntax = (ExpressionStatementSyntax)(ifStatementSyntax.Else.Statement is BlockSyntax blockInsideElse
                                           ? blockInsideElse.Statements[0]
                                           : ifStatementSyntax.Else.Statement);

            var assignedValue = expressionStatementSyntax.Expression is InvocationExpressionSyntax inv
                ? inv.ArgumentList.Arguments[1].Expression
                : ((AssignmentExpressionSyntax)expressionStatementSyntax.Expression).Right;

            var simpleAssignmentExpression = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, elementAccessExpression, assignedValue);

            return CreateExpressionStatementAndReplaceOldNode(simpleAssignmentExpression, ifStatementSyntax, editor);
        }

        var expectedDictionaryIdentifierNameText = expectedDictionaryIdentifier.Identifier.Text;
        var expectedKeyText = expectedKeyArgument.GetText().ToString();
        if (ifStatementSyntax.Statement is BlockSyntax block)
        {
            if (block.Statements.Count > 1)
            {
                return ProcessIfWithMultilineBody(editor, ifStatementSyntax, invocationExpression, expectedDictionaryIdentifierNameText, expectedKeyText);
            }

            return ProcessIfWithSingleLineBody(editor, ifStatementSyntax, invocationExpression, expectedDictionaryIdentifierNameText, block.Statements[0]);
        }

        return ProcessIfWithSingleLineBody(editor, ifStatementSyntax, invocationExpression, expectedDictionaryIdentifierNameText, ifStatementSyntax.Statement);
    }

    private static Document ProcessIfWithMultilineBody(
        DocumentEditor editor,
        IfStatementSyntax ifStatement,
        InvocationExpressionSyntax invocationExpression,
        string dictionaryName,
        string expectedKeyText)
    {
        // replaces ContainsKey by TryGetValue
        var newInvocationExpression = CreateTryGetValueInvocationExpression(invocationExpression);
        var newIfStatement = ifStatement.ReplaceNode(invocationExpression, newInvocationExpression);

        // replaces dictionary item access expressions by 'retrievedValue' in 'if' body
        var dictionaryItemAccessExpressionsToReplace =
                        newIfStatement.Statement
                        .DescendantNodes()
                        .Where(w => w is ElementAccessExpressionSyntax elementAccessExpr &&
                                CheckIndentifierNameAndFirstArgument(
                                    elementAccessExpr.Expression,
                                    elementAccessExpr.ArgumentList,
                                    dictionaryName,
                                    expectedKeyText));

        var newIdentifierName = SyntaxFactory.IdentifierName("retrievedValue");
        newIfStatement = newIfStatement.ReplaceNodes(dictionaryItemAccessExpressionsToReplace, (_, _) => newIdentifierName);
        editor.ReplaceNode(ifStatement, newIfStatement);

        return editor.GetChangedDocument();
    }

    private static Document CreateExpressionStatementAndReplaceOldNode(ExpressionSyntax newExpressionSyntax, SyntaxNode oldSyntaxNode, DocumentEditor editor)
    {
        var newExpression = SyntaxFactory.ExpressionStatement(newExpressionSyntax, SyntaxFactory.ParseToken(";")).WithTriviaFrom(oldSyntaxNode);
        editor.ReplaceNode(oldSyntaxNode, newExpression);
        return editor.GetChangedDocument();
    }

    private static Document ProcessIfWithSingleLineBody(
        DocumentEditor editor,
        SyntaxNode ifStatement,
        InvocationExpressionSyntax invocationExpression,
        string dictionaryName,
        StatementSyntax statementSyntax)
    {
        ExpressionSyntax expression = ((ExpressionStatementSyntax)statementSyntax).Expression;

        if (expression is AssignmentExpressionSyntax assignmentExpression)
        {
            if (assignmentExpression!.Left is ElementAccessExpressionSyntax elementAccessExpr &&
            elementAccessExpr.Expression.IdentifierNameEquals(dictionaryName))
            {
                // replaces if with ContainsKey by TryAdd
                ExpressionSyntax newExpr = ((MemberAccessExpressionSyntax)invocationExpression.Expression)
                                        .WithName(_tryAddMethod).WithAdditionalAnnotations(Formatter.Annotation);

                var newArguments = invocationExpression.ArgumentList.AddArguments(SyntaxFactory.Argument(assignmentExpression.Right));
                var newInvocationExpr = invocationExpression.WithExpression(newExpr).WithArgumentList(newArguments);

                return CreateExpressionStatementAndReplaceOldNode(newInvocationExpr, ifStatement, editor);
            }

            if (assignmentExpression!.Right is ElementAccessExpressionSyntax elementAccessExprRight &&
            elementAccessExprRight.Expression.IdentifierNameEquals(dictionaryName))
            {
                // replaces if with ContainsKey by TryGetValue
                var newTryGetValueInvocationExpr = CreateTryGetValueInvocationExpression(invocationExpression);

                editor.ReplaceNode(invocationExpression, newTryGetValueInvocationExpr);

                var identifierName = SyntaxFactory.IdentifierName("retrievedValue");
                editor.ReplaceNode(assignmentExpression.Right, identifierName);
                return editor.GetChangedDocument();
            }

            return CreateExpressionStatementAndReplaceOldNode(assignmentExpression, ifStatement, editor);
        }

        var removeOrAddOrTryGetSyntaxNode = expression
                                .DescendantNodesAndSelf()
                                .First(w => w.NodeHasSpecifiedMethod(editor.SemanticModel, _methodsFullNamesToBeHanled));

        return CheckInvocation(removeOrAddOrTryGetSyntaxNode, editor, ifStatement);
    }

    private static Document CheckInvocation(
        SyntaxNode node,
        DocumentEditor editor,
        SyntaxNode ifStatement)
    {
        if (node.NodeHasSpecifiedMethod(editor.SemanticModel, _removeAndTryGetMethodFullNames))
        {
            // replaces "if" containing "ContainsKey" with "Remove" or "TryGetValue"
            return CreateExpressionStatementAndReplaceOldNode((InvocationExpressionSyntax)node, ifStatement, editor);
        }

        // replaces "if" containing "ContainsKey" with "TryAdd"
        var foundInvocation = (InvocationExpressionSyntax)node;
        ExpressionSyntax newExpr = ((MemberAccessExpressionSyntax)foundInvocation.Expression)
                                    .WithName(_tryAddMethod).WithAdditionalAnnotations(Formatter.Annotation);
        var newInvocationExpr = foundInvocation.WithExpression(newExpr);

        return CreateExpressionStatementAndReplaceOldNode(newInvocationExpr, ifStatement, editor);
    }

    private static InvocationExpressionSyntax CreateTryGetValueInvocationExpression(InvocationExpressionSyntax oldInvocation)
    {
        ExpressionSyntax newExpr = ((MemberAccessExpressionSyntax)oldInvocation.Expression)
                                        .WithName(_tryGetValueMethod).WithAdditionalAnnotations(Formatter.Annotation);

        var newArgument = SyntaxFactory.DeclarationExpression(
            SyntaxFactory.IdentifierName(SyntaxFactory.ParseToken("var")).WithTrailingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(" "))),
            SyntaxFactory.SingleVariableDesignation(SyntaxFactory.ParseToken("retrievedValue")));

        var newArguments = oldInvocation.ArgumentList.AddArguments(SyntaxFactory.Argument(null, SyntaxFactory.ParseToken("out"), newArgument));

        return oldInvocation.WithExpression(newExpr).WithArgumentList(newArguments);
    }

    private static bool CheckIndentifierNameAndFirstArgument(ExpressionSyntax expression, BaseArgumentListSyntax argumentListSyntax, string expectedIndetifierName, string expectedFirstArgument)
    {
        return expression.IdentifierNameEquals(expectedIndetifierName) &&
                        argumentListSyntax.Arguments[0].GetText().ToString() == expectedFirstArgument;
    }
}
