// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.DiagnosticAdapter
{
    public class DiagnosticNameAttribute : Attribute
    {
        public DiagnosticNameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
