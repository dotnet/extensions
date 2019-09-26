/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { IEventEmitterFactory } from '../IEventEmitterFactory';
import { RazorDocumentManager } from '../RazorDocumentManager';
import { RazorLogger } from '../RazorLogger';
import { CSharpPreviewDocumentContentProvider } from './CSharpPreviewDocumentContentProvider';
import { CSharpProjectedDocumentContentProvider } from './CSharpProjectedDocumentContentProvider';

export class RazorCSharpFeature {
    public readonly projectionProvider: CSharpProjectedDocumentContentProvider;
    public readonly previewProvider: CSharpPreviewDocumentContentProvider;

    constructor(
        documentManager: RazorDocumentManager,
        eventEmitterFactory: IEventEmitterFactory,
        logger: RazorLogger) {
        this.projectionProvider = new CSharpProjectedDocumentContentProvider(documentManager, eventEmitterFactory, logger);
        this.previewProvider = new CSharpPreviewDocumentContentProvider(documentManager);
    }

    public register() {
        const registrations = [
            vscode.workspace.registerTextDocumentContentProvider(
                CSharpProjectedDocumentContentProvider.scheme, this.projectionProvider),
            vscode.workspace.registerTextDocumentContentProvider(
                CSharpPreviewDocumentContentProvider.scheme, this.previewProvider),
            vscode.commands.registerCommand(
                'extension.showRazorCSharpWindow', () => this.previewProvider.showRazorCSharpWindow()),
        ];

        return vscode.Disposable.from(...registrations);
    }
}
