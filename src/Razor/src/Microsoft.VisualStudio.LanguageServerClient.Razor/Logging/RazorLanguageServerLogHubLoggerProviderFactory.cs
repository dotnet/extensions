// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Composition;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.Logging
{
    [Shared]
    [Export(typeof(RazorLanguageServerLogHubLoggerProviderFactory))]
    internal class RazorLanguageServerLogHubLoggerProviderFactory : LogHubLoggerProviderFactoryBase
    {
        [ImportingConstructor]
        public RazorLanguageServerLogHubLoggerProviderFactory(RazorLogHubTraceProvider traceProvider) :
            base(traceProvider)
        {
        }
    }
}
