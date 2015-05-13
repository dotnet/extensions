// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.Notification
{
    public class NotifierParameterAdapter : INotifyParameterAdapter
    {
#if NET45 || DNX451 || DNXCORE50
        private readonly Internal.ConverterCache _cache = new Internal.ConverterCache();
#endif

        public object Adapt(object inputParameter, Type outputType)
        {
            if (inputParameter == null)
            {
                return null;
            }

#if NET45 || DNX451 || DNXCORE50
            return Internal.Converter.Convert(_cache, outputType, inputParameter.GetType(), inputParameter);
#else
            return inputParameter;
#endif
        }
    }
}
