// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
{
    public abstract class VirtualDocumentBase<T> : VirtualDocument where T : VirtualDocumentSnapshot
    {
        private T _currentSnapshot;
        private long? _hostDocumentSyncVersion;

        protected VirtualDocumentBase(Uri uri, ITextBuffer textBuffer)
        {
            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (textBuffer is null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }

            Uri = uri;
            TextBuffer = textBuffer;
            _currentSnapshot = GetUpdatedSnapshot();
        }

        public override Uri Uri { get; }

        public override ITextBuffer TextBuffer { get; }

        public override long? HostDocumentSyncVersion => _hostDocumentSyncVersion;

        public override VirtualDocumentSnapshot CurrentSnapshot => _currentSnapshot;

        public override VirtualDocumentSnapshot Update(IReadOnlyList<ITextChange> changes, long hostDocumentVersion)
        {
            if (changes is null)
            {
                throw new ArgumentNullException(nameof(changes));
            }

            _hostDocumentSyncVersion = hostDocumentVersion;
            TextBuffer.SetHostDocumentSyncVersion(_hostDocumentSyncVersion.Value);

            if (changes.Count == 0)
            {
                // Even though nothing changed here, we want the synchronizer to be aware of the host document version change.
                // So, let's make an empty edit to invoke the text buffer Changed events.
                TextBuffer.MakeEmptyEdit();

                _currentSnapshot = GetUpdatedSnapshot();
                return _currentSnapshot;
            }

            using var edit = TextBuffer.CreateEdit(EditOptions.None, reiteratedVersionNumber: null, InviolableEditTag.Instance);
            for (var i = 0; i < changes.Count; i++)
            {
                var change = changes[i];
                edit.Replace(change.OldSpan.Start, change.OldSpan.Length, change.NewText);
            }

            edit.Apply();
            _currentSnapshot = GetUpdatedSnapshot();

            return _currentSnapshot;
        }

        protected abstract T GetUpdatedSnapshot();

        public override void Dispose()
        {
            TextBuffer.ChangeContentType(InertContentType.Instance, null);

            if (TextBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument textDocument))
            {
                TextBuffer.Properties.RemoveProperty(typeof(ITextDocument));

                try
                {
                    textDocument.Dispose();
                }
                catch
                {
                    // Eat the exception for now while we are investigating an issue.
                    // There is System.OperationCanceledException: 'Project unload has already occurred or begun.'
                    // that gets thrown if Razor file is open when you are shutting down VS at
                    // Microsoft.VisualStudio.ProjectSystem.ProjectAsynchronousTasksServiceBase.RegisterAsyncTask(Microsoft.VisualStudio.Threading.JoinableTask, Microsoft.VisualStudio.ProjectSystem.ProjectCriticalOperation, bool)
                    // Microsoft.VisualStudio.ProjectSystem.VS.Implementation.CodeGenerators.GeneratorScheduler.ScheduleFileGeneration(Microsoft.VisualStudio.ProjectSystem.VS.Implementation.CodeGenerators.IGeneratorSchedulerRequest)
                    // Microsoft.VisualStudio.ProjectSystem.VS.Implementation.CodeGenerators.SingleFileGeneratorsService.ScheduleRefreshGeneratedFile(string)
                    // Microsoft.VisualStudio.ProjectSystem.VS.Implementation.CodeGenerators.SingleFileGeneratorsService.TextDocumentFactoryService_TextDocumentDisposed(object, Microsoft.VisualStudio.Text.TextDocumentEventArgs)
                    // Microsoft.VisualStudio.Text.Implementation.TextDocumentFactoryService.RaiseTextDocumentDisposed(Microsoft.VisualStudio.Text.ITextDocument)
                    // Microsoft.VisualStudio.Text.Implementation.TextDocument.Dispose()
                    // Microsoft.VisualStudio.LanguageServer.ContainedLanguage.VirtualDocumentBase<T>.Dispose() in VirtualDocumentBase
                }
            }
        }
    }
}
