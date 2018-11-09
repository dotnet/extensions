/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { ProvisionalCompletionOrchestrator } from './ProvisionalCompletionOrchestrator';
import { RazorDocumentManager } from './RazorDocumentManager';
import { RazorDocumentSynchronizer } from './RazorDocumentSynchronizer';
import { RazorLanguageFeatureBase } from './RazorLanguageFeatureBase';
import { RazorLanguageServiceClient } from './RazorLanguageServiceClient';
import { RazorLogger } from './RazorLogger';
import { getUriPath } from './UriPaths';

export class RazorCompletionItemProvider
    extends RazorLanguageFeatureBase
    implements vscode.CompletionItemProvider {

    public static async getCompletions(
        projectedUri: vscode.Uri, hostDocumentPosition: vscode.Position,
        projectedPosition: vscode.Position, triggerCharacter: string | undefined) {

        if (projectedUri) {
            const completions = await vscode
                .commands
                .executeCommand<vscode.CompletionList | vscode.CompletionItem[]>(
                    'vscode.executeCompletionItemProvider',
                    projectedUri,
                    projectedPosition,
                    triggerCharacter);

            const completionItems =
                completions instanceof Array ? completions  // was vscode.CompletionItem[]
                    : completions ? completions.items       // was vscode.CompletionList
                        : [];

            // There are times when the generated code will not line up with the content of the .cshtml file.
            // Therefore, we need to offset all completion items charactesr by a certain amount in order
            // to have proper completion. An example of this is typing @DateTime at the beginning of a line.
            // In the code behind it's represented as __o = DateTime.
            const completionCharacterOffset = projectedPosition.character - hostDocumentPosition.character;
            for (const completionItem of completionItems) {
                if (completionItem.range) {
                    const rangeStart = new vscode.Position(
                        hostDocumentPosition.line,
                        completionItem.range.start.character - completionCharacterOffset);
                    const rangeEnd = new vscode.Position(
                        hostDocumentPosition.line,
                        completionItem.range.end.character - completionCharacterOffset);
                    completionItem.range = new vscode.Range(rangeStart, rangeEnd);
                }

                // textEdit is deprecated in favor of .range. Clear out its value to avoid any unexpected behavior.
                completionItem.textEdit = undefined;
            }

            const isIncomplete = completions instanceof Array ? false
                : completions ? completions.isIncomplete
                    : false;
            return new vscode.CompletionList(completionItems, isIncomplete);
        }
    }

    constructor(
        documentSynchronizer: RazorDocumentSynchronizer,
        documentManager: RazorDocumentManager,
        serviceClient: RazorLanguageServiceClient,
        private readonly provisionalCompletionOrchestrator: ProvisionalCompletionOrchestrator,
        private readonly logger: RazorLogger) {
        super(documentSynchronizer, documentManager, serviceClient);
    }

    public async provideCompletionItems(
        document: vscode.TextDocument, position: vscode.Position,
        token: vscode.CancellationToken, context: vscode.CompletionContext) {
        const projection = await this.getProjection(document, position, token);

        if (this.logger.verboseEnabled) {
            this.logger.logVerbose(`Providing completions for document ${getUriPath(document.uri)} ` +
                `at location (${position.line}, ${position.character})`);
        }

        if (!projection) {
            return { isIncomplete: true, items: [] } as vscode.CompletionList;
        }

        const provisionalCompletions = await this.provisionalCompletionOrchestrator.tryGetProvisionalCompletions(
            document.uri,
            projection,
            context);
        if (provisionalCompletions) {
            return provisionalCompletions;
        }

        // Not a provisional completion

        const completionList = await RazorCompletionItemProvider.getCompletions(
            projection.uri,
            position,
            projection.position,
            context.triggerCharacter);
        return completionList;
    }
}
