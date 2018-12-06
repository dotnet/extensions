// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NETCOREAPP

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Default implemenation of <see cref="IExternalScopeProvider"/>
    /// </summary>
    internal class LoggerActivityExternalScopeProvider : IExternalScopeProvider
    {
        /// <inheritdoc />
        public void ForEachScope<TState>(Action<object, TState> callback, TState state)
        {
            void Report(Activity current)
            {
                if (current == null)
                {
                    return;
                }
                Report(current.Parent);
                callback(current, state);
            }
            Report(Activity.Current);
        }

        /// <inheritdoc />
        public IDisposable Push(object state)
        {
            if (state == null)
            {
                return NullScope.Instance;
            }

            Activity activity;
            IDisposable scope;
            if (state is Activity a)
            {
                activity = a;
                scope = new ActivityWrapper(activity);
            }
            else
            {
                var activityScope = new StateActivity(state);
                activity = activityScope;
                scope = activityScope;
            }

            activity.Start();
            return scope;
        }

        private class StateActivity : Activity, IDisposable, IReadOnlyList<KeyValuePair<string, object>>
        {
            private bool _isDisposed;

            internal StateActivity(object state): base(GetActivityName(state))
            {
                State = state;
            }

            private static string GetActivityName(object state)
            {
                switch (state)
                {
                    case string s:
                        return s;
                    default:
                        return state.GetType().FullName;
                }
            }

            public object State { get; }

            public int Count => State is IList<KeyValuePair<string, object>> list ? list.Count + 1 : 2;

            public KeyValuePair<string, object> this[int index]
            {
                get
                {

                    if (index == 0)
                    {
                        return new KeyValuePair<string, object>("ActivityId", Id);
                    }

                    if (State is IList<KeyValuePair<string, object>> list)
                    {
                        return list[index - 1];
                    }

                    if (index == 2)
                    {
                        return new KeyValuePair<string, object>("Scope", State);
                    }

                    throw new IndexOutOfRangeException();
                }
            }

            public override string ToString()
            {
                return State.ToString();
            }

            public void Dispose()
            {
                if (!_isDisposed)
                {
                    Stop();
                    _isDisposed = true;
                }
            }

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                for (int i = 0; i < Count; ++i)
                {
                    yield return this[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class ActivityWrapper : IDisposable
        {
            private bool _isDisposed;

            public ActivityWrapper(Activity activity)
            {
                Activity = activity;
            }

            public Activity Activity { get; }

            public override string ToString()
            {
                return Activity.ToString();
            }

            public void Dispose()
            {
                if (!_isDisposed)
                {
                    Activity.Stop();
                    _isDisposed = true;
                }
            }
        }
    }
}

#endif
