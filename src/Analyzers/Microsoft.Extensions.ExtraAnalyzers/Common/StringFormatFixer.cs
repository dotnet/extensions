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
using Microsoft.Extensions.ExtraAnalyzers.Utilities;

namespace Microsoft.Extensions.ExtraAnalyzers;

/// <summary>
/// Replace string.Format usage with Microsoft.Extensions.Text.CompositeFormat.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(StringFormatFixer))]
[Shared]
public sealed class StringFormatFixer : CodeFixProvider
{
    private const string TargetClass = "CompositeFormat";
    private const string TargetMethod = "Format";
    private const string VariableName = "_sf";
    private const int ArgumentsToSkip = 2;
    private static readonly IdentifierNameSyntax _textNamespace = SyntaxFactory.IdentifierName("Microsoft.Extensions.Text");

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagDescriptors.StringFormat.Id);

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc/>
    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostics = context.Diagnostics.First();
        context.RegisterCodeFix(
               CodeAction.Create(
                   title: Resources.StringFormatTitle,
                   createChangedDocument: cancellationToken => ApplyFixAsync(context.Document, diagnostics.Location, diagnostics.Properties, cancellationToken),
                   equivalenceKey: nameof(Resources.StringFormatTitle)),
               context.Diagnostics);

        return Task.CompletedTask;
    }

    private static async Task<Document> ApplyFixAsync(Document document, Location diagnosticLocation, IReadOnlyDictionary<string, string?> properties, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        if (editor.OriginalRoot.FindNode(diagnosticLocation.SourceSpan) is InvocationExpressionSyntax expression)
        {
            var classDeclaration = GetTypeDeclaration(expression);
            if (classDeclaration != null)
            {
                var (format, argList) = GetFormatAndArguments(editor, expression);
                var formatKind = format.ChildNodes().First().Kind();

                if (formatKind is SyntaxKind.StringLiteralExpression)
                {
                    var (identifier, field) = GetFieldDeclaration(editor, classDeclaration, format);
                    var invocation = properties.ContainsKey("StringFormat")
                        ? CreateInvocationExpression(editor, identifier, argList.Arguments, expression)
                        : GetStringBuilderExpression(editor, identifier, argList.Arguments, expression);
                    ApplyChanges(editor, expression, invocation, classDeclaration, field);
                }
            }
        }

        return editor.GetChangedDocument();
    }

    private static TypeDeclarationSyntax? GetTypeDeclaration(SyntaxNode node)
    {
        return node.FirstAncestorOrSelf<TypeDeclarationSyntax>(n => n.IsKind(SyntaxKind.ClassDeclaration) || n.IsKind(SyntaxKind.StructDeclaration));
    }

    private static (string identifier, FieldDeclarationSyntax? field) GetFieldDeclaration(SyntaxEditor editor, SyntaxNode classDeclaration, SyntaxNode format)
    {
        var members = classDeclaration.DescendantNodes().OfType<FieldDeclarationSyntax>();
        int numberOfMembers = 1;

        var strExp = format.ToString();

        var arguments = SyntaxFactory.Argument(SyntaxFactory.ParseExpression(strExp));
        HashSet<string> fields = new HashSet<string>();

        foreach (var member in members)
        {
            var fieldName = member.DescendantNodes().OfType<VariableDeclaratorSyntax>().First().Identifier.ToString();
            _ = fields.Add(fieldName);

            if (member.Declaration.Type.ToString() == "CompositeFormat")
            {
                if (member.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().First().ArgumentList!.Arguments.First().ToString() == strExp)
                {
                    return (member.DescendantNodes().OfType<VariableDeclaratorSyntax>().First().Identifier.ToString(), null);
                }

                numberOfMembers++;
            }
        }

        string variableName;
        do
        {
            variableName = $"{VariableName}{numberOfMembers}";
            numberOfMembers++;
        }
        while (!IsFieldNameAvailable(fields, variableName));

        return (variableName, editor.Generator.FieldDeclaration(
                        variableName,
                        SyntaxFactory.ParseTypeName(TargetClass),
                        Accessibility.Private,
                        DeclarationModifiers.Static | DeclarationModifiers.ReadOnly,
                        SyntaxFactory.ObjectCreationExpression(
                                SyntaxFactory.Token(SyntaxKind.NewKeyword),
                                SyntaxFactory.IdentifierName(TargetClass),
                                SyntaxFactory.ArgumentList().AddArguments(arguments),
                                null)) as FieldDeclarationSyntax);
    }

    private static bool IsFieldNameAvailable(ICollection<string> fields, string field)
    {
        return !fields.Contains(field);
    }

    private static (ArgumentSyntax argument, ArgumentListSyntax argumentList) GetFormatAndArguments(DocumentEditor editor, InvocationExpressionSyntax invocation)
    {
        var arguments = invocation.ArgumentList.Arguments;
        var first = arguments[0];
        var typeInfo = editor.SemanticModel.GetTypeInfo(first.ChildNodes().First());
        SeparatedSyntaxList<ArgumentSyntax> separatedList;
        if (arguments.Count > 1 && typeInfo.Type!.AllInterfaces.Any(i => i.MetadataName == "IFormatProvider"))
        {
            separatedList = SyntaxFactory.SingletonSeparatedList(first).AddRange(arguments.Skip(ArgumentsToSkip));
            return (arguments[1], SyntaxFactory.ArgumentList(separatedList));
        }

        var nullArgument = SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));
        separatedList = SyntaxFactory.SingletonSeparatedList(nullArgument).AddRange(arguments.Skip(1));
        return (first, SyntaxFactory.ArgumentList(separatedList));
    }

    private static SyntaxNode CreateInvocationExpression(SyntaxEditor editor, string identifierName, IEnumerable<SyntaxNode> arguments, SyntaxNode invocation)
    {
        var gen = editor.Generator;
        var identifier = gen.IdentifierName(identifierName);
        var memberAccessExpression = gen.MemberAccessExpression(identifier, TargetMethod);
        return gen.InvocationExpression(memberAccessExpression, arguments).WithTriviaFrom(invocation);
    }

    private static void ApplyChanges(SyntaxEditor editor, SyntaxNode oldInvocation, SyntaxNode newInvocation, SyntaxNode classDeclaration, SyntaxNode? field)
    {
        if (field != null)
        {
            editor.AddMember(classDeclaration, field);
        }

        editor.ReplaceNode(oldInvocation, newInvocation);
        editor.TryAddUsingDirective(_textNamespace);
    }

    private static SyntaxNode GetStringBuilderExpression(SyntaxEditor editor, string identifierName, IEnumerable<ArgumentSyntax> arguments, InvocationExpressionSyntax invocation)
    {
        var gen = editor.Generator;
        var identifier = gen.IdentifierName(identifierName);
        var memberAccessExpression = gen.Argument(identifier);
        var list = SyntaxFactory.SingletonSeparatedList(memberAccessExpression).AddRange(arguments);
        return invocation.WithArgumentList(SyntaxFactory.ArgumentList(list));
    }
}
