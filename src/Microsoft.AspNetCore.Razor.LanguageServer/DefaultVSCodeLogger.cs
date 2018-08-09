// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class DefaultVSCodeLogger : VSCodeLogger
    {
        private readonly ILanguageServer _router;
        private static string LastLogType = string.Empty;

        public DefaultVSCodeLogger(ILanguageServer router)
        {
            if (router == null)
            {
                throw new ArgumentNullException(nameof(router));
            }

            _router = router;
        }

        public override void Log(string message)
        {
            var messageBuilder = new StringBuilder();
            var typeName = GetType().Name;

            lock (LastLogType)
            {
                if (LastLogType != typeName)
                {
                    LastLogType = typeName;

                    messageBuilder
                        .AppendLine()
                        .AppendLine(typeName);
                }
            }

            messageBuilder
                .Append("  ")
                .Append(DateTime.Now.ToString("HH:mm:ss"))
                .Append("\t\t")
                .Append(message);

            _router.Window.LogMessage(new LogMessageParams()
            {
                Type = MessageType.Log,
                Message = messageBuilder.ToString()
            });
        }
    }
}
