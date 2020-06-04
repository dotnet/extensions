/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { CancellationToken } from 'vscode-jsonrpc';
import { RazorCodeLens } from './RazorCodeLens';
import { RazorDocumentChangeKind } from './RazorDocumentChangeKind';
import { RazorDocumentManager } from './RazorDocumentManager';
import { RazorDocumentSynchronizer } from './RazorDocumentSynchronizer';
import { RazorLanguageFeatureBase } from './RazorLanguageFeatureBase';
import { RazorLanguageServiceClient } from './RazorLanguageServiceClient';
import { RazorLogger } from './RazorLogger';
import { LanguageKind } from './RPC/LanguageKind';

export class RazorCodeLensProvider
    extends RazorLanguageFeatureBase
    implements vscode.CodeLensProvider {

    public onDidChangeCodeLenses: vscode.Event<void>;

    constructor(
        documentSynchronizer: RazorDocumentSynchronizer,
        documentManager: RazorDocumentManager,
        serviceClient: RazorLanguageServiceClient,
        logger: RazorLogger) {

        super(documentSynchronizer, documentManager, serviceClient, logger);

        const onCodeLensChangedEmitter = new vscode.EventEmitter<void>();
        this.onDidChangeCodeLenses = onCodeLensChangedEmitter.event;

        documentManager.onChange(async (event) => {
            if (event.kind !== RazorDocumentChangeKind.added) {
                return;
            }

            // Sometimes when a file already open in the editor is renamed, provideCodeLens would return empty
            // because the background C# document is not ready yet. So, when that happens we should manually invoke
            // a code lens refresh after waiting for a little while.
            const openDocumentUris = vscode.workspace.textDocuments.filter(doc => !doc.isClosed).map(doc => doc.uri);
            if (openDocumentUris.includes(event.document.uri)) {
                await new Promise(r => setTimeout(r, 5000));
                onCodeLensChangedEmitter.fire();
            }
        });
    }

    public async provideCodeLenses(document: vscode.TextDocument, token: vscode.CancellationToken) {
        try {
            const razorDocument = await this.documentManager.getDocument(document.uri);
            if (!razorDocument) {
                return;
            }

            const csharpDocument = razorDocument.csharpDocument;

            // Get all the code lenses that applies to our projected C# document.
            const codeLenses = await vscode.commands.executeCommand<vscode.CodeLens[]>(
                'vscode.executeCodeLensProvider',
                csharpDocument.uri) as vscode.CodeLens[];
            if (!codeLenses) {
                return;
            }

            // Re-map the CodeLens locations to the original Razor document.
            const remappedCodeLenses = new Array<vscode.CodeLens>();
            for (const codeLens of codeLenses) {
                const result = await this.serviceClient.mapToDocumentRanges(
                    LanguageKind.CSharp,
                    [codeLens.range],
                    razorDocument.uri);
                if (result && result.ranges.length > 0) {
                    const newCodeLens = new RazorCodeLens(result.ranges[0], razorDocument.uri, document, codeLens.command);
                    remappedCodeLenses.push(newCodeLens);
                } else {
                    // This means this CodeLens was for non-user code. We can safely ignore those.
                }
            }

            return remappedCodeLenses;

        } catch (error) {
            this.logger.logWarning(`provideCodeLens failed with ${error}`);
            return [];
        }
    }

    public async resolveCodeLens(codeLens: vscode.CodeLens, token: vscode.CancellationToken) {
        if (codeLens instanceof RazorCodeLens) {
            return this.resolveRazorCodeLens(codeLens, token);
        }
    }

    private async resolveRazorCodeLens(codeLens: RazorCodeLens, token: CancellationToken): Promise<vscode.CodeLens> {
        // Initialize with default values.
        codeLens.command = {
            title: '',
            command: '',
            arguments: [],
        };

        try {
            const razorDocument = await this.documentManager.getDocument(codeLens.uri);
            if (!razorDocument) {
                return codeLens;
            }

            // Make sure this CodeLens is for a valid location in the projected C# document.
            const projection = await this.getProjection(codeLens.document, codeLens.range.start, token);
            if (!projection || projection.languageKind !== LanguageKind.CSharp) {
                return codeLens;
            }

            const references = await vscode.commands.executeCommand<vscode.Location[]>(
                'vscode.executeReferenceProvider',
                projection.uri,
                projection.position) as vscode.Location[];

            // We now have a list of references to show in the CodeLens.
            const count = references.length;
            codeLens.command = {
                title: count === 1 ? '1 reference' : `${count} references`,
                command: 'editor.action.showReferences',
                arguments: [razorDocument.uri, codeLens.range.start, references],
            };

            return codeLens;

        } catch (error) {
            this.logger.logWarning(`resolveCodeLens failed with ${error}`);
            return codeLens;
        }
    }
}
