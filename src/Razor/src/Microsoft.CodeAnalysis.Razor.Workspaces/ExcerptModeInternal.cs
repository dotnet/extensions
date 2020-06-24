// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.ExternalAccess.Razor;

namespace Microsoft.CodeAnalysis.Razor
{
    // We have IVT access to the Roslyn APIs for product code, but not for testing.
    internal enum ExcerptModeInternal
    {
        SingleLine = RazorExcerptMode.SingleLine,
        Tooltip = RazorExcerptMode.Tooltip,
    }
}
