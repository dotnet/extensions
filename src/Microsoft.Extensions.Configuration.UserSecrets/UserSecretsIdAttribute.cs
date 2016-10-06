// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Configuration.UserSecrets
{
    /// <summary>
    /// Represents the user secrets id.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    public class UserSecretsIdAttribute : Attribute
    {
        /// <summary>
        /// Initializes an instance of <see cref="UserSecretsIdAttribute" />.
        /// </summary>
        /// <param name="userSecretId">The user secrets id</param>
        public UserSecretsIdAttribute(string userSecretId)
        {
            if (string.IsNullOrEmpty(userSecretId))
            {
                throw new ArgumentException(Resources.Common_StringNullOrEmpty, nameof(userSecretId));
            }

            UserSecretsId = userSecretId;
        }

        /// <summary>
        /// The user secrets id.
        /// </summary>
        public string UserSecretsId { get; }
    }
}
