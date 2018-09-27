// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RemoteProjectItem : RazorProjectItem
    {
        private readonly RazorProjectItem _from;

        public RemoteProjectItem(RazorProjectItem from, string physicalPath)
        {
            _from = from;
            PhysicalPath = physicalPath;
        }

        public override string BasePath => _from.BasePath;

        public override string FilePath => _from.FilePath;

        public override string PhysicalPath { get; }

        public override bool Exists => _from.Exists;

        public override Stream Read() => _from.Read();
    }
}
