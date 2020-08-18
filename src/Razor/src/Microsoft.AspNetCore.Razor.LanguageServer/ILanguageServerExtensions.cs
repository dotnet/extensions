// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Server;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public static class ILanguageServerExtensions
    {
        public static Task InitializedAsync(this ILanguageServer languageServer, CancellationToken cancellationToken)
        {
            var type = typeof(OmniSharp.Extensions.LanguageServer.Server.LanguageServer);
            var method = type.GetMethod("Initialize", BindingFlags.NonPublic | BindingFlags.Instance);
            var task = (Task)method.Invoke(languageServer, new object[] { cancellationToken });

            return task;
        }
    }
}
