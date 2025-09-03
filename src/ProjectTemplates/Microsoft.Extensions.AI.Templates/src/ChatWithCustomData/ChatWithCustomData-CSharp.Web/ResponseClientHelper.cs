using Microsoft.Extensions.AI;
using Aspire.OpenAI;
using OpenAI;

//namespace chat.aspire.Web;
namespace Microsoft.Extensions.Hosting;

public static class ResponseClientHelper
{

    public static ChatClientBuilder AddResponsesChatClient(this AspireOpenAIClientBuilder builder, string? deploymentName)
    {
        ArgumentNullException.ThrowIfNull(builder, "builder");

        return builder.HostBuilder.Services.AddChatClient((IServiceProvider services) => CreateInnerChatClient(services, builder, deploymentName));
    }
    private static IChatClient CreateInnerChatClient(IServiceProvider services, AspireOpenAIClientBuilder builder, string? deploymentName)
    {
        OpenAIClient openAIClient = builder.ServiceKey is null ? services.GetRequiredService<OpenAIClient>() : services.GetRequiredKeyedService<OpenAIClient>(builder.ServiceKey);

        if (deploymentName is null)
        {
            deploymentName = "gpt-4o-mini";
        }

        IChatClient chatClient = openAIClient.GetOpenAIResponseClient(deploymentName).AsIChatClient();

        if (builder.DisableTracing)
        {
            return chatClient;
        }

        var loggerFactory = services.GetService<ILoggerFactory>();
        return new OpenTelemetryChatClient(chatClient, loggerFactory?.CreateLogger(typeof(OpenTelemetryChatClient)));
    }
}
