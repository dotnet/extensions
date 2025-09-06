using Microsoft.Extensions.AI;
using Aspire.OpenAI;
using OpenAI;
using System.Data.Common;

namespace aichatweb.Web.Services;

public static class AspireOpenAIClientBuilderResponsesChatClientExtensions
{
    private const string DeploymentKey = "Deployment";
    private const string ModelKey = "Model";

    public static ChatClientBuilder AddResponsesChatClient(this AspireOpenAIClientBuilder builder, string? deploymentName)
    {
        ArgumentNullException.ThrowIfNull(builder, "builder");

        return builder.HostBuilder.Services.AddChatClient((IServiceProvider services) => CreateInnerChatClient(services, builder, deploymentName));
    }
    private static IChatClient CreateInnerChatClient(IServiceProvider services, AspireOpenAIClientBuilder builder, string? deploymentName)
    {
        OpenAIClient openAIClient = builder.ServiceKey is null ? services.GetRequiredService<OpenAIClient>() : services.GetRequiredKeyedService<OpenAIClient>(builder.ServiceKey);

        deploymentName ??= GetRequiredDeploymentName(builder);

        IChatClient chatClient = openAIClient.GetOpenAIResponseClient(deploymentName).AsIChatClient();

        if (builder.DisableTracing)
        {
            return chatClient;
        }

        var loggerFactory = services.GetService<ILoggerFactory>();
        return new OpenTelemetryChatClient(chatClient, loggerFactory?.CreateLogger(typeof(OpenTelemetryChatClient)));
    }

    private static string GetRequiredDeploymentName(this AspireOpenAIClientBuilder builder)
    {
        string? deploymentName = null;

        var configuration = builder.HostBuilder.Configuration;
        if (configuration.GetConnectionString(builder.ConnectionName) is string connectionString)
        {
            // The reason we accept either 'Deployment' or 'Model' as the key is because some hosting solutions
            // require specific named deployments (Azure Foundry AI) while others may use a generic model name (OpenAI, GitHub Models).
            var connectionBuilder = new DbConnectionStringBuilder { ConnectionString = connectionString };
            var deploymentValue = ConnectionStringValue(connectionBuilder, DeploymentKey);
            var modelValue = ConnectionStringValue(connectionBuilder, ModelKey);
            if (deploymentValue is not null && modelValue is not null)
            {
                throw new InvalidOperationException(
                    $"The connection string '{builder.ConnectionName}' contains both '{DeploymentKey}' and '{ModelKey}' keys. Either of these may be specified, but not both.");
            }

            deploymentName = deploymentValue ?? modelValue;
        }

        if (string.IsNullOrEmpty(deploymentName))
        {
            var configSection = configuration.GetSection(builder.ConfigurationSectionName);
            deploymentName = configSection[DeploymentKey];
        }

        if (string.IsNullOrEmpty(deploymentName))
        {
            throw new InvalidOperationException($"The deployment could not be determined. Ensure a '{DeploymentKey}' or '{ModelKey}' value is provided in 'ConnectionStrings:{builder.ConnectionName}', or specify a '{DeploymentKey}' in the '{builder.ConfigurationSectionName}' configuration section, or specify a '{nameof(deploymentName)}' in the call.");
        }

        return deploymentName;
    }

    private static string? ConnectionStringValue(DbConnectionStringBuilder connectionString, string key)
        => connectionString.TryGetValue(key, out var value) ? value as string : null;

}
