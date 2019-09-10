// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Common
{
    public class AdhocServices : HostServices
    {
        private readonly IEnumerable<IWorkspaceService> _workspaceServices;
        private readonly IEnumerable<ILanguageService> _razorLanguageServices;

        private AdhocServices(IEnumerable<IWorkspaceService> workspaceServices, IEnumerable<ILanguageService> razorLanguageServices)
        {
            if (workspaceServices == null)
            {
                throw new ArgumentNullException(nameof(workspaceServices));
            }

            if (razorLanguageServices == null)
            {
                throw new ArgumentNullException(nameof(razorLanguageServices));
            }

            _workspaceServices = workspaceServices;
            _razorLanguageServices = razorLanguageServices;
        }

        protected override HostWorkspaceServices CreateWorkspaceServices(Workspace workspace)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            return new AdhocWorkspaceServices(this, _workspaceServices, _razorLanguageServices, workspace);
        }

        public static HostServices Create(IEnumerable<ILanguageService> razorLanguageServices)
            => Create(Enumerable.Empty<IWorkspaceService>(), razorLanguageServices);

        public static HostServices Create(IEnumerable<IWorkspaceService> workspaceServices, IEnumerable<ILanguageService> razorLanguageServices)
            => new AdhocServices(workspaceServices, razorLanguageServices);
    }
}
