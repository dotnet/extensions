// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.Extensions.ExtraAnalyzers.FixAllProviders;

public sealed class SequentialFixAllCodeAction : CodeAction
{
    public override string Title { get; }

    public SequentialFixAllCodeAction(
        string fixAllTitle,
        FixAllContext context,
        ConcurrentDictionary<DocumentId, ImmutableArray<Diagnostic>> diagsToFixGroupedByDocId,
        ImmutableArray<DocumentId> inScopeDocumentIds)
    {
        Title = fixAllTitle;
        _context = context;
        _sequentialFixer = (context.CodeFixProvider as ISequentialFixer)!;
        _diagsToFixGroupedByDocId = diagsToFixGroupedByDocId;
        _solution = context.Solution;
        _inScopeDocumentIds = inScopeDocumentIds;
    }

    protected override async Task<Solution?> GetChangedSolutionAsync(CancellationToken cancellationToken)
    {
        do
        {
            // Apply CodeFix for all in scope documents and update solution with changed documents
            var fixDiagTasks = _diagsToFixGroupedByDocId.Select(diagsInDoc =>
            {
                var docId = diagsInDoc.Key;
                return ApplyDiagnosticFixesAndGetDocumentRootAsync(docId)
                      .ContinueWith(completeFixDiagTask =>
                      {
                          lock (_solution)
                          {
                              _solution = _solution.WithDocumentSyntaxRoot(docId, completeFixDiagTask.Result);
                          }
                      }, TaskScheduler.Default);
            });
            await Task.WhenAll(fixDiagTasks).ConfigureAwait(continueOnCapturedContext: false);

            // Clear the internal map of document grouped diagnostics
            _diagsToFixGroupedByDocId.Clear();

            // Recompute diagnostics from all in scope documents
            var recomputeDiagTasks = _inScopeDocumentIds.Select(docId =>
            {
                var document = _solution.GetDocument(docId);
                return _context
                        .GetDocumentDiagnosticsAsync(document!)
                        .ContinueWith(completeRecomputeTask =>
                        {
                            var newDiagnostics = completeRecomputeTask.Result;
                            if (newDiagnostics.Any())
                            {
                                _ = _diagsToFixGroupedByDocId.TryAdd(docId, newDiagnostics);
                            }
                        }, TaskScheduler.Default);
            });
            await Task.WhenAll(recomputeDiagTasks).ConfigureAwait(continueOnCapturedContext: false);
        }
        while (!_diagsToFixGroupedByDocId.IsEmpty);

        return _solution;
    }

    private readonly FixAllContext _context;
    private readonly ISequentialFixer _sequentialFixer;
    private readonly ConcurrentDictionary<DocumentId, ImmutableArray<Diagnostic>> _diagsToFixGroupedByDocId;
    private Solution _solution;
    private ImmutableArray<DocumentId> _inScopeDocumentIds;

    private async Task<SyntaxNode> ApplyDiagnosticFixesAndGetDocumentRootAsync(DocumentId docIdToFix)
    {
        var document = _solution.GetDocument(docIdToFix);
        var docRoot = await document!.GetSyntaxRootAsync().ConfigureAwait(continueOnCapturedContext: false);
        var diagnostics = _diagsToFixGroupedByDocId[docIdToFix];
        var nodeToDiagsMap = new Dictionary<SyntaxNode, Diagnostic>();

        foreach (var d in diagnostics)
        {
            nodeToDiagsMap.Add(_sequentialFixer.GetFixableSyntaxNodeFromDiagnostic(docRoot!, d), d);
        }

        return docRoot!.ReplaceNodes(nodeToDiagsMap.Keys.ToArray(),
            (oldNode, _) => _sequentialFixer.ApplyDiagnosticFixToSyntaxNode(oldNode, nodeToDiagsMap[oldNode]));
    }
}
