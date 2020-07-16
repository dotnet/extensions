// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Semantic.Models
{
    internal class SemanticTokensEdit
    {
        public int Start { get; set; }
        public int DeleteCount { get; set; }
        public IEnumerable<uint> Data { get; set; }

        public override bool Equals(object obj)
        {
            if ((obj is null && this != null) || !(obj is SemanticTokensEdit other))
            {
                return false;
            }

            var equal = Start.Equals(other.Start);
            equal &= DeleteCount.Equals(other.DeleteCount);
            equal &= (Data is null && other.Data is null) || Enumerable.SequenceEqual(Data, other.Data);

            return equal;
        }

        public override int GetHashCode()
        {
            var combiner = HashCodeCombiner.Start();

            combiner.Add(Start);
            combiner.Add(DeleteCount);
            combiner.Add(Data);

            return combiner.CombinedHash;
        }
    }
}
