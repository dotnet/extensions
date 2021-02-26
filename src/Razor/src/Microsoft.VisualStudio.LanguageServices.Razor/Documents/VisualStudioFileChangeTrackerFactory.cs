// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Editor.Razor.Documents
{
    internal class VisualStudioFileChangeTrackerFactory : FileChangeTrackerFactory
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly ErrorReporter _errorReporter;
        private readonly JoinableTaskContext _joinableTaskContext;
        private readonly IVsAsyncFileChangeEx _fileChangeService;

        public VisualStudioFileChangeTrackerFactory(
            ForegroundDispatcher foregroundDispatcher,
            ErrorReporter errorReporter,
            IVsAsyncFileChangeEx fileChangeService,
            JoinableTaskContext joinableTaskContext)
        {
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

            if (joinableTaskContext is null)
            {
                throw new ArgumentNullException(nameof(joinableTaskContext));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _errorReporter = errorReporter;
            _joinableTaskContext = joinableTaskContext;
            _fileChangeService = fileChangeService;
        }

        public override FileChangeTracker Create(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(filePath));
            }

            var fileChangeTracker = new VisualStudioFileChangeTracker(filePath, _foregroundDispatcher, _errorReporter, _fileChangeService, _joinableTaskContext.Factory);
            return fileChangeTracker;
        }
    }
}
