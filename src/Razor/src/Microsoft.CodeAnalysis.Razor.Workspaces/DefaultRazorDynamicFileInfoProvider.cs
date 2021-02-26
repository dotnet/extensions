// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Internal;

namespace Microsoft.CodeAnalysis.Razor.Workspaces
{
    [Shared]
    [Export(typeof(IRazorDynamicFileInfoProvider))]
    [Export(typeof(RazorDynamicFileInfoProvider))]
    internal class DefaultRazorDynamicFileInfoProvider : RazorDynamicFileInfoProvider, IRazorDynamicFileInfoProvider
    {
        private readonly ConcurrentDictionary<Key, Entry> _entries;
        private readonly Func<Key, Entry> _createEmptyEntry;
        private readonly RazorDocumentServiceProviderFactory _factory;
        private readonly LSPEditorFeatureDetector _lspEditorFeatureDetector;

        [ImportingConstructor]
        public DefaultRazorDynamicFileInfoProvider(RazorDocumentServiceProviderFactory factory, LSPEditorFeatureDetector lspEditorFeatureDetector)
        {
            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (lspEditorFeatureDetector is null)
            {
                throw new ArgumentNullException(nameof(lspEditorFeatureDetector));
            }

            _factory = factory;
            _lspEditorFeatureDetector = lspEditorFeatureDetector;
            _entries = new ConcurrentDictionary<Key, Entry>();
            _createEmptyEntry = (key) => new Entry(CreateEmptyInfo(key));
        }

        public event EventHandler<string> Updated;

        // Called by us to update LSP document entries
        public override void UpdateLSPFileInfo(Uri documentUri, DynamicDocumentContainer documentContainer)
        {
            if (documentUri is null)
            {
                throw new ArgumentNullException(nameof(documentUri));
            }

            if (documentContainer is null)
            {
                throw new ArgumentNullException(nameof(documentContainer));
            }

            // This endpoint is only called in LSP cases when the file is open(ed)
            // We report diagnostics are supported to Roslyn in this case
            documentContainer.SupportsDiagnostics = true;

            var filePath = documentUri.GetAbsoluteOrUNCPath().Replace('/', '\\');
            KeyValuePair<Key, Entry>? associatedKvp = null;
            foreach (var entry in _entries)
            {
                if (FilePathComparer.Instance.Equals(filePath, entry.Key.FilePath))
                {
                    associatedKvp = entry;
                    break;
                }
            }

            if (associatedKvp == null)
            {
                return;
            }

            var associatedKey = associatedKvp.Value.Key;
            var associatedEntry = associatedKvp.Value.Value;

            lock (associatedEntry.Lock)
            {
                associatedEntry.Current = CreateInfo(associatedKey, documentContainer);
            }

            Updated?.Invoke(this, filePath);
        }

        // Called by us to update entries
        public override void UpdateFileInfo(string projectFilePath, DynamicDocumentContainer documentContainer)
        {
            if (projectFilePath == null)
            {
                throw new ArgumentNullException(nameof(projectFilePath));
            }

            if (documentContainer == null)
            {
                throw new ArgumentNullException(nameof(documentContainer));
            }

            // This endpoint is called either when:
            //  1. LSP: File is closed
            //  2. Non-LSP: File is Supressed
            // We report, diagnostics are not supported, to Roslyn in these cases
            documentContainer.SupportsDiagnostics = false;

            // There's a possible race condition here where we're processing an update
            // and the project is getting unloaded. So if we don't find an entry we can
            // just ignore it.
            var key = new Key(projectFilePath, documentContainer.FilePath);
            if (_entries.TryGetValue(key, out var entry))
            {
                lock (entry.Lock)
                {
                    entry.Current = CreateInfo(key, documentContainer);
                }

                Updated?.Invoke(this, documentContainer.FilePath);
            }
        }

        // Called by us when a document opens in the editor
        public override void SuppressDocument(string projectFilePath, string documentFilePath)
        {
            if (projectFilePath == null)
            {
                throw new ArgumentNullException(nameof(projectFilePath));
            }

            if (documentFilePath == null)
            {
                throw new ArgumentNullException(nameof(documentFilePath));
            }

            if (_lspEditorFeatureDetector.IsLSPEditorFeatureEnabled())
            {
                return;
            }

            // There's a possible race condition here where we're processing an update
            // and the project is getting unloaded. So if we don't find an entry we can
            // just ignore it.
            var key = new Key(projectFilePath, documentFilePath);
            if (_entries.TryGetValue(key, out var entry))
            {
                var updated = false;
                lock (entry.Lock)
                {
                    if (!(entry.Current.TextLoader is EmptyTextLoader))
                    {
                        updated = true;
                        entry.Current = CreateEmptyInfo(key);
                    }
                }

                if (updated)
                {
                    Updated?.Invoke(this, documentFilePath);
                }
            }
        }

        public Task<RazorDynamicFileInfo> GetDynamicFileInfoAsync(ProjectId projectId, string projectFilePath, string filePath, CancellationToken cancellationToken)
        {
            if (projectFilePath == null)
            {
                throw new ArgumentNullException(nameof(projectFilePath));
            }

            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            var key = new Key(projectFilePath, filePath);
            var entry = _entries.GetOrAdd(key, _createEmptyEntry);
            return Task.FromResult(entry.Current);
        }

        public Task RemoveDynamicFileInfoAsync(ProjectId projectId, string projectFilePath, string filePath, CancellationToken cancellationToken)
        {
            if (projectFilePath == null)
            {
                throw new ArgumentNullException(nameof(projectFilePath));
            }

            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            var key = new Key(projectFilePath, filePath);
            _entries.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        private RazorDynamicFileInfo CreateEmptyInfo(Key key)
        {
            var filename = key.FilePath + ".g.cs";
            var textLoader = new EmptyTextLoader(filename);
            return new RazorDynamicFileInfo(filename, SourceCodeKind.Regular, textLoader, _factory.CreateEmpty());
        }

        private RazorDynamicFileInfo CreateInfo(Key key, DynamicDocumentContainer document)
        {
            var filename = key.FilePath + ".g.cs";
            var textLoader = document.GetTextLoader(filename);
            return new RazorDynamicFileInfo(filename, SourceCodeKind.Regular, textLoader, _factory.Create(document));
        }

        // Using a separate handle to the 'current' file info so that can allow Roslyn to send
        // us the add/remove operations, while we process the update operations.
        public class Entry
        {
            // Can't ever be null for thread-safety reasons
            private RazorDynamicFileInfo _current;

            public Entry(RazorDynamicFileInfo current)
            {
                if (current == null)
                {
                    throw new ArgumentNullException(nameof(current));
                }

                Current = current;
                Lock = new object();
            }

            public RazorDynamicFileInfo Current
            {
                get => _current;
                set
                {
                    if (value == null)
                    {
                        throw new ArgumentNullException(nameof(value));
                    }

                    _current = value;
                }
            }

            public object Lock { get; }

            public override string ToString()
            {
                lock (Lock)
                {
                    return $"{Current.FilePath} - {Current.TextLoader.GetType()}";
                }
            }
        }

        private readonly struct Key : IEquatable<Key>
        {
            public readonly string ProjectFilePath;
            public readonly string FilePath;

            public Key(string projectFilePath, string filePath)
            {
                ProjectFilePath = projectFilePath;
                FilePath = filePath;
            }

            public bool Equals(Key other)
            {
                return
                    FilePathComparer.Instance.Equals(ProjectFilePath, other.ProjectFilePath) &&
                    FilePathComparer.Instance.Equals(FilePath, other.FilePath);
            }

            public override bool Equals(object obj)
            {
                return obj is Key other ? Equals(other) : false;
            }

            public override int GetHashCode()
            {
                var hash = HashCodeCombiner.Start();
                hash.Add(ProjectFilePath, FilePathComparer.Instance);
                hash.Add(FilePath, FilePathComparer.Instance);
                return hash;
            }
        }

        private class EmptyTextLoader : TextLoader
        {
            private readonly string _filePath;
            private readonly VersionStamp _version;

            public EmptyTextLoader(string filePath)
            {
                _filePath = filePath;
                _version = VersionStamp.Default; // Version will never change so this can be reused.
            }

            public override Task<TextAndVersion> LoadTextAndVersionAsync(Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
            {
                // Providing an encoding here is important for debuggability. Without this edit-and-continue
                // won't work for projects with Razor files.
                return Task.FromResult(TextAndVersion.Create(SourceText.From("", Encoding.UTF8), _version, _filePath));
            }
        }
    }
}
