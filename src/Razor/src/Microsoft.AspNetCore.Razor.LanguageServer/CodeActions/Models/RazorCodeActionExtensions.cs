// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions.Models
{
    internal static class RazorCodeActionExtensions
    {
        public static CommandOrCodeAction AsVSCodeCommandOrCodeAction(this RazorCodeAction razorCodeAction)
        {
            if (razorCodeAction.Data is null)
            {
                // No command data
                return new CommandOrCodeAction(razorCodeAction);
            }

            var serializedParams = JToken.FromObject(razorCodeAction.Data);
            var arguments = new JArray(serializedParams);

            return new CommandOrCodeAction(new Command
            {
                Title = razorCodeAction.Title ?? string.Empty,
                Name = LanguageServerConstants.RazorCodeActionRunnerCommand,
                Arguments = arguments
            });
        }

        public static RazorCodeAction WrapResolvableCSharpCodeAction(
            this RazorCodeAction razorCodeAction,
            RazorCodeActionContext context,
            string action = LanguageServerConstants.CodeActions.Default)
        {
            if (razorCodeAction is null)
            {
                throw new ArgumentNullException(nameof(razorCodeAction));
            }

            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var csharpParams = new CSharpCodeActionParams()
            {
                Data = razorCodeAction.Data,
                RazorFileUri = context.Request.TextDocument.Uri
            };

            razorCodeAction.Data = new RazorCodeActionResolutionParams()
            {
                Action = action,
                Language = LanguageServerConstants.CodeActions.Languages.CSharp,
                Data = csharpParams
            };

            return razorCodeAction;
        }
    }
}
