// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Common
{
    public class AdhocLanguageServices : HostLanguageServices
    {
        private readonly HostWorkspaceServices _workspaceServices;
        private readonly IEnumerable<ILanguageService> _languageServices;

        public AdhocLanguageServices(HostWorkspaceServices workspaceServices, IEnumerable<ILanguageService> languageServices)
        {
            if (workspaceServices == null)
            {
                throw new ArgumentNullException(nameof(workspaceServices));
            }

            if (languageServices == null)
            {
                throw new ArgumentNullException(nameof(languageServices));
            }

            _workspaceServices = workspaceServices;
            _languageServices = languageServices;
        }

        public override HostWorkspaceServices WorkspaceServices => _workspaceServices;

        public override string Language => RazorLanguage.Name;

        public override TLanguageService GetService<TLanguageService>()
        {
            var service = _languageServices.OfType<TLanguageService>().FirstOrDefault();

            if (service == null)
            {
                throw new InvalidOperationException($"Razor language services not configured properly, missing language service '{typeof(TLanguageService).FullName}'.");
            }

            return service;
        }
    }
}
