// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class RazorCompletionHandler : LanguageServerHandlerBase, ICompletionHandler
    {
        private CompletionCapability _capability;

        public RazorCompletionHandler(ILanguageServer router) : base(router)
        {
        }

        public void SetCapability(CompletionCapability capability)
        {
            LogToClient("Setting capability");

            _capability = capability;
        }

        public Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
        {
            LogToClient("Handling completion request.");

            var completionItems = new List<CompletionItem>()
            {
                new CompletionItem()
                {
                    Label = "taylor",
                    InsertText = "taylor",
                    Detail = "The taylor directive.",
                    Documentation = "The taylor directive which is awesome.",
                    FilterText = "taylor",
                    SortText = "Taylor",
                    Kind = CompletionItemKind.Text
                }
            };

            var completionList = new CompletionList(completionItems, isIncomplete: true);

            return Task.FromResult(completionList);
        }

        public CompletionRegistrationOptions GetRegistrationOptions()
        {
            return new CompletionRegistrationOptions()
            {
                DocumentSelector = RazorDocument.Selector,
                ResolveProvider = true,
            };
        }
    }
}
