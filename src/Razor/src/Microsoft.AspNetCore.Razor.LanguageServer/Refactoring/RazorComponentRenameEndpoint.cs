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

namespace Microsoft.AspNetCore.Razor.LanguageServer.Refactoring
{
    internal class RazorComponentRenameEndpoint : IRenameHandler
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly DocumentResolver _documentResolver;
        private readonly ProjectSnapshotManager _projectSnapshotManager;
        private readonly RazorComponentSearchEngine _componentSearchEngine;

        private RenameCapability _capability;

        public RazorComponentRenameEndpoint(
            ForegroundDispatcher foregroundDispatcher,
            DocumentResolver documentResolver,
            RazorComponentSearchEngine componentSearchEngine,
            ProjectSnapshotManagerAccessor projectSnapshotManagerAccessor)
        {
            _foregroundDispatcher = foregroundDispatcher ?? throw new ArgumentNullException(nameof(foregroundDispatcher));
            _documentResolver = documentResolver ?? throw new ArgumentNullException(nameof(documentResolver));
            _componentSearchEngine = componentSearchEngine ?? throw new ArgumentNullException(nameof(componentSearchEngine));
            _projectSnapshotManager = projectSnapshotManagerAccessor?.Instance ?? throw new ArgumentNullException(nameof(projectSnapshotManagerAccessor));
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

            var originTagHelperBinding = await GetOriginTagHelperBindingAsync(requestDocumentSnapshot, codeDocument, request.Position).ConfigureAwait(false);
            if (originTagHelperBinding is null)
            {
                return null;
            }

            var originTagDescriptor = originTagHelperBinding.Descriptors.FirstOrDefault();
            if (originTagDescriptor is null)
            {
                return null;
            }

            var originComponentDocumentSnapshot = await _componentSearchEngine.TryLocateComponentAsync(originTagDescriptor).ConfigureAwait(false);
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
            AddEditsForCodeDocument(documentChanges, originTagHelperBinding, request.NewName, request.TextDocument.Uri, codeDocument);

            var documentSnapshots = await GetAllDocumentSnapshots(requestDocumentSnapshot, cancellationToken).ConfigureAwait(false);
            foreach (var documentSnapshot in documentSnapshots)
            {
                await AddEditsForCodeDocument(documentChanges, originTagHelperBinding, request.NewName, documentSnapshot, cancellationToken);
            }

            return new WorkspaceEdit
            {
                DocumentChanges = documentChanges,
            };
        }

        private async Task<List<DocumentSnapshot>> GetAllDocumentSnapshots(DocumentSnapshot skipDocumentSnapshot, CancellationToken cancellationToken)
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

        public async Task AddEditsForCodeDocument(List<WorkspaceEditDocumentChange> documentChanges, TagHelperBinding originTagHelperBinding, string newName, DocumentSnapshot documentSnapshot, CancellationToken cancellationToken)
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
            AddEditsForCodeDocument(documentChanges, originTagHelperBinding, newName, uri, codeDocument);
        }

        public void AddEditsForCodeDocument(List<WorkspaceEditDocumentChange> documentChanges, TagHelperBinding originTagHelperBinding, string newName, DocumentUri uri, RazorCodeDocument codeDocument)
        {
            var documentIdentifier = new VersionedTextDocumentIdentifier { Uri = uri };
            var tagHelperElements = codeDocument.GetSyntaxTree().Root
                .DescendantNodes()
                .Where(n => n.Kind == SyntaxKind.MarkupTagHelperElement)
                .OfType<MarkupTagHelperElementSyntax>();
            foreach (var node in tagHelperElements)
            {
                if (node is MarkupTagHelperElementSyntax tagHelperElement && BindingsMatch(originTagHelperBinding, tagHelperElement.TagHelperInfo.BindingResult))
                {
                    documentChanges.Add(new WorkspaceEditDocumentChange(new TextDocumentEdit
                    {
                        TextDocument = documentIdentifier,
                        Edits = CreateEditsForMarkupTagHelperElement(tagHelperElement, codeDocument, newName)
                    }));
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

        private static bool BindingsMatch(TagHelperBinding left, TagHelperBinding right)
        {
            foreach (var leftDescriptor in left.Descriptors)
            {
                foreach (var rightDescriptor in right.Descriptors)
                {
                    if (leftDescriptor.Equals(rightDescriptor))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private async Task<TagHelperBinding> GetOriginTagHelperBindingAsync(DocumentSnapshot documentSnapshot, RazorCodeDocument codeDocument, Position position)
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
            var node = owner.Ancestors().FirstOrDefault(n => n.Kind == SyntaxKind.MarkupTagHelperStartTag);
            if (node == null || !(node is MarkupTagHelperStartTagSyntax tagHelperStartTag))
            {
                return null;
            }

            if (!(tagHelperStartTag?.Parent is MarkupTagHelperElementSyntax tagHelperElement))
            {
                return null;
            }

            return tagHelperElement.TagHelperInfo.BindingResult;
        }

        public void SetCapability(RenameCapability capability)
        {
            _capability = capability;
        }
    }
}
