// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Configuration
{
    public static partial class NewtonsoftJsonConfigurationExtensions
    {
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddNewtonsoftJsonFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, Microsoft.Extensions.FileProviders.IFileProvider provider, string path, bool optional, bool reloadOnChange) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddNewtonsoftJsonFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, System.Action<Microsoft.Extensions.Configuration.NewtonsoftJson.NewtonsoftJsonConfigurationSource> configureSource) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddNewtonsoftJsonFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, string path) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddNewtonsoftJsonFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, string path, bool optional) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddNewtonsoftJsonFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, string path, bool optional, bool reloadOnChange) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddNewtonsoftJsonStream(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, System.IO.Stream stream) { throw null; }
    }
}
namespace Microsoft.Extensions.Configuration.NewtonsoftJson
{
    public partial class NewtonsoftJsonConfigurationProvider : Microsoft.Extensions.Configuration.FileConfigurationProvider
    {
        public NewtonsoftJsonConfigurationProvider(Microsoft.Extensions.Configuration.NewtonsoftJson.NewtonsoftJsonConfigurationSource source) : base (default(Microsoft.Extensions.Configuration.FileConfigurationSource)) { }
        public override void Load(System.IO.Stream stream) { }
    }
    public partial class NewtonsoftJsonConfigurationSource : Microsoft.Extensions.Configuration.FileConfigurationSource
    {
        public NewtonsoftJsonConfigurationSource() { }
        public override Microsoft.Extensions.Configuration.IConfigurationProvider Build(Microsoft.Extensions.Configuration.IConfigurationBuilder builder) { throw null; }
    }
    public partial class NewtonsoftJsonConfigurationStreamLoader : Microsoft.Extensions.Configuration.IConfigurationStreamLoader
    {
        public NewtonsoftJsonConfigurationStreamLoader() { }
        public void Load(Microsoft.Extensions.Configuration.IConfigurationProvider provider, System.IO.Stream stream) { }
    }
}
