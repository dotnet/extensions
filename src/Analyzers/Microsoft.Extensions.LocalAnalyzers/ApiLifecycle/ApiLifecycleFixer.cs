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

namespace Microsoft.Extensions.LocalAnalyzers.ApiLifecycle;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ApiLifecycleFixer))]
[Shared]
public sealed class ApiLifecycleFixer : CodeFixProvider
{
    private const string ExperimentalAttributeName = "Experimental";
    private const string ExperimentalAttributeNamespace = "System.Diagnostics.CodeAnalysis";

    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
        DiagDescriptors.NewSymbolsMustBeMarkedExperimental.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var location = context.Diagnostics.First().Location;

        context.RegisterCodeFix(
                CodeAction.Create(
                    title: Resources.AnnotateExperimentalApi,
                    createChangedDocument: cancelToken => AddExperimentalAttributeAsync(context, location, cancelToken),
                    equivalenceKey: nameof(Resources.AnnotateExperimentalApi)),
                context.Diagnostics);

        return Task.CompletedTask;
    }

    private static async Task<Document> AddExperimentalAttributeAsync(CodeFixContext context, Location argumentToChangeLocation,
        CancellationToken cancelToken)
    {
        var document = context.Document;
        var editor = await DocumentEditor.CreateAsync(document, cancelToken).ConfigureAwait(false);
        var member = (MemberDeclarationSyntax)editor.OriginalRoot.FindNode(argumentToChangeLocation.SourceSpan)!;
        var lead = member.GetLeadingTrivia();
        var attributeLists = member!.AttributeLists;

        var withoutTriviaMember = member.WithoutLeadingTrivia();

        attributeLists = attributeLists.Add(SyntaxFactory.AttributeList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Attribute(
                        SyntaxFactory.IdentifierName(ExperimentalAttributeName)))));

        if (editor.GetChangedRoot() is CompilationUnitSyntax mutableRoot)
        {
            mutableRoot = mutableRoot.ReplaceNode(member, withoutTriviaMember.WithAttributeLists(attributeLists).WithLeadingTrivia(lead));

            if (!mutableRoot.Usings.Any(@using => @using.Name.ToString() == ExperimentalAttributeNamespace))
            {
                mutableRoot = mutableRoot.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName(ExperimentalAttributeNamespace)));
            }

            editor.ReplaceNode(editor.OriginalRoot, mutableRoot);
        }

        return editor.GetChangedDocument();
    }
}
