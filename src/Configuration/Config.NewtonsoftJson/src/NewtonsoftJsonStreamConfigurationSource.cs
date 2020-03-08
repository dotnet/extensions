// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Extensions.Configuration.NewtonsoftJson
{
    /// <summary>
    /// Represents a JSON file as an <see cref="IConfigurationSource"/>.
    /// </summary>
    public class NewtonsoftJsonStreamConfigurationSource : StreamConfigurationSource
    {
        /// <summary>
        /// Builds the <see cref="NewtonsoftJsonStreamConfigurationProvider"/> for this source.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
        /// <returns>An <see cref="NewtonsoftJsonStreamConfigurationProvider"/></returns>
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
            => new NewtonsoftJsonStreamConfigurationProvider(this);
    }
}
