// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    [Export(typeof(ILanguageClient))]
    [ContentType(RazorLSPContentTypeDefinition.Name)]
    internal class RazorLanguageServerClient : ILanguageClient
    {
        public string Name => "Razor Language Server Client";

        public IEnumerable<string> ConfigurationSections => null;

        public object InitializationOptions => null;

        public IEnumerable<string> FilesToWatch => null;

        public event AsyncEventHandler<EventArgs> StartAsync;
        public event AsyncEventHandler<EventArgs> StopAsync
        {
            add { }
            remove { }
        }

        public async Task<Connection> ActivateAsync(CancellationToken token)
        {
            await Task.Yield();

            var currentAssembly = typeof(RazorLanguageServerClient).Assembly;
            var currentAssemblyLocation = currentAssembly.Location;
            var extensionDirectory = Path.GetDirectoryName(currentAssemblyLocation);
            var languageServerPath = Path.Combine(extensionDirectory, "LanguageServer", "rzls.exe");
            var info = new ProcessStartInfo();
            info.FileName = languageServerPath;
            info.Arguments = "-lsp --trace Verbose";
            info.RedirectStandardInput = true;
            info.RedirectStandardOutput = true;
            info.UseShellExecute = false;
            info.CreateNoWindow = true;

            Process process = new Process();
            process.StartInfo = info;

            if (process.Start())
            {
                return new Connection(process.StandardOutput.BaseStream, process.StandardInput.BaseStream);
            }

            return null;
        }

        public async Task OnLoadedAsync()
        {
            await StartAsync.InvokeAsync(this, EventArgs.Empty);
        }

        public Task OnServerInitializeFailedAsync(Exception e)
        {
            return Task.CompletedTask;
        }

        public Task OnServerInitializedAsync()
        {
            return Task.CompletedTask;
        }
    }
}
