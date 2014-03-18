using System;

namespace Microsoft.AspNet.DependencyInjection
{
    public interface IContextAccessor<TContext>
    {
        TContext Value { get; }

        TContext ExchangeValue(TContext value);

        IDisposable SetContextSource(Func<TContext> access, Func<TContext, TContext> exchange);
    }
}