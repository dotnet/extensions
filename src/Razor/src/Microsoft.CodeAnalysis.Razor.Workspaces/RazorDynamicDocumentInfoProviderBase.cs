// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Composition;
using System.IO;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Razor
{
    /// <summary>
    /// Base MEF component for implementing a dynamic document info provider
    /// Currently only used by VS Mac
    /// </summary>
    internal abstract class RazorDynamicDocumentInfoProviderBase
    {
        private readonly ConcurrentDictionary<Key, Entry> _entries;

        [Import]
        private DocumentServiceProviderFactory Factory { get; set; }

        public RazorDynamicDocumentInfoProviderBase()
        {
            _entries = new ConcurrentDictionary<Key, Entry>();
        }

        public event Action<DocumentInfo> Updated;

        // Called by us to update entries
        public void UpdateFileInfo(ProjectSnapshot projectSnapshot, DocumentSnapshot document)
        {
            if (projectSnapshot == null)
            {
                throw new ArgumentNullException(nameof(projectSnapshot));
            }

            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            // There's a possible race condition here where we're processing an update
            // and the project is getting unloaded. So if we don't find an entry we can
            // just ignore it.
            var key = new Key(projectSnapshot.FilePath, document.FilePath);
            if (_entries.TryGetValue(key, out var entry))
            {
                lock (entry.Lock)
                {
                    entry.Current = entry.Current
                        .WithTextLoader(new GeneratedDocumentTextLoader(document, entry.Current.FilePath));
                }

                Updated?.Invoke(entry.Current);
            }
        }

        // Called by us when a document opens in the editor
        public void SuppressDocument(ProjectSnapshot project, DocumentSnapshot document)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            // There's a possible race condition here where we're processing an update
            // and the project is getting unloaded. So if we don't find an entry we can
            // just ignore it.
            var key = new Key(project.FilePath, document.FilePath);
            if (_entries.TryGetValue(key, out var entry))
            {
                var updated = false;
                lock (entry.Lock)
                {
                    if (entry.Current.TextLoader is GeneratedDocumentTextLoader)
                    {
                        updated = true;
                        entry.Current = entry.Current.WithTextLoader(new EmptyTextLoader(entry.Current.FilePath));
                    }
                }

                if (updated)
                {
                    Updated?.Invoke(entry.Current);
                }
            }
        }

        public DocumentInfo GetDynamicDocumentInfo(ProjectId projectId, string projectFilePath, string filePath)
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
            var entry = _entries.GetOrAdd(key, k => new Entry(CreateEmptyInfo(k, projectId)));
            return entry.Current;
        }

        public void RemoveDynamicDocumentInfo(ProjectId projectId, string projectFilePath, string filePath)
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
            _entries.TryRemove(key, out var entry);
        }

        private DocumentInfo CreateEmptyInfo(Key key, ProjectId projectId)
        {
            var filename = Path.ChangeExtension(key.FilePath, ".g.cs");
            var textLoader = new EmptyTextLoader(filename);
            var docId = DocumentId.CreateNewId(projectId, debugName: filename);
            return DocumentInfo.Create(
                id: docId, 
                name: Path.GetFileName(filename),
                folders: null,
                sourceCodeKind: SourceCodeKind.Regular,
                filePath: filename,
                loader: textLoader,
                isGenerated: true,
                documentServiceProvider: Factory.CreateEmpty());
        }

        // Using a separate handle to the 'current' file info so that can allow Roslyn to send
        // us the add/remove operations, while we process the update operations.
        public class Entry
        {
            // Can't ever be null for thread-safety reasons
            private DocumentInfo _current;

            public Entry(DocumentInfo current)
            {
                if (current == null)
                {
                    throw new ArgumentNullException(nameof(current));
                }

                Current = current;
                Lock = new object();
            }

            public DocumentInfo Current
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
                return (FilePathComparer.Instance.GetHashCode(ProjectFilePath), FilePathComparer.Instance.GetHashCode(FilePath)).GetHashCode();
            }
        }
    }
}
