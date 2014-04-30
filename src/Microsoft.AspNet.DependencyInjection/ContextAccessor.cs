using System;

namespace Microsoft.AspNet.DependencyInjection
{
    public class ContextAccessor<TContext> : IContextAccessor<TContext>
    {
        private ContextSource _source;
        private TContext _value;

        public ContextAccessor()
        {
            _source = new ContextSource();
        }

        public TContext Value
        {
            get
            {
                return _source.Access != null ? _source.Access() : _value;
            }
        }

        public TContext SetValue(TContext value)
        {
            if (_source.Exchange != null)
            {
                return _source.Exchange(value);
            }
            var prior = _value;
            _value = value;
            return prior;
        }

        public IDisposable SetContextSource(Func<TContext> access, Func<TContext, TContext> exchange)
        {
            var prior = _source;
            _source = new ContextSource(access, exchange);
            return new Disposable(this, prior);
        }

        struct ContextSource
        {
            public ContextSource(Func<TContext> access, Func<TContext, TContext> exchange)
            {
                Access = access;
                Exchange = exchange;
            }

            public readonly Func<TContext> Access;
            public readonly Func<TContext, TContext> Exchange;
        }

        class Disposable : IDisposable
        {
            private readonly ContextAccessor<TContext> _contextAccessor;
            private readonly ContextSource _source;

            public Disposable(ContextAccessor<TContext> contextAccessor, ContextSource source)
            {
                _contextAccessor = contextAccessor;
                _source = source;
            }

            public void Dispose()
            {
                _contextAccessor._source = _source;
            }
        }
    }
}