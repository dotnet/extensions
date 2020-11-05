// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.Dialogs
{
    internal abstract class WaitDialogContext
    {
        public abstract CancellationToken CancellationToken { get; }
    }
}
