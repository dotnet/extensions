/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { RazorDocumentManager } from '../RazorDocumentManager';
import { RazorDocumentSynchronizer } from '../RazorDocumentSynchronizer';
import { RazorLanguageFeatureBase } from '../RazorLanguageFeatureBase';
import { RazorLanguageServiceClient } from '../RazorLanguageServiceClient';
import { RazorLogger } from '../RazorLogger';
import { CompositeCodeActionTranslator } from './CompositeRazorCodeActionTranslator';

export class RazorCodeActionProvider
    extends RazorLanguageFeatureBase
    implements vscode.CodeActionProvider {

    constructor(
        documentSynchronizer: RazorDocumentSynchronizer,
        documentManager: RazorDocumentManager,
        serviceClient: RazorLanguageServiceClient,
        logger: RazorLogger,
        private readonly compositeCodeActionTranslator: CompositeCodeActionTranslator,
        ) {
        super(documentSynchronizer, documentManager, serviceClient, logger);
    }

    public async provideCodeActions(
        document: vscode.TextDocument,
        range: vscode.Range | vscode.Selection,
        context: vscode.CodeActionContext,
        token: vscode.CancellationToken) {
        try {
            const startPosition = new vscode.Position(range.start.line, range.start.character);
            const startProjection = await this.getProjection(document, startPosition, token);
            if (!startProjection) {
                return null;
            }

            const endPosition = new vscode.Position(range.end.line, range.end.character);
            const endProjection = await this.getProjection(document, endPosition, token);
            if (!endProjection) {
                return null;
            }

            // This is just a sanity check, they should always be the same.
            if (startProjection.uri !== endProjection.uri) {
                return null;
            }

            const projectedRange = new vscode.Range(startProjection.position, endProjection.position);

            const codeActions = await vscode.commands.executeCommand<vscode.Command[]>(
                'vscode.executeCodeActionProvider',
                startProjection.uri,
                projectedRange) as vscode.Command[];

            if (codeActions.length > 0) {
                const result = this.filterCodeActions(codeActions, context, document);

                return result;
            }

            return null;
        } catch (error) {
            this.logger.logWarning(`provideCodeActions failed with ${error}`);
            return null;
        }
    }

    private filterCodeActions(codeActions: vscode.Command[], context: vscode.CodeActionContext, document: vscode.TextDocument) {
        const result = new Array<vscode.Command>();
        for (const codeAction of codeActions) {
            if (this.compositeCodeActionTranslator.canHandleCodeAction(codeAction, context, document)) {
                result.push(codeAction);
            }
        }

        return result;
    }
}
