// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Newtonsoft.Json;
using OmniSharp.Extensions.JsonRpc.Client;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Converters
{
    // This is a temporary workaround for https://github.com/OmniSharp/csharp-language-server-protocol/issues/202
    // The fix was not available on a non-alpha release, but this can be reverted once it is.
    internal class ResponseRazorConverter : JsonConverter<Response>
    {
        public override bool CanRead => false;
        public override Response ReadJson(JsonReader reader, Type objectType, Response existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, Response value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("jsonrpc");
            writer.WriteValue("2.0");

            writer.WritePropertyName("id");
            writer.WriteValue(value.Id);

            writer.WritePropertyName("result");
            // `null` is a valid value for some results, so we need to handle it properly
            if (value.Result != null)
            {
                serializer.Serialize(writer, value.Result);
            }
            else
            {
                writer.WriteNull();
            }
            writer.WriteEndObject();
        }
    }
}
