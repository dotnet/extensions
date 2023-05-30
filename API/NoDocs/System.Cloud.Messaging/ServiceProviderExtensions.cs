// Assembly 'System.Cloud.Messaging'

using System.Collections.Generic;

namespace System.Cloud.Messaging;

public static class ServiceProviderExtensions
{
    public static IMessageSource GetMessageSource(this IServiceProvider serviceProvider, string pipelineName);
    public static IReadOnlyList<IMessageMiddleware> GetMessageMiddlewares(this IServiceProvider serviceProvider, string pipelineName);
    public static MessageDelegate GetMessageDelegate(this IServiceProvider serviceProvider, string pipelineName);
}
