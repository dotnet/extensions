/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { IEventEmitterFactory } from '../IEventEmitterFactory';
import { RazorDocumentManager } from '../RazorDocumentManager';
import { RazorLogger } from '../RazorLogger';
import { CSharpPreviewPanel } from './CSharpPreviewPanel';
import { CSharpProjectedDocumentContentProvider } from './CSharpProjectedDocumentContentProvider';

export class RazorCSharpFeature {
    public readonly projectionProvider: CSharpProjectedDocumentContentProvider;
    private readonly csharpPreviewPanel: CSharpPreviewPanel;

    constructor(
        documentManager: RazorDocumentManager,
        eventEmitterFactory: IEventEmitterFactory,
        logger: RazorLogger) {
        this.projectionProvider = new CSharpProjectedDocumentContentProvider(documentManager, eventEmitterFactory, logger);
        this.csharpPreviewPanel = new CSharpPreviewPanel(documentManager);
    }

    public register() {
        const registrations = [
            vscode.workspace.registerTextDocumentContentProvider(
                CSharpProjectedDocumentContentProvider.scheme, this.projectionProvider),
            vscode.commands.registerCommand(
                'extension.showRazorCSharpWindow', () => this.csharpPreviewPanel.show()),
        ];

        if (vscode.window.registerWebviewPanelSerializer) {
            registrations.push(vscode.window.registerWebviewPanelSerializer(CSharpPreviewPanel.viewType, {
                deserializeWebviewPanel: async (panel: vscode.WebviewPanel) => {
                    await this.csharpPreviewPanel.revive(panel);
                },
            }));
        }

        return vscode.Disposable.from(...registrations);
    }
}
