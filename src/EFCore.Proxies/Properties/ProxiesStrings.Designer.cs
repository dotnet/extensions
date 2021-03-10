// <auto-generated />

using System;
using System.Reflection;
using System.Resources;
using JetBrains.Annotations;

#nullable enable

namespace Microsoft.EntityFrameworkCore.Internal
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static class ProxiesStrings
    {
        private static readonly ResourceManager _resourceManager
            = new ResourceManager("Microsoft.EntityFrameworkCore.Properties.ProxiesStrings", typeof(ProxiesStrings).Assembly);

        /// <summary>
        ///     The type '{dictionaryType}' used for shared entity type '{entityType}' is not suitable for use as a change-tracking proxy because its indexer property is not virtual. Consider using an implementation of '{interfaceType}' that allows overriding of the indexer.
        /// </summary>
        public static string DictionaryCannotBeProxied([CanBeNull] object? dictionaryType, [CanBeNull] object? entityType, [CanBeNull] object? interfaceType)
            => string.Format(
                GetString("DictionaryCannotBeProxied", nameof(dictionaryType), nameof(entityType), nameof(interfaceType)),
                dictionaryType, entityType, interfaceType);

        /// <summary>
        ///     The type '{clrType}' is configured as a shared-type entity type, but the entity type name is not known. Ensure that CreateProxy is called on a DbSet created specifically for the shared-type entity type through use of a 'DbContext.Set' overload that accepts an entity type name.
        /// </summary>
        public static string EntityTypeNotFoundShared([CanBeNull] object? clrType)
            => string.Format(
                GetString("EntityTypeNotFoundShared", nameof(clrType)),
                clrType);

        /// <summary>
        ///     Cannot create a proxy for '{typeName}' because it is mapped to multiple owned entity types. Proxy creation is not supported for owned types used more than once in the model.
        /// </summary>
        [Obsolete]
        public static string EntityTypeNotFoundWeak([CanBeNull] object? typeName)
            => string.Format(
                GetString("EntityTypeNotFoundWeak", nameof(typeName)),
                typeName);

        /// <summary>
        ///     Property '{property}' on entity type '{entityType}' is mapped without a CLR property. 'UseChangeTrackingProxies' requires all entity types to be public, unsealed, have virtual properties, and have a public or protected constructor. 'UseLazyLoadingProxies' requires only the navigation properties be virtual.
        /// </summary>
        public static string FieldProperty([CanBeNull] object? property, [CanBeNull] object? entityType)
            => string.Format(
                GetString("FieldProperty", nameof(property), nameof(entityType)),
                property, entityType);

        /// <summary>
        ///     Entity type '{entityType}' is sealed. 'UseChangeTrackingProxies' requires all entity types to be public, unsealed, have virtual properties, and have a public or protected constructor. 'UseLazyLoadingProxies' requires only the navigation properties be virtual.
        /// </summary>
        public static string ItsASeal([CanBeNull] object? entityType)
            => string.Format(
                GetString("ItsASeal", nameof(entityType)),
                entityType);

        /// <summary>
        ///     The mapped indexer property on entity type '{entityType}' is not virtual. 'UseChangeTrackingProxies' requires all entity types to be public, unsealed, have virtual properties, and have a public or protected constructor. 'UseLazyLoadingProxies' requires only the navigation properties be virtual.
        /// </summary>
        public static string NonVirtualIndexerProperty([CanBeNull] object? entityType)
            => string.Format(
                GetString("NonVirtualIndexerProperty", nameof(entityType)),
                entityType);

        /// <summary>
        ///     Property '{1_entityType}.{0_property}' is not virtual. 'UseChangeTrackingProxies' requires all entity types to be public, unsealed, have virtual properties, and have a public or protected constructor. 'UseLazyLoadingProxies' requires only the navigation properties be virtual.
        /// </summary>
        public static string NonVirtualProperty([CanBeNull] object? property, [CanBeNull] object? entityType)
            => string.Format(
                GetString("NonVirtualProperty", "0_property", "1_entityType"),
                property, entityType);

        /// <summary>
        ///     Unable to create proxy for '{entityType}' because proxies are not enabled. Call 'DbContextOptionsBuilder.UseChangeTrackingProxies' or 'DbContextOptionsBuilder.UseLazyLoadingProxies' to enable proxies.
        /// </summary>
        public static string ProxiesNotEnabled([CanBeNull] object? entityType)
            => string.Format(
                GetString("ProxiesNotEnabled", nameof(entityType)),
                entityType);

        /// <summary>
        ///     'UseChangeTrackingProxies' and 'UseLazyLoadingProxies' each require AddEntityFrameworkProxies to be called on the internal service provider used.
        /// </summary>
        public static string ProxyServicesMissing
            => GetString("ProxyServicesMissing");

        private static string GetString(string name, params string[] formatterNames)
        {
            var value = _resourceManager.GetString(name)!;
            for (var i = 0; i < formatterNames.Length; i++)
            {
                value = value.Replace("{" + formatterNames[i] + "}", "{" + i + "}");
            }

            return value;
        }
    }
}
