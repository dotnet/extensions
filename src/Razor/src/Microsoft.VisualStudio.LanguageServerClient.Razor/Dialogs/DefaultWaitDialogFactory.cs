// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.Dialogs
{
    [Export(typeof(WaitDialogFactory))]
    internal class DefaultWaitDialogFactory : WaitDialogFactory
    {
        private readonly IVsThreadedWaitDialogFactory _waitDialogFactory;
        private readonly JoinableTaskFactory _joinableTaskFactory;

        [ImportingConstructor]
        public DefaultWaitDialogFactory(JoinableTaskContext joinableTaskContext)
        {
            if (joinableTaskContext is null)
            {
                throw new ArgumentNullException(nameof(joinableTaskContext));
            }

            _waitDialogFactory = (IVsThreadedWaitDialogFactory)Shell.Package.GetGlobalService(typeof(SVsThreadedWaitDialogFactory));
            if (_waitDialogFactory == null)
            {
                throw new ArgumentNullException(nameof(_waitDialogFactory));
            }

            _joinableTaskFactory = joinableTaskContext.Factory;
        }

        // Test constructor
        internal DefaultWaitDialogFactory(
            JoinableTaskContext joinableTaskContext,
            IVsThreadedWaitDialogFactory waitDialogFactory)
        {

            _waitDialogFactory = waitDialogFactory;
            _joinableTaskFactory = joinableTaskContext.Factory;
        }

        public override WaitDialogResult<TResult> TryCreateWaitDialog<TResult>(string title, string message, Func<WaitDialogContext, Task<TResult>> onWaitAsync)
        {
            Debug.Assert(_joinableTaskFactory.Context.IsOnMainThread);

            var result = _waitDialogFactory.CreateInstance(out var dialog2);

            if (result != VSConstants.S_OK)
            {
                return null;
            }

            if (!(dialog2 is IVsThreadedWaitDialog4 dialog4))
            {
                Debug.Fail("This is unexpected, the dialog should always be an IVsThreadedWaitDialog4");
                return null;
            }

            var context = new DefaultWaitDialogContext();
            var callback = new Callback(context);

            dialog4.StartWaitDialogWithCallback(title, message, szProgressText: null, varStatusBmpAnim: null, szStatusBarText: null, fIsCancelable: true, iDelayToShowDialog: 2, fShowProgress: false, iTotalSteps: 0, iCurrentStep: 0, pCallback: callback);

            TResult waitResult = default;
            _joinableTaskFactory.Run(async () =>
            {
                try
                {
                    waitResult = await onWaitAsync(context).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    // Swallow task cancelled exceptions, they represent the user cancelling the wait dialog.
                }
            });

            dialog4.EndWaitDialog(out var cancelledResult);

            var cancelled = cancelledResult != 0;
            var dialogResult = new WaitDialogResult<TResult>(waitResult, cancelled);
            return dialogResult;
        }

        private class Callback : IVsThreadedWaitDialogCallback
        {
            private readonly DefaultWaitDialogContext _waitContext;

            public Callback(DefaultWaitDialogContext waitContext) => _waitContext = waitContext;

            public void OnCanceled() => _waitContext.OnCanceled();
        }
    }
}
