// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    /// <summary>
    /// Defines an attribute for LSP request handlers to map to LSP methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class), MetadataAttribute]
    internal class ExportLspMethodAttribute : ExportAttribute, IRequestHandlerMetadata
    {
        public string MethodName { get; }

        public ExportLspMethodAttribute(string methodName) : base(typeof(IRequestHandler))
        {
            if (methodName is null)
            {
                throw new ArgumentNullException(nameof(methodName));
            }

            MethodName = methodName;
        }
    }
}
