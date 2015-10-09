using System;
using Microsoft.Framework.Logging;
using System.IO;
using System.Threading;
using System.Linq;
using System.Collections.Generic;

namespace SampleApp
{
    public class CaptureData : ILoggerProvider
    {
        Action<TextWriter> _rewrite = _ => { };

        public CaptureData()
        {
        }

        public ILogger CreateLogger(string name)
        {
            return new CaptureLogger(this, name);
        }

        public void Rewrite(TextWriter writer)
        {
            _rewrite(writer);
        }

        public void OnRewrite(Action<TextWriter> rewrite)
        {
            var prior = _rewrite;
            _rewrite = writer =>
            {
                prior(writer);
                rewrite(writer);
            };
        }

        public void Dispose()
        {
        }

        private class CaptureLogger : ILogger
        {
            private readonly CaptureData self;
            private readonly string name;

            public CaptureLogger(CaptureData self, string name)
            {
                this.self = self;
                this.name = name;
            }

            public IDisposable BeginScopeImpl(object state)
            {
                return new CaptureScope(state);
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log(LogLevel logLevel, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
            {
                if (formatter == null)
                {
                    return;
                }

                var text = formatter(state, exception);

                var values = ((state as ILogValues)?.GetValues() ?? Enumerable.Empty<KeyValuePair<string, object>>())
                        .Where(kv => !kv.Key.StartsWith("{"))
                        .ToArray();

                for (var scope = CaptureScope.Current; scope != null; scope = scope.Previous)
                {
                    values = values.Concat(scope.Values).ToArray();
                }

                self.OnRewrite(writer =>
                {
                    writer.WriteLine();
                    writer.WriteLine($"{logLevel} {name} {eventId}");
                    writer.WriteLine($"  {text}");
                    foreach (var value in values)
                    {
                        writer.WriteLine($"  {value.Key}: {value.Value}");
                    }
                });
            }

            private class CaptureScope : IDisposable
            {
#if DNX451
                public static CaptureScope Current
                {
                    get
                    {
                        return (CaptureScope)System.Runtime.Remoting.Messaging.CallContext.LogicalGetData(nameof(CaptureScope));
                    }
                    set
                    {
                        System.Runtime.Remoting.Messaging.CallContext.LogicalSetData(nameof(CaptureScope), value);
                    }
                }
#else
                public static AsyncLocal<CaptureScope> _current = new AsyncLocal<CaptureScope>();
                public static CaptureScope Current
                {
                    get
                    {
                        return _current.Value;
                    }
                    set
                    {
                        _current.Value = value;
                    }
                }
#endif

                public CaptureScope(object scope)
                {
                    Previous = Current;
                    Current = this;

                    Values = ((scope as ILogValues)?.GetValues() ?? Enumerable.Empty<KeyValuePair<string, object>>())
                        .Where(kv => !kv.Key.StartsWith("{"))
                        .ToArray();
                }

                public CaptureScope Previous { get; set; }
                public KeyValuePair<string, object>[] Values { get; set; }

                public void Dispose()
                {
                    Current = Previous;
                }
            }
        }
    }
}
