// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Security.Claims;

namespace Microsoft.Framework.Internal
{
    /// <summary>
    /// Helper code used when implementing authentication middleware
    /// </summary>
    internal static class SecurityHelper
    {
        /// <summary>
        /// Add all ClaimsIdentities from an additional ClaimPrincipal to the ClaimsPrincipal
        /// Merges a new claims principal, placing all new identities first, and eliminating
        /// any empty unauthenticated identities from context.User
        /// </summary>
        /// <param name="identity"></param>
        public static ClaimsPrincipal MergeUserPrincipal([NotNull] ClaimsPrincipal existingPrincipal, [NotNull] ClaimsPrincipal additionalPrincipal)
        {
            var newPrincipal = new ClaimsPrincipal();
            // New principal identities go first
            newPrincipal.AddIdentities(additionalPrincipal.Identities);

            // Then add any existing non empty or authenticated identities
            if (existingPrincipal != null)
            {
                newPrincipal.AddIdentities(existingPrincipal.Identities.Where(i => i.IsAuthenticated || i.Claims.Any()));
            }
            return newPrincipal;
        }
    }
}
