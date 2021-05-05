// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

#nullable enable

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage.MessageInterception
{
    public abstract class InterceptorManager
    {
        /// <summary>
        /// Returns whether there is an interceptor available for the given message name.
        /// </summary>
        public abstract bool HasInterceptor(string messageName);

        /// <summary>
        /// Takes a message token and returns it with any transforms applied.  To block the message completely, return null.
        /// </summary>
        /// <param name="methodName">The LSP method being intercepted</param>
        /// <param name="message">The LSP message payload</param>
        /// <param name="sourceLanguageName">The content type name of the contained language where the message originated</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The message token with any applicable modifications, or null to block the message.</returns>
        public abstract Task<JToken?> ProcessInterceptorsAsync(string methodName, JToken message, string sourceLanguageName, CancellationToken cancellationToken);
    }
}
