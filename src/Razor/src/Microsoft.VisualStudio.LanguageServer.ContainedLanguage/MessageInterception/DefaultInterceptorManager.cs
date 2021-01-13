// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

#nullable enable

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage.MessageInterception
{
    [Export(typeof(InterceptorManager))]
    internal sealed class DefaultInterceptorManager : InterceptorManager
    {
        private readonly IReadOnlyList<Lazy<MessageInterceptor, IInterceptMethodMetadata>> _lazyInterceptors;

        [ImportingConstructor]
        public DefaultInterceptorManager([ImportMany] IEnumerable<Lazy<MessageInterceptor, IInterceptMethodMetadata>> lazyInterceptors)
        {
            _ = lazyInterceptors ?? throw new ArgumentNullException(nameof(lazyInterceptors));
            _lazyInterceptors = lazyInterceptors.ToList().AsReadOnly();
        }

        public override bool HasInterceptor(string methodName)
        {
            if (string.IsNullOrEmpty(methodName))
            {
                throw new ArgumentException("Cannot be empty", nameof(methodName));
            }

            foreach (var interceptor in _lazyInterceptors)
            {
                foreach (var method in interceptor.Metadata.InterceptMethods)
                {
                    if (method.Equals(methodName, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public override async Task<JToken?> ProcessInterceptorsAsync(string methodName, JToken message, string sourceLanguageName, CancellationToken cancellationToken)
        {
            _ = message ?? throw new ArgumentNullException(nameof(message));
            if (string.IsNullOrEmpty(methodName))
            {
                throw new ArgumentException("Cannot be empty", nameof(methodName));
            }
            if (string.IsNullOrEmpty(sourceLanguageName))
            {
                throw new ArgumentException("Cannot be empty", nameof(sourceLanguageName));
            }

            for (var i = 0; i < _lazyInterceptors.Count; i++)
            {
                var interceptor = _lazyInterceptors[i];
                if (CanInterceptMessage(methodName, interceptor.Metadata.InterceptMethods))
                {
                    var result = await interceptor.Value.ApplyChangesAsync(message, sourceLanguageName, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    if (result.UpdatedToken is null)
                    {
                        // The interceptor has blocked this message
                        return null;
                    }

                    message = result.UpdatedToken;

                    if (result.ChangedDocumentUri)
                    {
                        // If the DocumentUri changes, we need to restart the loop
                        i = -1;
                        continue;
                    }
                }
            }

            return message;

            static bool CanInterceptMessage(string methodName, IEnumerable<string> handledMessages)
            {
                return handledMessages.Any(m => methodName.Equals(m, StringComparison.Ordinal));
            }
        }
    }
}
