// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Windows.Controls;

namespace Microsoft.VisualStudio.RazorExtension.DocumentInfo
{
    public partial class RazorDocumentInfoWindowControl : UserControl
    {
        public RazorDocumentInfoWindowControl()
        {
            this.InitializeComponent();
        }
    }

    internal static class RazorDocumentInfoWindowControl_Workaround
    {
        public static void InitializeComponent(this RazorDocumentInfoWindowControl _)
        {
        }
    }
}