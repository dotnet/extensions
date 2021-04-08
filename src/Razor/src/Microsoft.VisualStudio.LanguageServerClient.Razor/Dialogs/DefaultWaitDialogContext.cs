// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.Dialogs
{
    internal class DefaultWaitDialogContext : WaitDialogContext
    {
        private readonly CancellationTokenSource _cts;

        public DefaultWaitDialogContext()
        {
            _cts = new CancellationTokenSource();
        }

        public override CancellationToken CancellationToken => _cts.Token;

        public void OnCanceled() => _cts.Cancel();
    }
}
