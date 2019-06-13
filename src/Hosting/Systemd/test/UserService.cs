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
            ProcessHelper.RunProcess("systemctl", "--user daemon-reload");
            ProcessHelper.RunProcess("systemctl", $"--user start {_name}");
        }

        public string GetLog()
        {
            return ProcessHelper.RunProcess("journalctl", $"--user -t {_syslogIdentifier}");
        }

        public bool IsActive()
        {
            try
            {
                ProcessHelper.RunProcess("systemctl", $"--user is-active {_name}");
                return true;
            }
            catch
            {
                return false;
            }
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
            ProcessHelper.RunProcess("systemctl", $"--user stop {_name}");
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
    }
}
