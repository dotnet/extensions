// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore.Cosmos.Metadata.Conventions.Internal
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public class CosmosConventionSetBuilder : ProviderConventionSetBuilder
    {
        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public CosmosConventionSetBuilder(
            ProviderConventionSetBuilderDependencies dependencies)
            : base(dependencies)
        {
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public override ConventionSet CreateConventionSet()
        {
            var conventionSet = base.CreateConventionSet();

            conventionSet.ModelInitializedConventions.Add(new ContextContainerConvention(Dependencies));

            conventionSet.ModelFinalizingConventions.Add(new ETagPropertyConvention());

            var storeKeyConvention = new StoreKeyConvention(Dependencies);
            var discriminatorConvention = new CosmosDiscriminatorConvention(Dependencies);
            KeyDiscoveryConvention keyDiscoveryConvention = new CosmosKeyDiscoveryConvention(Dependencies);
            InversePropertyAttributeConvention inversePropertyAttributeConvention =
                new CosmosInversePropertyAttributeConvention(Dependencies);
            RelationshipDiscoveryConvention relationshipDiscoveryConvention =
                new CosmosRelationshipDiscoveryConvention(Dependencies);
            conventionSet.EntityTypeAddedConventions.Add(storeKeyConvention);
            conventionSet.EntityTypeAddedConventions.Add(discriminatorConvention);
            ReplaceConvention(conventionSet.EntityTypeAddedConventions, keyDiscoveryConvention);
            ReplaceConvention(conventionSet.EntityTypeAddedConventions, inversePropertyAttributeConvention);
            ReplaceConvention(conventionSet.EntityTypeAddedConventions, relationshipDiscoveryConvention);

            ReplaceConvention(conventionSet.EntityTypeIgnoredConventions, relationshipDiscoveryConvention);

            ReplaceConvention(conventionSet.EntityTypeRemovedConventions, (DiscriminatorConvention)discriminatorConvention);
            ReplaceConvention(conventionSet.EntityTypeRemovedConventions, inversePropertyAttributeConvention);

            conventionSet.EntityTypeBaseTypeChangedConventions.Add(storeKeyConvention);
            ReplaceConvention(conventionSet.EntityTypeBaseTypeChangedConventions, (DiscriminatorConvention)discriminatorConvention);
            ReplaceConvention(conventionSet.EntityTypeBaseTypeChangedConventions, keyDiscoveryConvention);
            ReplaceConvention(conventionSet.EntityTypeBaseTypeChangedConventions, inversePropertyAttributeConvention);
            ReplaceConvention(conventionSet.EntityTypeBaseTypeChangedConventions, relationshipDiscoveryConvention);

            ReplaceConvention(conventionSet.EntityTypeMemberIgnoredConventions, keyDiscoveryConvention);
            ReplaceConvention(conventionSet.EntityTypeMemberIgnoredConventions, inversePropertyAttributeConvention);
            ReplaceConvention(conventionSet.EntityTypeMemberIgnoredConventions, relationshipDiscoveryConvention);

            conventionSet.EntityTypePrimaryKeyChangedConventions.Add(storeKeyConvention);

            conventionSet.KeyAddedConventions.Add(storeKeyConvention);

            conventionSet.KeyRemovedConventions.Add(storeKeyConvention);
            ReplaceConvention(conventionSet.KeyRemovedConventions, keyDiscoveryConvention);

            ReplaceConvention(conventionSet.ForeignKeyAddedConventions, keyDiscoveryConvention);

            ReplaceConvention(conventionSet.ForeignKeyRemovedConventions, relationshipDiscoveryConvention);
            conventionSet.ForeignKeyRemovedConventions.Add(discriminatorConvention);
            conventionSet.ForeignKeyRemovedConventions.Add(storeKeyConvention);
            ReplaceConvention(conventionSet.ForeignKeyRemovedConventions, keyDiscoveryConvention);

            ReplaceConvention(conventionSet.ForeignKeyPropertiesChangedConventions, keyDiscoveryConvention);

            ReplaceConvention(conventionSet.ForeignKeyUniquenessChangedConventions, keyDiscoveryConvention);

            conventionSet.ForeignKeyOwnershipChangedConventions.Add(discriminatorConvention);
            conventionSet.ForeignKeyOwnershipChangedConventions.Add(storeKeyConvention);
            ReplaceConvention(conventionSet.ForeignKeyOwnershipChangedConventions, keyDiscoveryConvention);
            ReplaceConvention(conventionSet.ForeignKeyOwnershipChangedConventions, relationshipDiscoveryConvention);

            ReplaceConvention(conventionSet.ForeignKeyNullNavigationSetConventions, relationshipDiscoveryConvention);

            ReplaceConvention(conventionSet.NavigationAddedConventions, inversePropertyAttributeConvention);
            ReplaceConvention(conventionSet.NavigationAddedConventions, relationshipDiscoveryConvention);

            ReplaceConvention(conventionSet.NavigationRemovedConventions, relationshipDiscoveryConvention);

            conventionSet.EntityTypeAnnotationChangedConventions.Add(discriminatorConvention);
            conventionSet.EntityTypeAnnotationChangedConventions.Add(storeKeyConvention);
            conventionSet.EntityTypeAnnotationChangedConventions.Add((CosmosKeyDiscoveryConvention)keyDiscoveryConvention);

            ReplaceConvention(conventionSet.PropertyAddedConventions, keyDiscoveryConvention);

            conventionSet.PropertyAnnotationChangedConventions.Add(storeKeyConvention);

            ReplaceConvention(conventionSet.ModelFinalizingConventions, inversePropertyAttributeConvention);

            return conventionSet;
        }

        /// <summary>
        ///     <para>
        ///         Call this method to build a <see cref="ModelBuilder" /> for Cosmos outside of <see cref="DbContext.OnModelCreating" />.
        ///     </para>
        ///     <para>
        ///         Note that it is unusual to use this method.
        ///         Consider using <see cref="DbContext" /> in the normal way instead.
        ///     </para>
        /// </summary>
        /// <returns> The convention set. </returns>
        public static ModelBuilder CreateModelBuilder()
        {
            using var serviceScope = CreateServiceScope();
            using var context = serviceScope.ServiceProvider.GetRequiredService<DbContext>();
            return new ModelBuilder(ConventionSet.CreateConventionSet(context), context.GetService<ModelDependencies>());
        }

        private static IServiceScope CreateServiceScope()
        {
            var serviceProvider = new ServiceCollection()
                .AddEntityFrameworkCosmos()
                .AddDbContext<DbContext>(
                    (p, o) =>
                        o.UseCosmos("localhost", "_", "_")
                            .UseInternalServiceProvider(p))
                .BuildServiceProvider();

            return serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        }
    }
}