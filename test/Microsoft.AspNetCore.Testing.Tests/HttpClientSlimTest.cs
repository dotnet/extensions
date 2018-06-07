// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Testing
{
    public class HttpClientSlimTest
    {
        private static byte[] _defaultResponse = Encoding.ASCII.GetBytes("test");

        [Fact]
        public async Task GetStringAsyncHttp()
        {
            using (var host = StartHost(out var address))
            {
                Assert.Equal("test", await HttpClientSlim.GetStringAsync(address));
            }
        }

        [Fact]
        public async Task GetStringAsyncThrowsForErrorResponse()
        {
            using (var host = StartHost(out var address, statusCode: 500))
            {
                await Assert.ThrowsAnyAsync<HttpRequestException>(() => HttpClientSlim.GetStringAsync(address));
            }
        }

        [Fact]
        public async Task PostAsyncHttp()
        {
            using (var host = StartHost(out var address, handler: context => context.Request.InputStream.CopyToAsync(context.Response.OutputStream)))
            {
                Assert.Equal("test post", await HttpClientSlim.PostAsync(address, new StringContent("test post")));
            }
        }

        [Fact]
        public async Task PostAsyncThrowsForErrorResponse()
        {
            using (var host = StartHost(out var address, statusCode: 500))
            {
                await Assert.ThrowsAnyAsync<HttpRequestException>(
                    () => HttpClientSlim.PostAsync(address, new StringContent("")));
            }
        }

        [Fact]
        public void Ipv6ScopeIdsFilteredOut()
        {
            var requestUri = new Uri("http://[fe80::5d2a:d070:6fd6:1bac%7]:5003/");
            Assert.Equal("[fe80::5d2a:d070:6fd6:1bac]:5003", HttpClientSlim.GetHost(requestUri));
        }

        [Fact]
        public void GetHostExcludesDefaultPort()
        {
            var requestUri = new Uri("http://[fe80::5d2a:d070:6fd6:1bac%7]:80/");
            Assert.Equal("[fe80::5d2a:d070:6fd6:1bac]", HttpClientSlim.GetHost(requestUri));
        }

        private HttpListener StartHost(out string address, int statusCode = 200, Func<HttpListenerContext, Task> handler = null)
        {
            var listener = new HttpListener();

            address = $"http://127.0.0.1:{FindFreePort()}/";
            listener.Prefixes.Add(address);
            listener.Start();

            _ = listener.GetContextAsync().ContinueWith(async task =>
            {
                var context = task.Result;
                context.Response.StatusCode = statusCode;

                if (handler == null)
                {
                    await context.Response.OutputStream.WriteAsync(_defaultResponse, 0, _defaultResponse.Length);
                }
                else
                {
                    await handler(context);
                }

                context.Response.Close();
            });

            return listener;
        }

        private int FindFreePort()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                return ((IPEndPoint)socket.LocalEndPoint).Port;
            }
        }
    }
}
