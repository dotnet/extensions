// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Http.Telemetry.Tracing.Test.Internal;

// Originally taken from https://github.com/open-telemetry/opentelemetry-dotnet/blob/3d6be9cb5770f9f1e46478dccfec18e2ec05f828/test/OpenTelemetry.Tests/Shared/TestHttpServer.cs
internal class TestHttpServer
{
    public static IDisposable RunServerOrThrow(Action<HttpListenerContext> action, out string host, out int port)
    {
        host = "localhost";
        port = 0;
        RunningServer? server = null;

        var retryCount = 5;
        while (retryCount > 0)
        {
            try
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Bind(new IPEndPoint(IPAddress.Any, 0));
                    port = ((IPEndPoint)socket.LocalEndPoint!).Port;
                }

                server = new RunningServer(action, host, port);
                server.Start();
                break;
            }
            catch (HttpListenerException)
            {
                retryCount--;
            }
        }

        if (server is null)
        {
            throw new InvalidOperationException($"Retry count of {retryCount} was reached while trying to run the test server");
        }

        return server;
    }

    private class RunningServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly Action<HttpListenerContext> _action;
        private readonly AutoResetEvent _initialized = new(initialState: false);
        private Task _httpListenerTask = Task.CompletedTask;

        public RunningServer(Action<HttpListenerContext> action, string host, int port)
        {
            _action = action;
            _listener = new HttpListener();

            _listener.Prefixes.Add($"http://{host}:{port}/");
            _listener.Start();
        }

        private async Task RunListenerTask()
        {
            while (true)
            {
                try
                {
                    var ctxTask = _listener.GetContextAsync();

                    _initialized.Set();

                    _action(await ctxTask.ConfigureAwait(false));
                }
                catch (ObjectDisposedException)
                {
                    // Listener was closed before we got into GetContextAsync
                    break;
                }
                catch (HttpListenerException ex) when (ex.ErrorCode == 995)
                {
                    // Listener was closed while we were in GetContextAsync.
                    break;
                }
            }
        }

        public void Start()
        {
            _httpListenerTask = RunListenerTask();
            _initialized.WaitOne();
        }

        [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "test helper")]
        public void Dispose()
        {
            try
            {
                _listener.Close();
                _httpListenerTask.Wait();
                _initialized.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // swallow this exception just in case
            }
        }
    }
}
