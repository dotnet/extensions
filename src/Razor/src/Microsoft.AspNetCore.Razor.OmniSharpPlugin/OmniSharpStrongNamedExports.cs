// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.OmniSharpPlugin.StrongNamed;
using OmniSharp;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    // We need to re-export MEF based services from the OmniSharp plugin strong named assembly in order
    // to make those services available via MEF. This isn't an issue for Roslyn based services because
    // we're able to hook into OmniSharp's Roslyn service aggregator to allow it to inspect the strong
    // named plugin assembly.

    [Shared]
    [Export(typeof(FilePathNormalizer))]
    public class ExportedFilePathNormalizer : FilePathNormalizer
    {
    }

    [Shared]
    [Export(typeof(OmniSharpForegroundDispatcher))]
    internal class ExportOmniSharpForegroundDispatcher : DefaultOmniSharpForegroundDispatcher
    {
    }

    [Shared]
    [Export(typeof(RemoteTextLoaderFactory))]
    internal class ExportRemoteTextLoaderFactory : DefaultRemoteTextLoaderFactory
    {
        [ImportingConstructor]
        public ExportRemoteTextLoaderFactory(FilePathNormalizer filePathNormalizer) : base(filePathNormalizer)
        {
        }
    }

    [Shared]
    [Export(typeof(OmniSharpProjectSnapshotManagerAccessor))]
    internal class ExportDefaultOmniSharpProjectSnapshotManagerAccessor : DefaultOmniSharpProjectSnapshotManagerAccessor
    {
        [ImportingConstructor]
        public ExportDefaultOmniSharpProjectSnapshotManagerAccessor(
            RemoteTextLoaderFactory remoteTextLoaderFactory,
            [ImportMany] IEnumerable<IOmniSharpProjectSnapshotManagerChangeTrigger> projectChangeTriggers,
            OmniSharpForegroundDispatcher foregroundDispatcher,
            OmniSharpWorkspace workspace) : base(remoteTextLoaderFactory, projectChangeTriggers, foregroundDispatcher, workspace)
        {
        }
    }

    [Shared]
    [Export(typeof(IOmniSharpProjectSnapshotManagerChangeTrigger))]
    public class ExportOmniSharpWorkspaceProjectStateChangeDetector : OmniSharpWorkspaceProjectStateChangeDetector
    {
        [ImportingConstructor]
        public ExportOmniSharpWorkspaceProjectStateChangeDetector(
            OmniSharpForegroundDispatcher foregroundDispatcher,
            OmniSharpProjectWorkspaceStateGenerator workspaceStateGenerator) : base(foregroundDispatcher, workspaceStateGenerator)
        {
        }
    }

    [Shared]
    [Export(typeof(IOmniSharpProjectSnapshotManagerChangeTrigger))]
    [Export(typeof(OmniSharpProjectWorkspaceStateGenerator))]
    public class ExportOmniSharpProjectWorkspaceStateGenerator : OmniSharpProjectWorkspaceStateGenerator
    {
        [ImportingConstructor]
        public ExportOmniSharpProjectWorkspaceStateGenerator(OmniSharpForegroundDispatcher foregroundDispatcher) : base(foregroundDispatcher)
        {
        }
    }

    [Shared]
    [Export(typeof(IOmniSharpProjectSnapshotManagerChangeTrigger))]
    public class ExportOmniSharpBackgroundDocumentGenerator : OmniSharpBackgroundDocumentGenerator
    {
        [ImportingConstructor]
        public ExportOmniSharpBackgroundDocumentGenerator(
            OmniSharpForegroundDispatcher foregroundDispatcher,
            RemoteTextLoaderFactory remoteTextLoaderFactory,
            [ImportMany] IEnumerable<OmniSharpDocumentProcessedListener> documentProcessedListeners) : base(foregroundDispatcher, remoteTextLoaderFactory, documentProcessedListeners)
        {
        }
    }
}
