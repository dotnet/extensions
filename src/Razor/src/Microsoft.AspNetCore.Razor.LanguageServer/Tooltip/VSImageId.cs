// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Tooltip
{
    /// <summary>
    /// Equivalent to VS' ContainerElementStyle. The class has been adapted here so we
    /// can use it for LSP serialization since we don't have access to the VS version.
    /// Refer to original class for additional details.
    /// </summary>
    internal class VSImageId : IEquatable<VSImageId>
    {
        [JsonProperty("type")]
        public static readonly string Type = "ImageId";

        [JsonProperty("Guid")]
        public readonly Guid Guid;

        [JsonProperty("Id")]
        public readonly int Id;

        public VSImageId(Guid guid, int id)
        {
            Guid = guid;
            Id = id;
        }
        public override string ToString()
        {
            return this.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public string ToString(IFormatProvider provider)
        {
            return string.Format(provider, @"{0} : {1}", Guid.ToString("D", provider), Id.ToString(provider));
        }

        bool IEquatable<VSImageId>.Equals(VSImageId other) => Id.Equals(other.Id) && Guid.Equals(other.Guid);

        public override bool Equals(object other) => other is VSImageId otherImage && ((IEquatable<VSImageId>)this).Equals(otherImage);

        public static bool operator ==(VSImageId left, VSImageId right) => left.Equals(right);

        public static bool operator !=(VSImageId left, VSImageId right) => !(left == right);

        public override int GetHashCode() => Guid.GetHashCode() ^ Id.GetHashCode();
    }
}
