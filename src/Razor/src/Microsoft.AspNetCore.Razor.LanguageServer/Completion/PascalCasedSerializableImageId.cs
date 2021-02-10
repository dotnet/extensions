// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion
{
    [DataContract]
    internal struct PascalCasedSerializableImageId
    {
        public PascalCasedSerializableImageId(Guid guid, int id)
        {
            Guid = guid;
            Id = id;
        }

        [DataMember(Name = "Guid")]
        public Guid Guid { get; set; }

        [DataMember(Name = "Id")]
        public int Id { get; set; }
    }
}
