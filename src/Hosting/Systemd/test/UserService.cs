// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace Microsoft.Extensions.Hosting
{
    class UserService : IDisposable
    {
        private static readonly Random Random = new Random();
        private readonly string _name;
        private readonly string _type;
        private readonly string _dotnetPath;
        private readonly string _applicationAssemblyPath;
        private readonly string _syslogIdentifier;

        public UserService(string name, string type, string dotnetPath, string applicationAssemblyPath)
        {
            _name = name;
            _type = type;
            _dotnetPath = dotnetPath;
            _applicationAssemblyPath = applicationAssemblyPath;
            lock (Random)
            {
                // Unique identifier for retrieving log
                _syslogIdentifier = "dotnet" + Random.Next();
            }
        }

        public void Start()
        {
            // In case the service wasn't properly stopped in a previous test run.
            TryStop();

            WriteUnitFile();
            RunProcess("systemctl", "daemon-reload");
            RunProcess("systemctl", $"start {_name}");
        }

        public string GetLog()
        {
            return RunProcess("journalctl", $"-t {_syslogIdentifier}");
        }

        private void WriteUnitFile()
        {
            StringBuilder unit = new StringBuilder();
            unit.AppendLine("[Service]");
            unit.AppendLine($"Type={_type}");
            unit.AppendLine($"WorkingDirectory={Path.GetDirectoryName(_applicationAssemblyPath)}");
            unit.AppendLine($"ExecStart='{_dotnetPath}' '{_applicationAssemblyPath}'");
            unit.AppendLine($"SyslogIdentifier={_syslogIdentifier}");
            unit.AppendLine($"Environment=ASPNETCORE_URLS=http://*:0"); // bind to a free port

            File.WriteAllText(UnitFilePath, unit.ToString());
        }

        public void Dispose()
        {
            TryStop();

            try
            {
                File.Delete(UnitFilePath);
            }
            catch
            {}
        }

        public void Stop()
        {
            RunProcess("systemctl", $"stop {_name}");
        }

        private void TryStop()
        {
            try
            {
                Stop();
            }
            catch
            {}
        }

        private string UnitFilePath
            => Path.Combine(UserUnitFolder, $"{_name}.service");

        private string UserUnitFolder
        {
            get
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ".config/systemd/user");
                Directory.CreateDirectory(folder);
                return folder;
            }
        }

        private static string RunProcess(string filename, string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = filename,
                Arguments = $"--user {arguments}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            using var process = Process.Start(startInfo);
            string stdout = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new Exception(process.StandardError.ReadToEnd());
            }

            return stdout;
        }
    }
}
