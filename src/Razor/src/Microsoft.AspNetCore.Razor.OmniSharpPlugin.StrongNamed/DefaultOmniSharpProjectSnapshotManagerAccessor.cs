// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    public class DefaultOmniSharpProjectSnapshotManagerAccessor : OmniSharpProjectSnapshotManagerAccessor
    {
        private readonly IEnumerable<IOmniSharpProjectSnapshotManagerChangeTrigger> _projectChangeTriggers;
        private readonly OmniSharpForegroundDispatcher _foregroundDispatcher;
        private readonly Workspace _workspace;
        private OmniSharpProjectSnapshotManager _instance;

        [ImportingConstructor]
        public DefaultOmniSharpProjectSnapshotManagerAccessor(
            [ImportMany] IEnumerable<IOmniSharpProjectSnapshotManagerChangeTrigger> projectChangeTriggers,
            OmniSharpForegroundDispatcher foregroundDispatcher,
            Workspace workspace)
        {
            if (projectChangeTriggers == null)
            {
                throw new ArgumentNullException(nameof(projectChangeTriggers));
            }

            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            _projectChangeTriggers = projectChangeTriggers;
            _foregroundDispatcher = foregroundDispatcher;
            _workspace = workspace;
        }

        public override OmniSharpProjectSnapshotManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var projectSnapshotManager = new DefaultProjectSnapshotManager(
                        _foregroundDispatcher.InternalDispatcher,
                        new DefaultErrorReporter(),
                        Enumerable.Empty<ProjectSnapshotChangeTrigger>(),
                        _workspace);

                    var instance = new DefaultOmniSharpProjectSnapshotManager(projectSnapshotManager);
                    _instance = instance;
                    foreach (var changeTrigger in _projectChangeTriggers)
                    {
                        changeTrigger.Initialize(instance);
                    }
                }

                return _instance;
            }
        }
    }
}
