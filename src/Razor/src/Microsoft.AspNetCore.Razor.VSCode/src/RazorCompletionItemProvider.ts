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

            // There are times when the generated code will not line up with the content of the .razor/.cshtml file.
            // Therefore, we need to offset all completion items' characters by a certain amount in order
            // to have proper completion. An example of this is typing @DateTime at the beginning of a line.
            // In the code behind it's represented as __o = DateTime.
            const completionCharacterOffset = projectedPosition.character - hostDocumentPosition.character;
            for (const completionItem of completionItems) {
                const doc = completionItem.documentation as vscode.MarkdownString;
                if (doc) {
                    // Without this, the documentation doesn't get rendered in the editor.
                    const newDoc = new vscode.MarkdownString(doc.value);
                    newDoc.isTrusted = doc.isTrusted;
                    completionItem.documentation = newDoc;
                }

                if (completionItem.range) {
                    const range = completionItem.range;
                    const insertingRange = (range as any).inserting;
                    if (insertingRange) {
                        const insertingRangeStart = this.offsetColumn(completionCharacterOffset, hostDocumentPosition.line, insertingRange.start);
                        const insertingRangeEnd = this.offsetColumn(completionCharacterOffset, hostDocumentPosition.line, insertingRange.end);
                        (range as any).inserting = new vscode.Range(insertingRangeStart, insertingRangeEnd);
                    }

                    const replacingRange = (range as any).replacing;
                    if (replacingRange) {
                        const replacingRangeStart = this.offsetColumn(completionCharacterOffset, hostDocumentPosition.line, replacingRange.start);
                        const replacingRangeEnd = this.offsetColumn(completionCharacterOffset, hostDocumentPosition.line, replacingRange.end);
                        (range as any).replacing = new vscode.Range(replacingRangeStart, replacingRangeEnd);
                    }

                    if (range instanceof vscode.Range &&  range.start && range.end) {
                        const rangeStart = this.offsetColumn(completionCharacterOffset, hostDocumentPosition.line, range.start);
                        const rangeEnd = this.offsetColumn(completionCharacterOffset, hostDocumentPosition.line, range.end);
                        completionItem.range = new vscode.Range(rangeStart, rangeEnd);
                    }
                } else if ((completionItem as any).range2) {
                    const insertRange = (completionItem as any).range2.insert;
                    if (insertRange) {
                        const insertRangeStart = this.offsetColumn(completionCharacterOffset, hostDocumentPosition.line, insertRange.start);
                        const insertRangeEnd = this.offsetColumn(completionCharacterOffset, hostDocumentPosition.line, insertRange.end);
                        (completionItem as any).range2.insert = new vscode.Range(insertRangeStart, insertRangeEnd);
                    }

                    const replaceRange = (completionItem as any).range2.replace;
                    if (replaceRange) {
                        const replaceRangeStart = this.offsetColumn(completionCharacterOffset, hostDocumentPosition.line, replaceRange.start);
                        const replaceRangeEnd = this.offsetColumn(completionCharacterOffset, hostDocumentPosition.line, replaceRange.end);
                        (completionItem as any).range2.replace = new vscode.Range(replaceRangeStart, replaceRangeEnd);
                    }

                    const insertingRange = (completionItem as any).range2.inserting;
                    if (insertingRange) {
                        const insertingRangeStart = this.offsetColumn(completionCharacterOffset, hostDocumentPosition.line, insertingRange.start);
                        const insertingRangeEnd = this.offsetColumn(completionCharacterOffset, hostDocumentPosition.line, insertingRange.end);
                        (completionItem as any).range2.inserting = new vscode.Range(insertingRangeStart, insertingRangeEnd);
                    }

                    const replacingRange = (completionItem as any).range2.replacing;
                    if (replacingRange) {
                        const replacingRangeStart = this.offsetColumn(completionCharacterOffset, hostDocumentPosition.line, replacingRange.start);
                        const replacingRangeEnd = this.offsetColumn(completionCharacterOffset, hostDocumentPosition.line, replacingRange.end);
                        (completionItem as any).range2.replacing = new vscode.Range(replacingRangeStart, replacingRangeEnd);
                    }
                }

                // textEdit is deprecated in favor of .range. Clear out its value to avoid any unexpected behavior.
                completionItem.textEdit = undefined;

                if (triggerCharacter === '@' &&
                    completionItem.commitCharacters) {
                    // We remove `{` from the commit characters to prevent auto-completing the first completion item
                    // with a curly brace when a user intended to type `@{}`.
                    completionItem.commitCharacters = completionItem.commitCharacters.filter(
                        commitChar => commitChar !== '{');
                }
            }

            const isIncomplete = completions instanceof Array ? false
                : completions ? completions.isIncomplete
                    : false;
            return new vscode.CompletionList(completionItems, isIncomplete);
        }
    }

    private static offsetColumn(offset: number, hostDocumentLine: number, projectedPosition: vscode.Position) {
        const offsetPosition = new vscode.Position(
            hostDocumentLine,
            projectedPosition.character - offset);
        return offsetPosition;
    }

    constructor(
        documentSynchronizer: RazorDocumentSynchronizer,
        documentManager: RazorDocumentManager,
        serviceClient: RazorLanguageServiceClient,
        private readonly provisionalCompletionOrchestrator: ProvisionalCompletionOrchestrator,
        logger: RazorLogger) {
        super(documentSynchronizer, documentManager, serviceClient, logger);
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
