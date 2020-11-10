// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Razor.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Serialization
{
    public class ExtendableClientCapabilitiesJsonConverter : JsonConverter
    {
        public static readonly ExtendableClientCapabilitiesJsonConverter Instance = new ExtendableClientCapabilitiesJsonConverter();

        public override bool CanRead => true;

        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType)
        {
            return typeof(ClientCapabilities).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartObject)
            {
                return null;
            }

            var capabilities = new ExtendableClientCapabilities();

            reader.ReadProperties(propertyName =>
            {
                switch (propertyName)
                {
                    case "workspace":
                        if (reader.Read())
                        {
                            var obj = JObject.Load(reader);
                            capabilities.Workspace = obj.ToObject<WorkspaceClientCapabilities>(serializer);
                        }
                        break;
                    case "textDocument":
                        if (reader.Read())
                        {
                            var obj = JObject.Load(reader);
                            capabilities.TextDocument = obj.ToObject<TextDocumentClientCapabilities>(serializer);
                        }
                        break;
                    case "experimental":
                        if (reader.Read())
                        {
                            var obj = JObject.Load(reader);
                            capabilities.Experimental = obj.ToObject<IDictionary<string, JToken>>(serializer);
                        }
                        break;
                    case "supportsCodeActionResolve":
                        capabilities.SupportsCodeActionResolve = reader.ReadAsBoolean().GetValueOrDefault();
                        break;
                }
            });

            return capabilities;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }
    }
}

