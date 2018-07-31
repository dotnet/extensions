// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed
{
    internal class DefaultProjectSnapshotManagerShimAccessor : ProjectSnapshotManagerShimAccessor
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly ErrorReporter _errorReporter;
        private ProjectSnapshotManagerShim _instance;

        public DefaultProjectSnapshotManagerShimAccessor(
            ForegroundDispatcher foregroundDispatcher,
            ErrorReporter errorReporter)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (errorReporter == null)
            {
                throw new ArgumentNullException(nameof(errorReporter));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _errorReporter = errorReporter;
        }

        public override ProjectSnapshotManagerShim Instance
        {
            get
            {
                if (_instance == null)
                {
                    var workspace = new AdhocWorkspace();
                    var projectSnapshotManager = new DefaultProjectSnapshotManager(
                        _foregroundDispatcher,
                        _errorReporter,
                        Enumerable.Empty<ProjectSnapshotChangeTrigger>(),
                        workspace);
                    _instance = new DefaultProjectSnapshotManagerShim(projectSnapshotManager);
                }

                return _instance;
            }
        }
    }
}
