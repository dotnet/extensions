// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.Serialization;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion
{
    [DataContract]
    internal class PascalCasedSerializableImageElement
    {
        public PascalCasedSerializableImageElement(PascalCasedSerializableImageId imageId)
        {
            ImageId = imageId;
        }

        [DataMember(Name = "ImageId")]
        public PascalCasedSerializableImageId ImageId { get; set; }
    }
}
