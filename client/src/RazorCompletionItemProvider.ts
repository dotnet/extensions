/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { RazorCSharpFeature } from './CSharp/RazorCSharpFeature';
import { RazorLanguageServiceClient } from './RazorLanguageServiceClient';
import { LanguageKind } from './RPC/LanguageKind';

export class RazorCompletionItemProvider implements vscode.CompletionItemProvider {
    constructor(
        private readonly csharpFeature: RazorCSharpFeature,
        private readonly serviceClient: RazorLanguageServiceClient) {
    }

    public async provideCompletionItems(
        document: vscode.TextDocument, position: vscode.Position,
        token: vscode.CancellationToken, context: vscode.CompletionContext) {
        const languageResponse = await this.serviceClient.languageQuery(position, document.uri);

        if (languageResponse.kind === LanguageKind.CSharp) {
            const projectionProvider = this.csharpFeature.projectionProvider;
            const projectedDocument = await projectionProvider.getDocument(document.uri);
            const projectedUri = projectedDocument.projectedUri;

            if (projectedUri) {
                const completions = await vscode
                    .commands
                    .executeCommand<vscode.CompletionList | vscode.CompletionItem[]>(
                        'vscode.executeCompletionItemProvider',
                        projectedUri,
                        languageResponse.position,
                        context.triggerCharacter);

                const completionItems =
                    completions instanceof Array ? completions  // was vscode.CompletionItem[]
                        : completions ? completions.items       // was vscode.CompletionList
                            : [];

                // There are times when the generated code will not line up with the content of the .cshtml file.
                // Therefore, we need to offset all completion items charactesr by a certain amount in order
                // to have proper completion. An example of this is typing @DateTime at the beginning of a line.
                // In the code behind it's represented as __o = DateTime.
                const completionCharacterOffset = languageResponse.position.character - position.character;
                for (const completionItem of completionItems) {
                    if (completionItem.range) {
                        const rangeStart = new vscode.Position(
                            position.line,
                            completionItem.range.start.character - completionCharacterOffset);
                        const rangeEnd = new vscode.Position(
                            position.line,
                            completionItem.range.end.character - completionCharacterOffset);
                        completionItem.range = new vscode.Range(rangeStart, rangeEnd);
                    }

                    // textEdit is deprecated in favor of .range. Clear out its value to avoid any unexpected behavior.
                    completionItem.textEdit = undefined;
                }

                const isIncomplete = completions instanceof Array ? true
                    : completions ? (<vscode.CompletionList>completions).isIncomplete 
                        : true;
                return new vscode.CompletionList(completionItems, isIncomplete);
            }
        }

        return { isIncomplete: true, items: [] } as vscode.CompletionList;
    }
}
