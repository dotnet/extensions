// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.Extensions.ExtraAnalyzers;

/// <summary>
/// Replace explicit throw with static method.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeExeTypesInternalFixer))]
[Shared]
public sealed class MakeExeTypesInternalFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagDescriptors.MakeExeTypesInternal.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var node = root?.FindNode(context.Span);
        if (node != null)
        {
            var action = CodeAction.Create(Resources.MakeTypeInternal, c => MakeInternalAsync(context.Document, node, context.CancellationToken), nameof(MakeExeTypesInternalFixer));
            context.RegisterCodeFix(action, context.Diagnostics);
        }
    }

    private static async Task<Document> MakeInternalAsync(Document document, SyntaxNode decl, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        editor.SetAccessibility(decl, Accessibility.Internal);
        return editor.GetChangedDocument();
    }
}
