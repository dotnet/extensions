// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.Dialogs
{
    internal class WaitDialogResult<TResult>
    {
        public WaitDialogResult(TResult result, bool cancelled)
        {
            Result = result;
            Cancelled = cancelled;
        }

        public TResult Result { get; }

        public bool Cancelled { get; }
    }
}
