// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Editor.Razor.Documents
{
    internal class VisualStudioFileChangeTracker : FileChangeTracker, IVsFreeThreadedFileChangeEvents2
    {
        private const _VSFILECHANGEFLAGS FileChangeFlags = _VSFILECHANGEFLAGS.VSFILECHG_Time | _VSFILECHANGEFLAGS.VSFILECHG_Size | _VSFILECHANGEFLAGS.VSFILECHG_Del | _VSFILECHANGEFLAGS.VSFILECHG_Add;

        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly ErrorReporter _errorReporter;
        private readonly IVsAsyncFileChangeEx _fileChangeService;
        private readonly JoinableTaskFactory _joinableTaskFactory;

        // Internal for testing
        internal JoinableTask<uint> _fileChangeAdviseTask;
        internal JoinableTask _fileChangeUnadviseTask;
        internal JoinableTask _fileChangedTask;

        public override event EventHandler<FileChangeEventArgs> Changed;

        public VisualStudioFileChangeTracker(
            string filePath,
            ForegroundDispatcher foregroundDispatcher,
            ErrorReporter errorReporter,
            IVsAsyncFileChangeEx fileChangeService,
            JoinableTaskFactory joinableTaskFactory)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(filePath));
            }

            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (errorReporter == null)
            {
                throw new ArgumentNullException(nameof(errorReporter));
            }

            if (fileChangeService == null)
            {
                throw new ArgumentNullException(nameof(fileChangeService));
            }

            FilePath = filePath;
            _foregroundDispatcher = foregroundDispatcher;
            _errorReporter = errorReporter;
            _fileChangeService = fileChangeService;
            _joinableTaskFactory = joinableTaskFactory ?? throw new ArgumentNullException();
        }

        public override string FilePath { get; }

        public override void StartListening()
        {
            _foregroundDispatcher.AssertForegroundThread();

            if (_fileChangeUnadviseTask?.IsCompleted == false)
            {
                // An unadvise operation is still processing, block the foreground thread until it completes.
                _fileChangeUnadviseTask.Join();
            }

            if (_fileChangeAdviseTask != null)
            {
                // Already listening
                return;
            }

            _fileChangeAdviseTask = _joinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    return await _fileChangeService.AdviseFileChangeAsync(FilePath, FileChangeFlags, this).ConfigureAwait(true);
                }
                catch (PathTooLongException)
                {
                    // Don't report PathTooLongExceptions but don't fault either.
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception exception)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    // Don't explode on actual exceptions, just report gracefully.
                    _errorReporter.ReportError(exception);
                }

                return VSConstants.VSCOOKIE_NIL;
            });
        }

        public override void StopListening()
        {
            _foregroundDispatcher.AssertForegroundThread();

            if (_fileChangeAdviseTask == null || _fileChangeUnadviseTask?.IsCompleted == false)
            {
                // Already not listening or trying to stop listening
                return;
            }

            _fileChangeUnadviseTask = _joinableTaskFactory.RunAsync(async () =>
            {
                try
                {
                    var fileChangeCookie = await _fileChangeAdviseTask;

                    if (fileChangeCookie == VSConstants.VSCOOKIE_NIL)
                    {
                        // Wasn't able to listen for file change events. This typically happens when some sort of exception (i.e. access exceptions)
                        // is thrown when attempting to listen for file changes.
                        return;
                    }

                    await _fileChangeService.UnadviseFileChangeAsync(fileChangeCookie).ConfigureAwait(true);
                    _fileChangeAdviseTask = null;
                }
                catch (PathTooLongException)
                {
                    // Don't report PathTooLongExceptions but don't fault either.
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception exception)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    // Don't explode on actual exceptions, just report gracefully.
                    _errorReporter.ReportError(exception);
                }
            });
        }

        public int FilesChanged(uint fileCount, string[] filePaths, uint[] fileChangeFlags)
        {
            // Capturing task for testing purposes
            _fileChangedTask = _joinableTaskFactory.RunAsync(async () =>
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();

                foreach (var fileChangeFlag in fileChangeFlags)
                {
                    var fileChangeKind = FileChangeKind.Changed;
                    var changeFlag = (_VSFILECHANGEFLAGS)fileChangeFlag;
                    if ((changeFlag & _VSFILECHANGEFLAGS.VSFILECHG_Del) == _VSFILECHANGEFLAGS.VSFILECHG_Del)
                    {
                        fileChangeKind = FileChangeKind.Removed;
                    }
                    else if ((changeFlag & _VSFILECHANGEFLAGS.VSFILECHG_Add) == _VSFILECHANGEFLAGS.VSFILECHG_Add)
                    {
                        fileChangeKind = FileChangeKind.Added;
                    }

                    // Purposefully not passing through the file paths here because we know this change has to do with this trackers FilePath.
                    // We use that FilePath instead so any path normalization the file service did does not impact callers.
                    OnChanged(fileChangeKind);
                }
            });

            return VSConstants.S_OK;
        }

        public int DirectoryChanged(string pszDirectory) => VSConstants.S_OK;

        public int DirectoryChangedEx(string pszDirectory, string pszFile) => VSConstants.S_OK;

        public int DirectoryChangedEx2(string pszDirectory, uint cChanges, string[] rgpszFile, uint[] rggrfChange) => VSConstants.S_OK;

        private void OnChanged(FileChangeKind changeKind)
        {
            _foregroundDispatcher.AssertForegroundThread();

            if (Changed == null)
            {
                return;
            }

            var args = new FileChangeEventArgs(FilePath, changeKind);
            Changed.Invoke(this, args);
        }
    }
}
