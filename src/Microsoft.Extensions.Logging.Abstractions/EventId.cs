// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Logging
{
    public struct EventId
    {
        private int _id;
        private string _name;

        public EventId(int id, string name = null)
        {
            _id = id;
            _name = name;
        }

        public int Id
        {
            get
            {
                return _id;
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
        }

        public static implicit operator EventId(int i)
        {
            return new EventId(i);
        }

        public override string ToString()
        {
            if (_name != null)
            {
                return _name;
            }
            else
            {
                return _id.ToString();
            }
        }
    }
}
