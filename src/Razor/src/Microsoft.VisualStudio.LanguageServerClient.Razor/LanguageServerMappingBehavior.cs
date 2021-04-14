// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    // This should be kept in-sync with the language server's MappingBehavior enum.

    internal enum LanguageServerMappingBehavior
    {
        Strict,

        /// <summary>
        /// Inclusive mapping behavior will attempt to map overlapping or intersecting generated ranges with a provided projection range.
        ///
        /// Behavior:
        ///     - Overlaps > 1 generated range = No mapping
        ///     - Intersects > 1 generated range = No mapping
        ///     - Overlaps 1 generated range = Will reduce the provided range down to the generated range.
        ///     - Intersects 1 generated range = Will use the generated range mappings
        /// </summary>
        Inclusive
    }
}
