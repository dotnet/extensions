// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
using Microsoft.Extensions.ExtraAnalyzers.Utilities;

namespace Microsoft.Extensions.ExtraAnalyzers;

/// <summary>
/// Removes excessive <see cref="ISet{T}"/> lookups.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UsingExcessiveSetLookupFixer))]
[Shared]
public sealed class UsingExcessiveSetLookupFixer : CodeFixProvider
{
    private const string ContainsMethodName = "Contains";
    private const string AddMethodName = "Add";
    private const string RemoveMethodName = "Remove";

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagDescriptors.UsingExcessiveSetLookup.Id);

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc/>
    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        context.RegisterCodeFix(
               CodeAction.Create(
                   title: Resources.UsingExcessiveSetLookupTitle,
                   createChangedDocument: cancellationToken => ApplyFixAsync(context.Document, context.Diagnostics.First().Location, cancellationToken),
                   equivalenceKey: nameof(Resources.UsingExcessiveSetLookupTitle)),
               context.Diagnostics);

        return Task.CompletedTask;
    }

    private static async Task<Document> ApplyFixAsync(Document document, Location diagnosticLocation, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var invocationExpression = (InvocationExpressionSyntax)editor.OriginalRoot.FindNode(diagnosticLocation.SourceSpan);
        var memberAccessExpression = (MemberAccessExpressionSyntax)invocationExpression.Expression;
        var methodName = memberAccessExpression.Name.GetText().ToString();
        if (methodName == ContainsMethodName)
        {
            var ifStatementSyntax = (IfStatementSyntax)invocationExpression.GetFirstAncestorOfSyntaxKind(SyntaxKind.IfStatement)!;

            if (ifStatementSyntax.Statement is BlockSyntax block)
            {
                if (block.Statements.Count > 1)
                {
                    var newInvocation = ((ExpressionStatementSyntax)block.Statements[0]).Expression.WithTriviaFrom(invocationExpression);
                    editor.ReplaceNode(invocationExpression, newInvocation);
                    var newBlock = block.WithStatements(block.Statements.RemoveAt(0));
                    editor.ReplaceNode(block, newBlock);
                }
                else
                {
                    editor.ReplaceNode(ifStatementSyntax, block.Statements[0].WithTriviaFrom(ifStatementSyntax));
                }

                return editor.GetChangedDocument();
            }

            editor.ReplaceNode(ifStatementSyntax, ifStatementSyntax.Statement.WithTriviaFrom(ifStatementSyntax));
            return editor.GetChangedDocument();
        }

        if (methodName == AddMethodName || methodName == RemoveMethodName)
        {
            var nodeToRemove = invocationExpression.GetFirstAncestorOfSyntaxKind(SyntaxKind.ExpressionStatement);
            var blockStatements = ((BlockSyntax)nodeToRemove!.Parent!).Statements;
            int ifStatementIndex = blockStatements.IndexOf((StatementSyntax)nodeToRemove) - 1;
            var ifStatement = (IfStatementSyntax)blockStatements[ifStatementIndex];

            var newNode = SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, invocationExpression);
            editor.ReplaceNode(ifStatement.Condition, newNode);
            editor.RemoveNode(nodeToRemove!);
            return editor.GetChangedDocument();
        }

        return document;
    }
}
