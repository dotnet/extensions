/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as assert from 'assert';
import { afterEach, before, beforeEach } from 'mocha';
import * as path from 'path';
import * as vscode from 'vscode';
import {
    componentRoot,
    pollUntil,
    waitForProjectReady,
} from './TestUtil';

let counterDoc: vscode.TextDocument;
let counterEditor: vscode.TextEditor;

suite('Completions Components', () => {
    before(async () => {
        await waitForProjectReady(componentRoot);
    });

    beforeEach(async () => {
        const counterPath = path.join(componentRoot, 'Components', 'Pages', 'Counter.razor');
        counterDoc = await vscode.workspace.openTextDocument(counterPath);
        counterEditor = await vscode.window.showTextDocument(counterDoc);
    });

    afterEach(async () => {
        await vscode.commands.executeCommand('workbench.action.revertAndCloseActiveEditor');
        await pollUntil(async () => {
            await vscode.commands.executeCommand('workbench.action.closeAllEditors');
            if (vscode.window.visibleTextEditors.length === 0) {
                return true;
            }

            return false;
        }, /* timeout */ 3000, /* pollInterval */ 500, true /* suppress timeout */);
    });

    test('Can perform Completions on directive attributes', async () => {
        const firstLine = new vscode.Position(1, 0);
        await counterEditor.edit(edit => edit.insert(firstLine, '<Microsoft.AspNetCore.Components.Forms.EditForm OnV'));

        const completions = await vscode.commands.executeCommand<vscode.CompletionList>(
            'vscode.executeCompletionItemProvider',
            counterDoc.uri,
            new vscode.Position(1, 50));
        const matchingCompletions = completions!.items
            .filter(item => item.label === 'OnValidSubmit')
            .map(item => item.label as string);

        assert.deepEqual(matchingCompletions, ['OnValidSubmit']);
    });
});
