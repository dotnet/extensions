/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { beforeEach } from 'mocha';
import * as path from 'path';
import * as vscode from 'vscode';
import {
    assertHasCompletion,
    componentRoot,
} from './TestUtil';

let counterDoc: vscode.TextDocument;
let counterEditor: vscode.TextEditor;
const pagesDirectory = path.join(componentRoot, 'Components', 'Pages');

suite('Completions Components', () => {
    beforeEach(async () => {
        const counterPath =  path.join(pagesDirectory, 'Counter.razor');
        counterDoc = await vscode.workspace.openTextDocument(counterPath);
        counterEditor = await vscode.window.showTextDocument(counterDoc);
    });

    test('Can perform Completions on directive attributes', async () => {
        const firstLine = new vscode.Position(1, 0);
        await counterEditor.edit(edit => edit.insert(firstLine, '<Microsoft.AspNetCore.Components.Forms.EditForm OnV'));

        const completions = await vscode.commands.executeCommand<vscode.CompletionList>(
            'vscode.executeCompletionItemProvider',
            counterDoc.uri,
            new vscode.Position(1, 50));

        assertHasCompletion(completions, 'OnValidSubmit');
    });
});
