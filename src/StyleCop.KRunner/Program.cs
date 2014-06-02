// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;

namespace StyleCop.KRunner
{
    public class Program
    {
        private static readonly List<Violation> _violations = new List<Violation>();

        public static int Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("StyleCop.KRunner - A StyleCop commandline runner");
            Console.ResetColor();

            if (args.Length == 0)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("\tStyleCop.KRunner <path/to/project.json>");
                Console.WriteLine();
                return -1;
            }

            var projectFile = args[0];
            if (!File.Exists(projectFile))
            {
                Console.WriteLine("File '{0}' does not exist.", Path.GetFullPath(projectFile));
                return -2;
            }

            var runner = new StyleCopConsole(
                settings: null,
                writeResultsCache:
                false,
                outputFile: null,
                addInPaths: null,
                loadFromDefaultPath: true); // Loads rules next to StyleCop.dll in the file system.

            var projectDirectory = Path.GetDirectoryName(projectFile);

            // It's OK if the settings file is null.
            var settingsFile = FindSettingsFile(projectDirectory);

            var project = new CodeProject(0, projectDirectory, new Configuration(null));
            foreach (var file in Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories))
            {
                runner.Core.Environment.AddSourceCode(project, file, context: null);
            }

            try
            {
                runner.OutputGenerated += Runner_OutputGenerated;
                runner.ViolationEncountered += Runner_ViolationEncountered;

                if (settingsFile == null)
                {
                    runner.Core.Analyze(new CodeProject[] { project });
                }
                else
                {
                    runner.Core.Analyze(new CodeProject[] { project }, settingsFile);
                }
            }
            finally
            {
                runner.OutputGenerated -= Runner_OutputGenerated;
                runner.ViolationEncountered -= Runner_ViolationEncountered;
            }

            if (_violations.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }

            Console.WriteLine();
            Console.WriteLine("Finished Processing: {0}", projectFile);
            Console.WriteLine("{0} errors found.", _violations.Count);
            Console.WriteLine();

            Console.ResetColor();

            return _violations.Count;
        }

        // Performs an ascending directory search starting at the project directory
        private static string FindSettingsFile(string projectDirectory)
        {
            var current = new DirectoryInfo(projectDirectory);
            var root = current.Root;

            do
            {
                var settingsFile = Path.Combine(current.FullName, "Settings.StyleCop");
                if (File.Exists(settingsFile))
                {
                    return settingsFile;
                }
            }
            while ((current = current.Parent) != null);

            return null;
        }

        private static void Runner_ViolationEncountered(object sender, ViolationEventArgs e)
        {
            Console.WriteLine("{0}: {1} Line {2} - {3}", e.Violation.Rule.CheckId, e.SourceCode.Path, e.LineNumber, e.Message);

            _violations.Add(e.Violation);
        }

        private static void Runner_OutputGenerated(object sender, OutputEventArgs e)
        {
            // There will be a bunch of message like "processing file Bleh.cs" with low importance,
            // we're intentionally excluding those.
            if (e.Importance != MessageImportance.Low)
            {
                Console.WriteLine(e.Output);
            }
        }
    }
}
