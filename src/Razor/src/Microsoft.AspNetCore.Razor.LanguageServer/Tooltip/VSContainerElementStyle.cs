// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Tooltip
{
    /// <summary>
    /// Equivalent to VS' ContainerElementStyle. The class has been adapted here so we
    /// can use it for LSP serialization since we don't have access to the VS version.
    /// Refer to original class for additional details.
    /// </summary>
    [Flags]
    internal enum VSContainerElementStyle
    {
        Wrapped = 0b_0000,
        Stacked = 0b_0001,
        VerticalPadding = 0b_0010
    }
}
