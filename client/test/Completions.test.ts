/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as assert from 'assert';
import * as path from 'path';
import * as vscode from 'vscode';
import { extensionActivated } from '../src/extension';
import { basicRazorAppRoot } from './TestUtil';

let doc: vscode.TextDocument;
let editor: vscode.TextEditor;

describe('Completions', () => {
    beforeEach(async () => {
        const filePath = path.join(basicRazorAppRoot, 'Pages', 'Index.cshtml');
        doc = await vscode.workspace.openTextDocument(filePath);
        editor = await vscode.window.showTextDocument(doc);
        await extensionActivated;
    });

    afterEach(async () => {
        await vscode.commands.executeCommand('workbench.action.revertAndCloseActiveEditor');
    });

    it('Can complete HTML tag', async () => {
        const lastLine = new vscode.Position(doc.lineCount - 1, 0);
        await editor.edit(edit => edit.insert(lastLine, '<str'));
        const completions = await vscode.commands.executeCommand<vscode.CompletionList>(
            'vscode.executeCompletionItemProvider',
            doc.uri,
            new vscode.Position(doc.lineCount - 1, 4));
        const matchingCompletions = completions!.items
            .filter(item => (typeof item.insertText === 'string') && item.insertText.startsWith('str'))
            .map(item => item.insertText as string);

        assert.deepEqual(matchingCompletions, ['strong']);
    });

    it('Can complete Razor directive', async () => {
        const firstLine = new vscode.Position(0, 0);
        await editor.edit(edit => edit.insert(firstLine, '@\n'));
        const completions = await vscode.commands.executeCommand<vscode.CompletionList>(
            'vscode.executeCompletionItemProvider',
            doc.uri,
            new vscode.Position(0, 1));

        const hasCompletion = (text: string) => completions!.items.some(item => item.insertText === text);

        assert.ok(hasCompletion('page'), 'Should have completion for "page"');
        assert.ok(hasCompletion('inject'), 'Should have completion for "inject"');
        assert.ok(!hasCompletion('div'), 'Should not have completion for "div"');
    });
});
