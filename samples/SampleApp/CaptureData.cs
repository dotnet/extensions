using System;
using Microsoft.Framework.Logging;
using System.IO;

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
                return new CaptureScope();
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
                var values = state as ILogValues;

                self.OnRewrite(writer =>
                {
                    writer.WriteLine($"{logLevel} {name} {eventId} {text}");
                    if (values != null)
                    {
                        foreach (var value in values.GetValues())
                        {
                            writer.WriteLine($"  {value.Key}: {value.Value}");
                        }
                    }
                });
            }

            private class CaptureScope : IDisposable
            {
                public void Dispose()
                {
                }
            }
        }
    }
}