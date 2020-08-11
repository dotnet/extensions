// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.Feedback
{
    [Shared]
    [Export(typeof(FeedbackLogDirectoryProvider))]
    internal class DefaultFeedbackLogDirectoryProvider : FeedbackLogDirectoryProvider
    {
        private const string FeedbackDirectoryName = "RazorVSFeedbackLogs";
        private readonly object _accessLock = new object();
        private string _baseLogDirectory;
        private string _logDirectory;

        public override bool DirectoryCreated => _logDirectory != null;

        public override string GetDirectory()
        {
            // Start cleaning up old directories in the background. Fire and forget
            _ = Task.Run(() => CleanupOldDirectories());

            lock (_accessLock)
            {
                if (_logDirectory == null)
                {
                    EnsureBaseLogDirectory();

                    var processId = Process.GetCurrentProcess().Id;

                    // There could be multiple versions of VS running, lets ensure that we get our own directory.
                    var logDirectory = Path.Combine(_baseLogDirectory, processId.ToString(CultureInfo.InvariantCulture));
                    Directory.CreateDirectory(logDirectory);

                    // In the end the log directory looks something like C:\Users\nimullen\AppData\Local\Temp\RazorVSFeedbackLogs\12345

                    // Assign after creation so it's attempted again if the directory creation fails.
                    _logDirectory = logDirectory;
                }
            }

            return _logDirectory;
        }

        private void EnsureBaseLogDirectory()
        {
            lock (_accessLock)
            {
                if (_baseLogDirectory == null)
                {
                    var tempDirectory = Path.GetTempPath();
                    var baseLogDirectory = Path.Combine(tempDirectory, FeedbackDirectoryName);
                    if (!Directory.Exists(_baseLogDirectory))
                    {
                        Directory.CreateDirectory(baseLogDirectory);
                    }

                    // In the end the base directory looks something like C:\Users\nimullen\AppData\Local\Temp\RazorVSFeedbackLogs

                    // Assign after creation so it's attempted again if the directory creation fails.
                    _baseLogDirectory = baseLogDirectory;
                }
            }
        }

        private void CleanupOldDirectories()
        {
            try
            {
                EnsureBaseLogDirectory();

                var directories = Directory.GetDirectories(_baseLogDirectory);
                for (var i = 0; i < directories.Length; i++)
                {
                    var directory = directories[i];
                    var processIdString = Path.GetFileName(directory);

                    // Logs are grouped by process ID as the folder name.
                    if (!int.TryParse(processIdString, out var logProcessId))
                    {
                        continue;
                    }

                    var devenvProcesses = Process.GetProcessesByName("devenv");

                    // Given we're runnin in VS there will always be at least 1 devenv process.

                    var logsAssociatedProcess = devenvProcesses.FirstOrDefault(process => process.Id == logProcessId);
                    if (logsAssociatedProcess != null)
                    {
                        // Devenv for that log is still running, don't touch it
                        continue;
                    }

                    // Stale log, no associated devenv. Delete it.
                    Directory.Delete(directory, recursive: true);
                }
            }
            catch (Exception)
            {
                // Swallow all exceptions when cleaning up. We're doing our best to not leave big disk footprints in the users temp folder; however, if this time we fail
                // we'll try again next time.
            }
        }
    }
}
