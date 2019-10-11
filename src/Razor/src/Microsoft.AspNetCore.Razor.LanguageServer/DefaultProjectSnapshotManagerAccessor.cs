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

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class DefaultProjectSnapshotManagerAccessor : ProjectSnapshotManagerAccessor
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly IEnumerable<ProjectSnapshotChangeTrigger> _changeTriggers;
        private readonly FilePathNormalizer _filePathNormalizer;
        private ProjectSnapshotManagerBase _instance;

        public DefaultProjectSnapshotManagerAccessor(
            ForegroundDispatcher foregroundDispatcher,
            IEnumerable<ProjectSnapshotChangeTrigger> changeTriggers,
            FilePathNormalizer filePathNormalizer)
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

            _foregroundDispatcher = foregroundDispatcher;
            _changeTriggers = changeTriggers;
            _filePathNormalizer = filePathNormalizer;
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
                            new RemoteProjectSnapshotProjectEngineFactory(_filePathNormalizer)
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
    }
}
