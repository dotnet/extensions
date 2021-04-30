// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Tooltip
{
    /// <summary>
    /// Equivalent to VS' ClassifiedTextRunStyle. The class has been adapted here so we
    /// can use it for LSP serialization since we don't have access to the VS version.
    /// Refer to original class for additional details.
    /// </summary>
    [Flags]
    public enum VSClassifiedTextRunStyle
    {
        Plain = 0b_0000_0000,
        Bold = 0b_0000_0001,
        Italic = 0b_0000_0010,
        Underline = 0b_0000_0100,
        UseClassificationFont = 0b_0000_1000,
        UseClassificationStyle = 0b_0001_0000,
    }
}
