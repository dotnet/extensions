// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.AspNetCore.Razor.Language.Components;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Refactoring
{
    internal class RazorComponentRenameEndpoint : IRenameHandler
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly DocumentResolver _documentResolver;
        private readonly ProjectSnapshotManager _projectSnapshotManager;
        private readonly RazorComponentSearchEngine _componentSearchEngine;
        private readonly LanguageServerFeatureOptions _languageServerFeatureOptions;
        private RenameCapability _capability;

        public RazorComponentRenameEndpoint(
            ForegroundDispatcher foregroundDispatcher,
            DocumentResolver documentResolver,
            RazorComponentSearchEngine componentSearchEngine,
            ProjectSnapshotManagerAccessor projectSnapshotManagerAccessor,
            LanguageServerFeatureOptions languageServerFeatureOptions)
        {
            _foregroundDispatcher = foregroundDispatcher ?? throw new ArgumentNullException(nameof(foregroundDispatcher));
            _documentResolver = documentResolver ?? throw new ArgumentNullException(nameof(documentResolver));
            _componentSearchEngine = componentSearchEngine ?? throw new ArgumentNullException(nameof(componentSearchEngine));
            _projectSnapshotManager = projectSnapshotManagerAccessor?.Instance ?? throw new ArgumentNullException(nameof(projectSnapshotManagerAccessor));
            _languageServerFeatureOptions = languageServerFeatureOptions ?? throw new ArgumentNullException(nameof(languageServerFeatureOptions));
        }

        public RenameRegistrationOptions GetRegistrationOptions()
        {
            return new RenameRegistrationOptions
            {
                PrepareProvider = false,
                DocumentSelector = RazorDefaults.Selector,
            };
        }

        public async Task<WorkspaceEdit> Handle(RenameParams request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!_languageServerFeatureOptions.SupportsFileManipulation)
            {
                // If we cannot rename a component file then return early indicating a failure to rename anything.
                return null;
            }

            var requestDocumentSnapshot = await Task.Factory.StartNew(() =>
            {
                var path = request.TextDocument.Uri.GetAbsoluteOrUNCPath();
                _documentResolver.TryResolveDocument(path, out var documentSnapshot);
                return documentSnapshot;
            }, cancellationToken, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler).ConfigureAwait(false);

            if (requestDocumentSnapshot is null)
            {
                return null;
            }

            if (!FileKinds.IsComponent(requestDocumentSnapshot.FileKind))
            {
                return null;
            }

            var codeDocument = await requestDocumentSnapshot.GetGeneratedOutputAsync().ConfigureAwait(false);
            if (codeDocument.IsUnsupported())
            {
                return null;
            }

            var originTagHelpers = await GetOriginTagHelpersAsync(requestDocumentSnapshot, codeDocument, request.Position).ConfigureAwait(false);
            if (originTagHelpers is null || originTagHelpers.Count == 0)
            {
                return null;
            }

            var originComponentDocumentSnapshot = await _componentSearchEngine.TryLocateComponentAsync(originTagHelpers.First()).ConfigureAwait(false);
            if (originComponentDocumentSnapshot is null)
            {
                return null;
            }

            var newPath = MakeNewPath(originComponentDocumentSnapshot.FilePath, request.NewName);
            if (File.Exists(newPath))
            {
                return null;
            }

            var documentChanges = new List<WorkspaceEditDocumentChange>();
            AddFileRenameForComponent(documentChanges, originComponentDocumentSnapshot, newPath);
            AddEditsForCodeDocument(documentChanges, originTagHelpers, request.NewName, request.TextDocument.Uri, codeDocument);

            var documentSnapshots = await GetAllDocumentSnapshotsAsync(requestDocumentSnapshot, cancellationToken).ConfigureAwait(false);
            foreach (var documentSnapshot in documentSnapshots)
            {
                await AddEditsForCodeDocumentAsync(documentChanges, originTagHelpers, request.NewName, documentSnapshot, cancellationToken);
            }

            return new WorkspaceEdit
            {
                DocumentChanges = documentChanges,
            };
        }

        private async Task<List<DocumentSnapshot>> GetAllDocumentSnapshotsAsync(DocumentSnapshot skipDocumentSnapshot, CancellationToken cancellationToken)
        {
            return await Task.Factory.StartNew(() =>
            {
                var documentSnapshots = new List<DocumentSnapshot>();
                var documentPaths = new HashSet<string>();
                foreach (var project in _projectSnapshotManager.Projects)
                {
                    foreach (var documentPath in project.DocumentFilePaths)
                    {
                        // We've already added refactoring edits for our document snapshot
                        if (string.Equals(documentPath, skipDocumentSnapshot.FilePath, FilePathComparison.Instance))
                        {
                            continue;
                        }

                        // Don't add duplicates between projects
                        if (documentPaths.Contains(documentPath))
                        {
                            continue;
                        }

                        // Add to the list and add the path to the set
                        _documentResolver.TryResolveDocument(documentPath, out var documentSnapshot);
                        documentSnapshots.Add(documentSnapshot);
                        documentPaths.Add(documentPath);
                    }
                }
                return documentSnapshots;
            }, cancellationToken, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler).ConfigureAwait(false);
        }

        public void AddFileRenameForComponent(List<WorkspaceEditDocumentChange> documentChanges, DocumentSnapshot documentSnapshot, string newPath)
        {
            var oldUri = new UriBuilder
            {
                Path = documentSnapshot.FilePath,
                Host = string.Empty,
                Scheme = Uri.UriSchemeFile,
            }.Uri;
            var newUri = new UriBuilder
            {
                Path = newPath,
                Host = string.Empty,
                Scheme = Uri.UriSchemeFile,
            }.Uri;

            documentChanges.Add(new WorkspaceEditDocumentChange(new RenameFile
            {
                OldUri = oldUri.ToString(),
                NewUri = newUri.ToString(),
            }));
        }

        private static string MakeNewPath(string originalPath, string newName)
        {
            var newFileName = $"{newName}{Path.GetExtension(originalPath)}";
            var newPath = Path.Combine(Path.GetDirectoryName(originalPath), newFileName);
            return newPath;
        }

        public async Task AddEditsForCodeDocumentAsync(
            List<WorkspaceEditDocumentChange> documentChanges,
            IReadOnlyList<TagHelperDescriptor> originTagHelpers,
            string newName,
            DocumentSnapshot documentSnapshot,
            CancellationToken cancellationToken)
        {
            if (documentSnapshot is null)
            {
                return;
            }

            var codeDocument = await documentSnapshot.GetGeneratedOutputAsync().ConfigureAwait(false);
            if (codeDocument.IsUnsupported())
            {
                return;
            }

            if (!FileKinds.IsComponent(codeDocument.GetFileKind()))
            {
                return;
            }

            var uri = new UriBuilder
            {
                Path = documentSnapshot.FilePath,
                Host = string.Empty,
                Scheme = Uri.UriSchemeFile,
            }.Uri;

            AddEditsForCodeDocument(documentChanges, originTagHelpers, newName, uri, codeDocument);
        }

        public void AddEditsForCodeDocument(
            List<WorkspaceEditDocumentChange> documentChanges,
            IReadOnlyList<TagHelperDescriptor> originTagHelpers,
            string newName,
            DocumentUri uri,
            RazorCodeDocument codeDocument)
        {
            var documentIdentifier = new VersionedTextDocumentIdentifier { Uri = uri };
            var tagHelperElements = codeDocument.GetSyntaxTree().Root
                .DescendantNodes()
                .Where(n => n.Kind == SyntaxKind.MarkupTagHelperElement)
                .OfType<MarkupTagHelperElementSyntax>();

            for (var i = 0; i < originTagHelpers.Count; i++)
            {
                var editedName = newName;
                var originTagHelper = originTagHelpers[i];
                if (originTagHelper?.IsComponentFullyQualifiedNameMatch() == true)
                {
                    // Fully qualified binding, our "new name" needs to be fully qualified.
                    if (!DefaultRazorTagHelperBinderPhase.ComponentDirectiveVisitor.TrySplitNamespaceAndType(originTagHelper.Name, out var namespaceSpan, out _))
                    {
                        return;
                    }

                    var namespaceString = originTagHelper.Name.Substring(namespaceSpan.Start, namespaceSpan.Length);

                    // The origin TagHelper was fully qualified so any fully qualified rename locations we find will need a fully qualified renamed edit.
                    editedName = $"{namespaceString}.{newName}";
                }

                foreach (var node in tagHelperElements)
                {
                    if (node is MarkupTagHelperElementSyntax tagHelperElement && BindingContainsTagHelper(originTagHelper, tagHelperElement.TagHelperInfo.BindingResult))
                    {
                        documentChanges.Add(new WorkspaceEditDocumentChange(new TextDocumentEdit
                        {
                            TextDocument = documentIdentifier,
                            Edits = CreateEditsForMarkupTagHelperElement(tagHelperElement, codeDocument, editedName)
                        }));
                    }
                }
            }
        }

        public List<TextEdit> CreateEditsForMarkupTagHelperElement(MarkupTagHelperElementSyntax element, RazorCodeDocument codeDocument, string newName)
        {
            var edits = new List<TextEdit>
            {
                new TextEdit()
                {
                    Range = element.StartTag.Name.GetRange(codeDocument.Source),
                    NewText = newName,
                },
            };
            if (element.EndTag != null)
            {
                edits.Add(new TextEdit()
                {
                    Range = element.EndTag.Name.GetRange(codeDocument.Source),
                    NewText = newName,
                });
            }
            return edits;
        }

        private static bool BindingContainsTagHelper(TagHelperDescriptor tagHelper, TagHelperBinding potentialBinding) =>
            potentialBinding.Descriptors.Any(descriptor => descriptor.Equals(tagHelper));

        private async Task<IReadOnlyList<TagHelperDescriptor>> GetOriginTagHelpersAsync(DocumentSnapshot documentSnapshot, RazorCodeDocument codeDocument, Position position)
        {
            var sourceText = await documentSnapshot.GetTextAsync().ConfigureAwait(false);
            var linePosition = new LinePosition((int)position.Line, (int)position.Character);
            var hostDocumentIndex = sourceText.Lines.GetPosition(linePosition);
            var location = new SourceLocation(hostDocumentIndex, (int)position.Line, (int)position.Character);

            var change = new SourceChange(location.AbsoluteIndex, length: 0, newText: string.Empty);
            var syntaxTree = codeDocument.GetSyntaxTree();
            if (syntaxTree?.Root is null)
            {
                return null;
            }

            var owner = syntaxTree.Root.LocateOwner(change);
            if (owner == null)
            {
                Debug.Fail("Owner should never be null.");
                return null;
            }

            var node = owner.Ancestors().FirstOrDefault(n => n.Kind == SyntaxKind.MarkupTagHelperStartTag);
            if (node == null || !(node is MarkupTagHelperStartTagSyntax tagHelperStartTag))
            {
                return null;
            }

            // Ensure the rename action was invoked on the component name
            // instead of a component parameter. This serves as an issue 
            // mitigation till `textDocument/prepareRename` is supported 
            // and we can ensure renames aren't triggered in unsupported
            // contexts. (https://github.com/dotnet/aspnetcore/issues/26407)
            if (!tagHelperStartTag.Name.FullSpan.IntersectsWith(hostDocumentIndex))
            {
                return null;
            }

            if (!(tagHelperStartTag?.Parent is MarkupTagHelperElementSyntax tagHelperElement))
            {
                return null;
            }

            // Can only have 1 component TagHelper belonging to an element at a time
            var primaryTagHelper = tagHelperElement.TagHelperInfo.BindingResult.Descriptors.FirstOrDefault(descriptor => descriptor.IsComponentTagHelper());
            if (primaryTagHelper == null)
            {
                return null;
            }

            var originTagHelpers = new List<TagHelperDescriptor>() { primaryTagHelper };
            var associatedTagHelper = FindAssociatedTagHelper(primaryTagHelper, documentSnapshot.Project.TagHelpers);
            if (associatedTagHelper == null)
            {
                Debug.Fail("Components should always have an associated TagHelper.");
                return null;
            }

            originTagHelpers.Add(associatedTagHelper);

            return originTagHelpers;
        }

        private static TagHelperDescriptor FindAssociatedTagHelper(TagHelperDescriptor tagHelper, IReadOnlyList<TagHelperDescriptor> tagHelpers)
        {
            var typeName = tagHelper.GetTypeName();
            var assemblyName = tagHelper.AssemblyName;
            for (var i = 0; i < tagHelpers.Count; i++)
            {
                var currentTagHelper = tagHelpers[i];

                if (tagHelper == currentTagHelper)
                {
                    // Same as the primary, we're looking for our other pair.
                    continue;
                }

                var currentTypeName = currentTagHelper.GetTypeName();
                if (!string.Equals(typeName, currentTypeName, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!string.Equals(assemblyName, currentTagHelper.AssemblyName, StringComparison.Ordinal))
                {
                    continue;
                }

                // Found our associated TagHelper, there should only ever be 1 other associated TagHelper (fully qualified and non-fully qualified).
                return currentTagHelper;
            }

            return null;
        }

        public void SetCapability(RenameCapability capability)
        {
            _capability = capability;
        }
    }
}
