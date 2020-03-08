// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Extensions.Configuration.NewtonsoftJson
{
    /// <summary>
    /// Represents a JSON file as an <see cref="IConfigurationSource"/>.
    /// </summary>
    public class NewtonsoftJsonConfigurationSource : FileConfigurationSource
    {
        /// <summary>
        /// Builds the <see cref="NewtonsoftJsonConfigurationProvider"/> for this source.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
        /// <returns>A <see cref="NewtonsoftJsonConfigurationProvider"/></returns>
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            EnsureDefaults(builder);
            return new NewtonsoftJsonConfigurationProvider(this);
        }
    }
}