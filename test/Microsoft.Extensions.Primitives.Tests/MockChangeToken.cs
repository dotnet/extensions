using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Primitives.Tests
{
    public class MockChangeToken: IChangeToken
    {
        private readonly List<Tuple<Action<object>, object, MockDisposable>> _callbacks = new List<Tuple<Action<object>, object, MockDisposable>>();

        public bool ActiveChangeCallbacks { get; set; }

        public bool HasChanged { get; set; }

        public List<Tuple<Action<object>, object, MockDisposable>> Callbacks
        {
            get
            {
                return _callbacks;
            }
        }

        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
            var disposable = new MockDisposable();
            _callbacks.Add(Tuple.Create(callback, state, disposable));
            return disposable;
        }

        internal void RaiseCallback(object item)
        {
            foreach (var callback in _callbacks)
            {
                callback.Item1(item);
            }
        }
    }
}
