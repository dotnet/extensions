/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { IEventEmitterFactory } from '../IEventEmitterFactory';
import { RazorDocumentManager } from '../RazorDocumentManager';
import { RazorLanguageServiceClient } from '../RazorLanguageServiceClient';
import { RazorLogger } from '../RazorLogger';
import { HtmlPreviewPanel } from './HtmlPreviewPanel';
import { HtmlProjectedDocumentContentProvider } from './HtmlProjectedDocumentContentProvider';
import { HtmlTagCompletionProvider } from './HtmlTagCompletionProvider';

export class RazorHtmlFeature {
    public readonly projectionProvider: HtmlProjectedDocumentContentProvider;
    private readonly htmlTagCompletionProvider: HtmlTagCompletionProvider;
    private readonly htmlPreviewPanel: HtmlPreviewPanel;

    constructor(
        documentManager: RazorDocumentManager,
        serviceClient: RazorLanguageServiceClient,
        eventEmitterFactory: IEventEmitterFactory,
        logger: RazorLogger) {
        this.projectionProvider = new HtmlProjectedDocumentContentProvider(documentManager, eventEmitterFactory, logger);
        this.htmlTagCompletionProvider = new HtmlTagCompletionProvider(documentManager, serviceClient);
        this.htmlPreviewPanel = new HtmlPreviewPanel(documentManager);
    }

    public register() {
        const registrations = [
            vscode.workspace.registerTextDocumentContentProvider(
                HtmlProjectedDocumentContentProvider.scheme, this.projectionProvider),
            vscode.commands.registerCommand(
                'extension.showRazorHtmlWindow', () => this.htmlPreviewPanel.show()),
            this.htmlTagCompletionProvider.register(),
        ];

        if (vscode.window.registerWebviewPanelSerializer) {
            registrations.push(vscode.window.registerWebviewPanelSerializer(HtmlPreviewPanel.viewType, {
                deserializeWebviewPanel: async (panel: vscode.WebviewPanel) => {
                    await this.htmlPreviewPanel.revive(panel);
                },
            }));
        }

        return vscode.Disposable.from(...registrations);
    }
}
