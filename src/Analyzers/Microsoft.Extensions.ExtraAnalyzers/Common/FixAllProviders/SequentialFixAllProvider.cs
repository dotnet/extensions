// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.Extensions.ExtraAnalyzers.FixAllProviders;

public sealed class SequentialFixAllProvider : FixAllProvider
{
    public const string DocumentScopeStr = "document";
    public const string ProjectScopeStr = "project";

    public static SequentialFixAllProvider GetInstance(ISequentialFixer _) => _instance;

    public override async Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
    {
        (var fixAllTitle, var documentsToFix) = GetFixAllTitleAndDocumentsToFix(fixAllContext);
        ConcurrentDictionary<DocumentId, ImmutableArray<Diagnostic>> diagsToFixGroupedByDocId = new();

        var computeDiagTasks = documentsToFix.Select(docId =>
            fixAllContext.GetDocumentDiagnosticsAsync(fixAllContext.Solution.GetDocument(docId)!)
                   .ContinueWith((completeComputeDiagsTask) =>
                   {
                       var diagsToFixInDocument = completeComputeDiagsTask.Result;
                       if (diagsToFixInDocument.Any())
                       {
                           _ = diagsToFixGroupedByDocId.TryAdd(docId, diagsToFixInDocument);
                       }
                   }, TaskScheduler.Default));

        await Task.WhenAll(computeDiagTasks).ConfigureAwait(continueOnCapturedContext: false);

        return new SequentialFixAllCodeAction(
            fixAllTitle,
            fixAllContext,
            diagsToFixGroupedByDocId,
            documentsToFix);
    }

    private static readonly SequentialFixAllProvider _instance = new();

    internal static ImmutableArray<DocumentId> GetAllDocumentsInSolution(Solution solution)
    {
        var allDocsInSolution = ImmutableArray<DocumentId>.Empty;

        foreach (var project in solution.Projects)
        {
            allDocsInSolution = allDocsInSolution.AddRange(project!.Documents.Select(d => d.Id));
        }

        return allDocsInSolution;
    }

    internal static (string fixAllTitle, ImmutableArray<DocumentId> documentsToFix) GetFixAllTitleAndDocumentsToFix(FixAllContext context)
    {
        var fixAllTitle = string.Empty;
        var documentsToFix = ImmutableArray<DocumentId>.Empty;

        switch (context.Scope)
        {
            case FixAllScope.Document:
                fixAllTitle = string.Format(
                    CultureInfo.InvariantCulture,
                    Resources.SequentialFixAllFormat,
                    DocumentScopeStr,
                    context.Document!.Name);
                documentsToFix = ImmutableArray.Create(context.Document!.Id);
                break;
            case FixAllScope.Project:
                fixAllTitle = string.Format(
                    CultureInfo.InvariantCulture,
                    Resources.SequentialFixAllFormat,
                    ProjectScopeStr,
                    context.Project!.Name);
                documentsToFix = context.Project.Documents.Select(d => d.Id).ToImmutableArray();
                break;
            case FixAllScope.Solution:
                fixAllTitle = Resources.SequentialFixAllInSolution;
                documentsToFix = GetAllDocumentsInSolution(context.Solution);
                break;
        }

        return (fixAllTitle, documentsToFix);
    }
}
