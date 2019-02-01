// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal abstract class ProjectSnapshot
    {
        public abstract RazorConfiguration Configuration { get; }

        public abstract IEnumerable<string> DocumentFilePaths { get; }

        public abstract string FilePath { get; }

        [Obsolete]
        public abstract bool IsInitialized { get; }

        public abstract VersionStamp Version { get; }

        [Obsolete]
        public abstract Project WorkspaceProject { get; }

        public virtual IReadOnlyList<TagHelperDescriptor> TagHelpers { get; }

        public abstract RazorProjectEngine GetProjectEngine();

        public abstract DocumentSnapshot GetDocument(string filePath);

        public abstract bool IsImportDocument(DocumentSnapshot document);

        /// <summary>
        /// If the provided document is an import document, gets the other documents in the project
        /// that include directives specified by the provided document. Otherwise returns an empty
        /// list.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <returns>A list of related documents.</returns>
        public abstract IEnumerable<DocumentSnapshot> GetRelatedDocuments(DocumentSnapshot document);

        [Obsolete("Use the " + nameof(TagHelpers) + " propery instead")]
        public abstract Task<IReadOnlyList<TagHelperDescriptor>> GetTagHelpersAsync();

        [Obsolete("Use the " + nameof(TagHelpers) + " propery instead")]
        public abstract bool TryGetTagHelpers(out IReadOnlyList<TagHelperDescriptor> result);
    }
}