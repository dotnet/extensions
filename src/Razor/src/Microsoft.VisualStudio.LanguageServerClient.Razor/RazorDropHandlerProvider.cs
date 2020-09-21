// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.DragDrop;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    [Export(typeof(IDropHandlerProvider))]
    [ContentType(RazorLSPConstants.RazorLSPContentTypeName)]
    [DropFormat(RazorLSPConstants.VSProjectItemsIdentifier)]
    [Name(nameof(RazorDropHandlerProvider))]
    [Order(Before = "LanguageServiceTextDropHandler")]
    internal sealed class RazorDropHandlerProvider : IDropHandlerProvider
    {
        public IDropHandler GetAssociatedDropHandler(IWpfTextView wpfTextView) => new DisabledDropHandler();

        private sealed class DisabledDropHandler : IDropHandler
        {
            public DragDropPointerEffects HandleDataDropped(DragDropInfo dragDropInfo)
            {
                return DragDropPointerEffects.None;
            }

            public void HandleDragCanceled()
            {
            }

            public DragDropPointerEffects HandleDraggingOver(DragDropInfo dragDropInfo)
            {
                return DragDropPointerEffects.None;
            }

            public DragDropPointerEffects HandleDragStarted(DragDropInfo dragDropInfo)
            {
                return DragDropPointerEffects.None;
            }

            public bool IsDropEnabled(DragDropInfo dragDropInfo)
            {
                // We specifically return true here because the default handling (what would be used if we returned false) of drag & drop ends up resulting in an error dialog.
                return true;
            }
        }
    }
}
