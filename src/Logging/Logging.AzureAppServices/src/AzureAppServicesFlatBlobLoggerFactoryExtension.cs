namespace Microsoft.Extensions.Logging.AzureAppServices
{
    public static class AzureAppServicesFlatBlobLoggerFactoryExtension
    {
        public static string BlobPrefixFileName;
        public static ILoggingBuilder AddAzureWebAppDiagnostics(this ILoggingBuilder builder, string fileName)
        {
            BlobPrefixFileName = fileName;

            var context = WebAppContext.Default;

            // Only add the provider if we're in Azure WebApp. That cannot change once the apps started
            return AzureAppServicesLoggerFactoryExtensions.AddAzureWebAppDiagnostics(builder, context);
        }
 
    }
}