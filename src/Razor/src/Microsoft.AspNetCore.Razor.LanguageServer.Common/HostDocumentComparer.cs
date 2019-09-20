// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Common
{
    internal class HostDocumentComparer : IEqualityComparer<HostDocument>
    {
        public static readonly HostDocumentComparer Instance = new HostDocumentComparer();

        private HostDocumentComparer()
        {
        }

        public bool Equals(HostDocument x, HostDocument y)
        {
            if (x.FileKind != y.FileKind)
            {
                return false;
            }

            if (!FilePathComparer.Instance.Equals(x.FilePath, y.FilePath))
            {
                return false;
            }

            if (!FilePathComparer.Instance.Equals(x.TargetPath, y.TargetPath))
            {
                return false;
            }

            return true;
        }

        public int GetHashCode(HostDocument hostDocument)
        {
            var combiner = HashCodeCombiner.Start();
            combiner.Add(hostDocument.FilePath, FilePathComparer.Instance);
            combiner.Add(hostDocument.TargetPath, FilePathComparer.Instance);
            combiner.Add(hostDocument.FileKind);

            return combiner.CombinedHash;
        }
    }
}
