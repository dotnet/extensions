// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace TestClasses
{
    public partial class Class2
    {
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Needed for testing.")]
        public string Bar => "BarValue";
    }
}
