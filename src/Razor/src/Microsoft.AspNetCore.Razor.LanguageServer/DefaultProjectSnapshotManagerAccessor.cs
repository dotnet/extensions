// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class DefaultProjectSnapshotManagerAccessor : ProjectSnapshotManagerAccessor, IDisposable
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly IEnumerable<ProjectSnapshotChangeTrigger> _changeTriggers;
        private readonly FilePathNormalizer _filePathNormalizer;
        private readonly IOptionsMonitor<RazorLSPOptions> _optionsMonitor;
        private ProjectSnapshotManagerBase _instance;
        private bool _disposed;

        public DefaultProjectSnapshotManagerAccessor(
            ForegroundDispatcher foregroundDispatcher,
            IEnumerable<ProjectSnapshotChangeTrigger> changeTriggers,
            FilePathNormalizer filePathNormalizer,
            IOptionsMonitor<RazorLSPOptions> optionsMonitor)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (changeTriggers == null)
            {
                throw new ArgumentNullException(nameof(changeTriggers));
            }

            if (filePathNormalizer == null)
            {
                throw new ArgumentNullException(nameof(filePathNormalizer));
            }

            if (optionsMonitor is null)
            {
                throw new ArgumentNullException(nameof(optionsMonitor));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _changeTriggers = changeTriggers;
            _filePathNormalizer = filePathNormalizer;
            _optionsMonitor = optionsMonitor;
        }

        public override ProjectSnapshotManagerBase Instance
        {
            get
            {
                if (_instance == null)
                {
                    var services = AdhocServices.Create(
                        workspaceServices: new[]
                        {
                            new RemoteProjectSnapshotProjectEngineFactory(_filePathNormalizer, _optionsMonitor)
                        },
                        razorLanguageServices: Enumerable.Empty<ILanguageService>());
                    var workspace = new AdhocWorkspace(services);
                    _instance = new DefaultProjectSnapshotManager(
                        _foregroundDispatcher,
                        new DefaultErrorReporter(),
                        _changeTriggers,
                        workspace);
                }

                return _instance;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _instance?.Workspace.Dispose();
        }
    }
}
