// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.RazorExtension.RazorInfo
{
    public class ProjectPropertyItemViewModel : NotifyPropertyChanged
    {
        internal ProjectPropertyItemViewModel(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }

        public string Value { get; }
    }
}