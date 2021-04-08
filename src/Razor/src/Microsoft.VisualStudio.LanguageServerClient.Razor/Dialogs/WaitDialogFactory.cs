// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.Dialogs
{
    internal abstract class WaitDialogFactory
    {
        public abstract WaitDialogResult<TResult> TryCreateWaitDialog<TResult>(string title, string message, Func<WaitDialogContext, Task<TResult>> onWaitAsync);
    }
}
