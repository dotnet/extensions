// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.Dialogs
{
    public class DefaultWaitDialogFactoryTest : IDisposable
    {
        public DefaultWaitDialogFactoryTest()
        {
            JoinableTaskContext = new JoinableTaskContext();
        }

        private JoinableTaskContext JoinableTaskContext { get; }

        private JoinableTaskFactory JoinableTaskFactory => JoinableTaskContext.Factory;

        [Fact]
        public void TryCreateWaitDialog_Cancelled()
        {
            // Arrange
            var dialog = new TestThreadedWaitDialog();
            var vsWaitDialogFactory = CreateVSWaitDialogFactory(dialog);
            var waitDialogFactory = new DefaultWaitDialogFactory(JoinableTaskContext, vsWaitDialogFactory);

            // Act
            var result = waitDialogFactory.TryCreateWaitDialog(title: null, message: null, async (context) =>
            {
                await Task.Delay(10, context.CancellationToken);

                dialog.Cancel();

                await Task.Delay(10000, context.CancellationToken);
                return true;
            });

            // Assert
            Assert.True(result.Cancelled);
            Assert.False(result.Result);
        }

        [Fact]
        public void TryCreateWaitDialog_Success()
        {
            // Arrange
            var vsWaitDialogFactory = CreateVSWaitDialogFactory();
            var waitDialogFactory = new DefaultWaitDialogFactory(JoinableTaskContext, vsWaitDialogFactory);

            // Act
            var result = waitDialogFactory.TryCreateWaitDialog(title: null, message: null, async (context) =>
            {
                await Task.Delay(10, context.CancellationToken);
                return 1234;
            });

            // Assert
            Assert.False(result.Cancelled);
            Assert.Equal(1234, result.Result);
        }

        [Fact]
        public void TryCreateWaitDialog_FailureToCreateDialog_ReturnsNull()
        {
            // Arrange
            IVsThreadedWaitDialog2 waitDialog2 = null;
            var vsWaitDialogFactory = Mock.Of<IVsThreadedWaitDialogFactory>(factory => factory.CreateInstance(out waitDialog2) == VSConstants.E_FAIL, MockBehavior.Strict);
            var waitDialogFactory = new DefaultWaitDialogFactory(JoinableTaskContext, vsWaitDialogFactory);

            // Act
            var result = waitDialogFactory.TryCreateWaitDialog(title: null, message: null, (context) => Task.FromResult(true));

            // Assert
            Assert.Null(result);
        }

        public void Dispose()
        {
            JoinableTaskContext.Dispose();
        }

        private IVsThreadedWaitDialogFactory CreateVSWaitDialogFactory(IVsThreadedWaitDialog2 waitDialog = null)
        {
            waitDialog ??= new TestThreadedWaitDialog();
            var vsWaitDialogFactory = Mock.Of<IVsThreadedWaitDialogFactory>(factory => factory.CreateInstance(out waitDialog) == VSConstants.S_OK, MockBehavior.Strict);
            return vsWaitDialogFactory;
        }

        private class TestThreadedWaitDialog : IVsThreadedWaitDialog2, IVsThreadedWaitDialog4
        {
            private IVsThreadedWaitDialogCallback _callback;
            private bool _cancelled;

            public void Cancel()
            {
                _cancelled = true;
                _callback?.OnCanceled();
            }

            public void StartWaitDialogWithCallback(string szWaitCaption, string szWaitMessage, string szProgressText, object varStatusBmpAnim, string szStatusBarText, bool fIsCancelable, int iDelayToShowDialog, bool fShowProgress, int iTotalSteps, int iCurrentStep, IVsThreadedWaitDialogCallback pCallback)
            {
                _callback = pCallback;
            }

            public void EndWaitDialog(out int pfCanceled)
            {
                pfCanceled = _cancelled ? 1 : 0;
            }

            public void StartWaitDialog(string szWaitCaption, string szWaitMessage, string szProgressText, object varStatusBmpAnim, string szStatusBarText, int iDelayToShowDialog, bool fIsCancelable, bool fShowMarqueeProgress) => throw new NotImplementedException();

            public void StartWaitDialogWithPercentageProgress(string szWaitCaption, string szWaitMessage, string szProgressText, object varStatusBmpAnim, string szStatusBarText, bool fIsCancelable, int iDelayToShowDialog, int iTotalSteps, int iCurrentStep) => throw new NotImplementedException();

            public void UpdateProgress(string szUpdatedWaitMessage, string szProgressText, string szStatusBarText, int iCurrentStep, int iTotalSteps, bool fDisableCancel, out bool pfCanceled) => throw new NotImplementedException();

            public void HasCanceled(out bool pfCanceled) => throw new NotImplementedException();

            public bool StartWaitDialogEx(string szWaitCaption, string szWaitMessage, string szProgressText, object varStatusBmpAnim, string szStatusBarText, int iDelayToShowDialog, bool fIsCancelable, bool fShowMarqueeProgress) => throw new NotImplementedException();

            int IVsThreadedWaitDialog2.StartWaitDialog(string szWaitCaption, string szWaitMessage, string szProgressText, object varStatusBmpAnim, string szStatusBarText, int iDelayToShowDialog, bool fIsCancelable, bool fShowMarqueeProgress) => throw new NotImplementedException();

            int IVsThreadedWaitDialog2.StartWaitDialogWithPercentageProgress(string szWaitCaption, string szWaitMessage, string szProgressText, object varStatusBmpAnim, string szStatusBarText, bool fIsCancelable, int iDelayToShowDialog, int iTotalSteps, int iCurrentStep) => throw new NotImplementedException();

            int IVsThreadedWaitDialog2.EndWaitDialog(out int pfCanceled) => throw new NotImplementedException();

            int IVsThreadedWaitDialog2.UpdateProgress(string szUpdatedWaitMessage, string szProgressText, string szStatusBarText, int iCurrentStep, int iTotalSteps, bool fDisableCancel, out bool pfCanceled) => throw new NotImplementedException();

            int IVsThreadedWaitDialog2.HasCanceled(out bool pfCanceled) => throw new NotImplementedException();
        }
    }
}
