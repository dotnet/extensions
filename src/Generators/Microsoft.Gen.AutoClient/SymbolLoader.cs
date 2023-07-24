// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Gen.AutoClient;

internal static class SymbolLoader
{
    internal const string RestApiAttribute = "Microsoft.Extensions.Http.AutoClient.AutoClientAttribute";

    internal const string RestGetAttribute = "Microsoft.Extensions.Http.AutoClient.GetAttribute";
    internal const string RestPostAttribute = "Microsoft.Extensions.Http.AutoClient.PostAttribute";
    internal const string RestPutAttribute = "Microsoft.Extensions.Http.AutoClient.PutAttribute";
    internal const string RestDeleteAttribute = "Microsoft.Extensions.Http.AutoClient.DeleteAttribute";
    internal const string RestPatchAttribute = "Microsoft.Extensions.Http.AutoClient.PatchAttribute";
    internal const string RestOptionsAttribute = "Microsoft.Extensions.Http.AutoClient.OptionsAttribute";
    internal const string RestHeadAttribute = "Microsoft.Extensions.Http.AutoClient.HeadAttribute";

    internal const string RestStaticHeaderAttribute = "Microsoft.Extensions.Http.AutoClient.StaticHeaderAttribute";
    internal const string RestHeaderAttribute = "Microsoft.Extensions.Http.AutoClient.HeaderAttribute";
    internal const string RestQueryAttribute = "Microsoft.Extensions.Http.AutoClient.QueryAttribute";
    internal const string RestBodyAttribute = "Microsoft.Extensions.Http.AutoClient.BodyAttribute";

    internal static SymbolHolder? LoadSymbols(Compilation compilation)
    {
        var restApiAttribute = compilation.GetTypeByMetadataName(RestApiAttribute);

        var restGetAttribute = compilation.GetTypeByMetadataName(RestGetAttribute);
        var restPostAttribute = compilation.GetTypeByMetadataName(RestPostAttribute);
        var restPutAttribute = compilation.GetTypeByMetadataName(RestPutAttribute);
        var restDeleteAttribute = compilation.GetTypeByMetadataName(RestDeleteAttribute);
        var restPatchAttribute = compilation.GetTypeByMetadataName(RestPatchAttribute);
        var restOptionsAttribute = compilation.GetTypeByMetadataName(RestOptionsAttribute);
        var restHeadAttribute = compilation.GetTypeByMetadataName(RestHeadAttribute);

        var restStaticHeaderAttribute = compilation.GetTypeByMetadataName(RestStaticHeaderAttribute);
        var restHeaderAttribute = compilation.GetTypeByMetadataName(RestHeaderAttribute);
        var restQueryAttribute = compilation.GetTypeByMetadataName(RestQueryAttribute);
        var restBodyAttribute = compilation.GetTypeByMetadataName(RestBodyAttribute);

        if (restApiAttribute == null)
        {
            // nothing to do if these types aren't available
            return null;
        }

        return new(
            restApiAttribute,
            restGetAttribute,
            restPostAttribute,
            restPutAttribute,
            restDeleteAttribute,
            restPatchAttribute,
            restOptionsAttribute,
            restHeadAttribute,
            restStaticHeaderAttribute,
            restHeaderAttribute,
            restQueryAttribute,
            restBodyAttribute);
    }
}
