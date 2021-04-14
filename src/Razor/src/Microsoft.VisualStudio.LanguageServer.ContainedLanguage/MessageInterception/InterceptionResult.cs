// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Newtonsoft.Json.Linq;

#nullable enable

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage.MessageInterception
{
    /// <summary>
    /// Contains an updated message token and a signal of whether the document Uri was changed.
    /// </summary>
    public struct InterceptionResult
    {
        public InterceptionResult(JToken? newToken, bool changedDocumentUri)
        {
            UpdatedToken = newToken;
            ChangedDocumentUri = changedDocumentUri;
        }

        public JToken? UpdatedToken { get; }
        public bool ChangedDocumentUri { get; }
    }
}
