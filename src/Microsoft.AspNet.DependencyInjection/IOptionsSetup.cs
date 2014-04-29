using System;

namespace Microsoft.AspNet.DependencyInjection
{
    public interface IOptionsSetup<in TOptions>
    {
        int Order { get; }
        void Setup(TOptions options);
    }
}