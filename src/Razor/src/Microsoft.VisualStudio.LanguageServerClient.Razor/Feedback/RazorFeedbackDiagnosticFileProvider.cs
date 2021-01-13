// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.Internal.VisualStudio.Shell.Embeddable.Feedback;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.Feedback
{
    [Shared]
    [Export(typeof(IFeedbackDiagnosticFileProvider))]
    internal class RazorFeedbackDiagnosticFileProvider : IFeedbackDiagnosticFileProvider
    {
        private readonly FeedbackLogDirectoryProvider _feedbackLogDirectoryProvider;
        private readonly JoinableTaskFactory _joinableTaskFactory;

        [ImportingConstructor]
        public RazorFeedbackDiagnosticFileProvider(
            JoinableTaskContext joinableTaskContext,
            FeedbackLogDirectoryProvider feedbackLogDirectoryProvider)
        {
            if (joinableTaskContext is null)
            {
                throw new ArgumentNullException(nameof(joinableTaskContext));
            }

            if (feedbackLogDirectoryProvider is null)
            {
                throw new ArgumentNullException(nameof(feedbackLogDirectoryProvider));
            }

            _feedbackLogDirectoryProvider = feedbackLogDirectoryProvider;
            _joinableTaskFactory = joinableTaskContext.Factory;
        }

        public IReadOnlyCollection<string> GetFiles()
        {
            if (!_feedbackLogDirectoryProvider.DirectoryCreated)
            {
                // No one requested to create any feedback logs, no reason for us to provide any logs.
                return Array.Empty<string>();
            }

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture);
            var zipFileName = $"RazorLogs_{timestamp}.zip";
            var zipFilePath = Path.Combine(Path.GetTempPath(), zipFileName);
            var logDirectory = _feedbackLogDirectoryProvider.GetDirectory();
            if (!Directory.Exists(logDirectory))
            {
                // This should never be the case, just being extra defensive.
                return Array.Empty<string>();
            }

            _joinableTaskFactory.RunAsync(() => ZipLogsAsync(logDirectory, zipFilePath));

            return new[] { zipFilePath };
        }

        private static async Task ZipLogsAsync(string logs, string zipFilePath)
        {
            // Because we still have a handle to the log files, we can't zip them. So copy them and then zip the copy
            var copiedLogs = CopyLogsToDirectory(logs);

            // Start another task to zip this in the background.
            await Task.Run(() => ZipFile.CreateFromDirectory(copiedLogs, zipFilePath));

            try
            {
                if (Directory.Exists(copiedLogs))
                {
                    Directory.Delete(copiedLogs, true);
                }
            }
            catch (Exception)
            {
                // Swallow any cleanup exceptions, we did our best.
            }
        }

        private static string CopyLogsToDirectory(string logs)
        {
            var copyLocation = logs + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture);
            if (!Directory.Exists(copyLocation))
            {
                Directory.CreateDirectory(copyLocation);
                var dirInfo = new DirectoryInfo(logs);
                foreach (var file in dirInfo.GetFiles())
                {
                    try
                    {
                        var toFilepath = Path.Combine(copyLocation, file.Name);
                        file.CopyTo(toFilepath);
                    }
                    catch (Exception)
                    {
                        // If we can't copy a log file, indicate that there was one that failed to copy
                        var failedFilePath = Path.Combine(copyLocation, "FAILED_" + file.Name);
                        using var failedFile = File.Create(failedFilePath);
                    }
                }
            }

            return copyLocation;
        }
    }
}
