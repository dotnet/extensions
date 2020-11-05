// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

#nullable enable

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage.MessageInterception
{
    /// <summary>
    /// Intercepts an LSP message and applies changes to the payload.
    /// </summary>
    public abstract class MessageInterceptor
    {
        /// <summary>
        /// Applies changes to the message token, and signals if the document path has been changed.
        /// </summary>
        /// <param name="message">The message payload</param>
        /// <param name="containedLanguageName">The name of the content type for the contained language.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns></returns>
        public abstract Task<InterceptionResult> ApplyChangesAsync(JToken message, string containedLanguageName, CancellationToken cancellationToken);
    }
}
