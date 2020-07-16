/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { RazorDocumentManager } from './RazorDocumentManager';
import { RazorDocumentSynchronizer } from './RazorDocumentSynchronizer';
import { RazorLanguageFeatureBase } from './RazorLanguageFeatureBase';
import { RazorLanguageServiceClient } from './RazorLanguageServiceClient';
import { RazorLogger } from './RazorLogger';
import { LanguageKind } from './RPC/LanguageKind';

export class RazorRenameProvider
    extends RazorLanguageFeatureBase
    implements vscode.RenameProvider {

    constructor(
        documentSynchronizer: RazorDocumentSynchronizer,
        documentManager: RazorDocumentManager,
        serviceClient: RazorLanguageServiceClient,
        logger: RazorLogger) {
        super(documentSynchronizer, documentManager, serviceClient, logger);
    }

    public async prepareRename(
        document: vscode.TextDocument,
        position: vscode.Position,
        token: vscode.CancellationToken) {

        const projection = await this.getProjection(document, position, token);
        if (!projection || projection.languageKind !== LanguageKind.CSharp) {
            // We only support C# renames for now. Reject the rename request.
            return Promise.reject('Cannot rename this symbol.');
        }

        // Let the rename go through. OmniSharp doesn't currently support "prepareRename" so we need to utilize document
        // APIs in order to resolve the appropriate rename range.
        const range = document.getWordRangeAtPosition(position);
        return range;
    }

    public async provideRenameEdits(
        document: vscode.TextDocument,
        position: vscode.Position,
        newName: string,
        token: vscode.CancellationToken) {

        const projection = await this.getProjection(document, position, token);
        if (!projection) {
            return;
        }

        if (projection.languageKind !== LanguageKind.CSharp) {
            // We only support C# renames for now.
            return;
        }

        // Send a rename command to Omnisharp which in turn would call our command to get the Razor documents re-mapped.
        const response = await vscode.commands.executeCommand<vscode.WorkspaceEdit>(
            'vscode.executeDocumentRenameProvider',
            projection.uri,
            projection.position,
            newName,
        );

        return response;
    }
}
