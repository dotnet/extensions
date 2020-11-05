// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Razor.LanguageServer.Serialization;
using Newtonsoft.Json;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Refactoring.Test
{
    public class ExtendableClientCapabilitiesJsonConverterTest
    {
        [Fact]
        public void ReadJson_ReadsValues()
        {
            // Arrange
            // Note this is a small subset of the actual ClientCapabilities provided
            // for use in basic validations.
            var rawJson = @"{
  ""supportsCodeActionResolve"": true,
  ""workspace"": {
    ""applyEdit"": true,
    ""workspaceEdit"": {
      ""documentChanges"": true
    }
  },
  ""textDocument"": {
    ""_ms_onAutoInsert"": {
      ""dynamicRegistration"": false
    },
    ""synchronization"": {
      ""willSave"": false,
      ""willSaveWaitUntil"": false,
      ""didSave"": true,
      ""dynamicRegistration"": false
    },
    ""completion"": {
      ""completionItem"": {
        ""snippetSupport"": false,
        ""commitCharactersSupport"": true
      },
      ""completionItemKind"": {
        ""valueSet"": [
          3
        ]
      },
      ""contextSupport"": false,
      ""dynamicRegistration"": false
    },
    ""hover"": {
      ""contentFormat"": [
        ""plaintext""
      ],
      ""dynamicRegistration"": false
    },
    ""signatureHelp"": {
      ""signatureInformation"": {
        ""documentationFormat"": [
          ""plaintext""
        ]
      },
      ""contextSupport"": true,
      ""dynamicRegistration"": false
    },
    ""codeAction"": {
      ""codeActionLiteralSupport"": {
        ""codeActionKind"": {
          ""valueSet"": [
            ""refactor.extract""
          ]
        }
      },
      ""dynamicRegistration"": false
    }
  }
}";
            var stringReader = new StringReader(rawJson);
            var serializer = OmniSharp.Extensions.LanguageServer.Protocol.Serialization.Serializer.Instance.JsonSerializer;
            serializer.Converters.Add(ExtendableClientCapabilitiesJsonConverter.Instance);

            // Act
            var capabilities = serializer.Deserialize<ExtendableClientCapabilities>(new JsonTextReader(stringReader));

            // Assert
            Assert.True(capabilities.SupportsCodeActionResolve);
            Assert.True(capabilities.Workspace.ApplyEdit);
            Assert.Equal(MarkupKind.PlainText, capabilities.TextDocument.Hover.Value.ContentFormat.First());
            Assert.Equal(CompletionItemKind.Function, capabilities.TextDocument.Completion.Value.CompletionItemKind.ValueSet.First());
            Assert.Equal(MarkupKind.PlainText, capabilities.TextDocument.SignatureHelp.Value.SignatureInformation.DocumentationFormat.First());
            Assert.Equal(CodeActionKind.RefactorExtract, capabilities.TextDocument.CodeAction.Value.CodeActionLiteralSupport.CodeActionKind.ValueSet.First());
        }
    }
}
