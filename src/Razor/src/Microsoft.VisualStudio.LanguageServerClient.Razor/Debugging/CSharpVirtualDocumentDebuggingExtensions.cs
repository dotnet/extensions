// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.Debugging
{
    internal static class CSharpVirtualDocumentDebuggingExtensions
    {
        // This method should only ever be used at debug language service time to ensure two super large assumptions:
        //      1. The UI thread will be blocked meaning no more user input to influence a Razor C# virtual document in a debug meaningful way.
        //      2. The C# virtual document has been synchronized to the "latest" version that's known by Razor based on the Razor content.
        // With both of these assumptions in place we can try and lookup a corresponding C# document in the workspace prior to attempting to
        // re-parse C# content to get a syntax tree.
        public static async Task<SyntaxTree> GetCSharpSyntaxTreeAsync(this CSharpVirtualDocumentSnapshot virtualDocument, CodeAnalysis.Workspace? workspace, CancellationToken cancellationToken)
        {
            if (virtualDocument is null)
            {
                throw new ArgumentNullException(nameof(virtualDocument));
            }

            SyntaxTree? syntaxTree = null;

            if (workspace == null)
            {
                // No workspace means we have to fallback to C# syntax tree resoltuion from snapshot.
                syntaxTree = CreateSyntaxTreeFromSnapshot(virtualDocument.Snapshot, cancellationToken);
                return syntaxTree;
            }

            var solution = workspace.CurrentSolution;
            var filePath = virtualDocument.Uri.GetAbsoluteOrUNCPath().Replace('/', '\\');
            var documentIds = solution.GetDocumentIdsWithFilePath(filePath);

            if (documentIds.Length > 0)
            {
                Debug.Assert(documentIds.Length == 1, "Should not have more then one backing C# Razor document in the workspace.");

                var documentId = documentIds[0];
                var document = solution.GetDocument(documentId);

                syntaxTree = await document!.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            }

            if (syntaxTree == null)
            {
                // Couldn't find the document in the workspace OR the version in the workspace couldn't have its syntax tree computed.
                syntaxTree = CreateSyntaxTreeFromSnapshot(virtualDocument.Snapshot, cancellationToken);
            }

            return syntaxTree;

            static SyntaxTree CreateSyntaxTreeFromSnapshot(ITextSnapshot snapshot, CancellationToken cancellationToken)
            {
                var sourceText = snapshot.AsText();
                var syntaxTree = CSharpSyntaxTree.ParseText(sourceText, cancellationToken: cancellationToken);
                return syntaxTree;
            }
        }
    }
}
