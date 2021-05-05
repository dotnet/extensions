// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.VisualStudio.Editor.Razor;
using MonoDevelop.Ide.TypeSystem;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    // Unlike Visual Studio windows this class is not used to enable Find All References in VS4Mac. It's used to take
    // the output of generated C# and push that content into the VS4Mac's workspace. This way in Blazor scenarios we
    // can introspect over the solution to find Components that should be turned into TagHelperDescriptors.
    [System.Composition.Shared]
    [ExportMetadata("Extensions", new string[] { "cshtml", "razor", })]
    [Export(typeof(IDynamicDocumentInfoProvider))]
    internal class RazorDynamicDocumentInfoProvider : IDynamicDocumentInfoProvider
    {
        private readonly ConcurrentDictionary<Key, Entry> _entries;
        private readonly VisualStudioMacDocumentInfoFactory _documentInfoFactory;
        private readonly IRazorDynamicFileInfoProvider _dynamicFileInfoProvider;

        [ImportingConstructor]
        public RazorDynamicDocumentInfoProvider(
            VisualStudioMacDocumentInfoFactory documentInfoFactory,
            IRazorDynamicFileInfoProvider dynamicFileInfoProvider)
        {
            _entries = new ConcurrentDictionary<Key, Entry>();
            _documentInfoFactory = documentInfoFactory;
            _dynamicFileInfoProvider = dynamicFileInfoProvider;
            _dynamicFileInfoProvider.Updated += InnerUpdated;
        }

        public event Action<DocumentInfo> Updated;

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

            // The underlying method doesn't actually do anything truly asynchronous which allows us to synchronously call it.
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
            _ = _dynamicFileInfoProvider.GetDynamicFileInfoAsync(projectId, projectFilePath, filePath, CancellationToken.None).Result;
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits

            var key = new Key(projectId, projectFilePath, filePath);
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

            // The underlying method doesn't actually do anything truly asynchronous which allows us to synchronously call and wait on it.
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
            _dynamicFileInfoProvider.RemoveDynamicFileInfoAsync(projectId, projectFilePath, filePath, CancellationToken.None).Wait();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits

            var key = new Key(projectId, projectFilePath, filePath);
            _entries.TryRemove(key, out _);
        }

        private void InnerUpdated(object sender, string path)
        {
            // A filepath could be shared among more than one project which would result in us having multiple document infos present.
            // To address this we capture all the document infos that apply to the "updated" filepath
            var impactedEntries = _entries.Where(kvp => string.Equals(kvp.Key.FilePath, path, FilePathComparison.Instance)).ToList();

            for (var i = 0; i < impactedEntries.Count; i++)
            {
                var impactedEntry = impactedEntries[i];

                lock (impactedEntry.Value.Lock)
                {
                    // The underlying method doesn't actually do anything truly asynchronous which allows us to synchronously call it.
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                    var innerDynamicFileInfo = _dynamicFileInfoProvider.GetDynamicFileInfoAsync(impactedEntry.Key.ProjectId, impactedEntry.Key.ProjectFilePath, impactedEntry.Key.FilePath, CancellationToken.None).Result;
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
                    var newDocumentInfo = impactedEntry.Value.Current.WithTextLoader(innerDynamicFileInfo.TextLoader);
                    impactedEntry.Value.Current = newDocumentInfo;
                    Updated?.Invoke(newDocumentInfo);
                }
            }
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
            public readonly ProjectId ProjectId;
            public readonly string ProjectFilePath;
            public readonly string FilePath;

            public Key(ProjectId projectId, string projectFilePath, string filePath)
            {
                ProjectId = projectId;
                ProjectFilePath = projectFilePath;
                FilePath = filePath;
            }

            public bool Equals(Key other)
            {
                return
                    ProjectId == other.ProjectId &&
                    FilePathComparer.Instance.Equals(ProjectFilePath, other.ProjectFilePath) &&
                    FilePathComparer.Instance.Equals(FilePath, other.FilePath);
            }

            public override bool Equals(object obj)
            {
                return obj is Key other && Equals(other);
            }

            public override int GetHashCode()
            {
                return (
                    ProjectId?.GetHashCode(),
                    FilePathComparer.Instance.GetHashCode(ProjectFilePath),
                    FilePathComparer.Instance.GetHashCode(FilePath)).GetHashCode();
            }
        }
    }
}
