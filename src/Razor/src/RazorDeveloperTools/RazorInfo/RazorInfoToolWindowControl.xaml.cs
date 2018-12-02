// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Windows;
using System.Windows.Controls;

namespace Microsoft.VisualStudio.RazorExtension.RazorInfo
{
    public partial class RazorInfoToolWindowControl : UserControl
    {
        public RazorInfoToolWindowControl()
        {
            this.InitializeComponent();
        }

        protected partial void InitializeComponent()
        {
        }
    }

    internal static class RazorInfoToolWindowControl_Workaround
    {
        public static void InitializeComponent(this RazorInfoToolWindowControl _)
        {
        }
    }
}