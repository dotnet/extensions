// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    public abstract class RazorLanguageClientMiddleLayer : ILanguageClientMiddleLayer
    {
        public abstract bool CanHandle(string methodName);

        public abstract Task HandleNotificationAsync(string methodName, JToken methodParam, Func<JToken, Task> sendNotification);

        public abstract Task<JToken> HandleRequestAsync(string methodName, JToken methodParam, Func<JToken, Task<JToken>> sendRequest);
    }
}
