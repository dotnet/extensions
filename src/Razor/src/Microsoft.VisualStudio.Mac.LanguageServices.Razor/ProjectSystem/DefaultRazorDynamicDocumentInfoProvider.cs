// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.VisualStudio.Editor.Razor;
using MonoDevelop.Ide.TypeSystem;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    // Unlike Visual Studio windows this class is not used to enable Find All References in VS4Mac. It's used to take
    // the output of generated C# and push that content into the VS4Mac's workspace. This way in Blazor scenarios we
    // can introspect over the solution to find Components that should be turned into TagHelperDescriptors.
    [System.Composition.Shared]
    [ExportMetadata("Extensions", new string[] { "cshtml", "razor", })]
    [Export(typeof(RazorDynamicFileInfoProvider))]
    [Export(typeof(IDynamicDocumentInfoProvider))]
    internal class DefaultRazorDynamicDocumentInfoProvider : RazorDynamicFileInfoProvider, IDynamicDocumentInfoProvider
    {
        private readonly ConcurrentDictionary<Key, Entry> _entries;
        private readonly VisualStudioMacDocumentInfoFactory _documentInfoFactory;

        [ImportingConstructor]
        public DefaultRazorDynamicDocumentInfoProvider(VisualStudioMacDocumentInfoFactory documentInfoFactory)
        {
            _entries = new ConcurrentDictionary<Key, Entry>();
            _documentInfoFactory = documentInfoFactory;
        }

        public event Action<DocumentInfo> Updated;

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

            // There's a possible race condition here where we're processing an update
            // and the project is getting unloaded. So if we don't find an entry we can
            // just ignore it.
            var key = new Key(projectFilePath, documentContainer.FilePath);
            if (_entries.TryGetValue(key, out var entry))
            {
                lock (entry.Lock)
                {
                    var textLoader = documentContainer.GetTextLoader(entry.Current.FilePath);
                    entry.Current = entry.Current.WithTextLoader(textLoader);
                }

                Updated?.Invoke(entry.Current);
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

            // There's a possible race condition here where we're processing an update
            // and the project is getting unloaded. So if we don't find an entry we can
            // just ignore it.
            var key = new Key(projectFilePath, documentFilePath);
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
            var entry = _entries.GetOrAdd(key, k => new Entry(_documentInfoFactory.CreateEmpty(k.FilePath, projectId)));
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