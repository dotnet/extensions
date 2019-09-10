/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as assert from 'assert';
import * as path from 'path';
import * as vscode from 'vscode';
import { extensionActivated } from '../src/extension';
import {
    basicRazorApp10Root,
    csharpExtensionReady,
    dotnetRestore,
    htmlLanguageFeaturesExtensionReady,
    pollUntil,
} from './TestUtil';

let doc: vscode.TextDocument;
let editor: vscode.TextEditor;

describe('Completions 1.0', () => {
    before(async () => {
        await csharpExtensionReady();
        await htmlLanguageFeaturesExtensionReady();
        await dotnetRestore(basicRazorApp10Root);
    });

    beforeEach(async () => {
        const filePath = path.join(basicRazorApp10Root, 'Views', 'Index.cshtml');
        doc = await vscode.workspace.openTextDocument(filePath);
        editor = await vscode.window.showTextDocument(doc);
        await extensionActivated;
    });

    afterEach(async () => {
        await vscode.commands.executeCommand('workbench.action.revertAndCloseActiveEditor');
        await pollUntil(() => vscode.window.visibleTextEditors.length === 0, 1000);
    });

    it('Can complete Razor directive', async () => {
        const firstLine = new vscode.Position(0, 0);
        await editor.edit(edit => edit.insert(firstLine, '@\n'));
        const completions = await vscode.commands.executeCommand<vscode.CompletionList>(
            'vscode.executeCompletionItemProvider',
            doc.uri,
            new vscode.Position(0, 1));

        const hasCompletion = (text: string) => completions!.items.some(item => item.insertText === text);

        assert.ok(!hasCompletion('page'), 'Should not have completion for "page"');
        assert.ok(hasCompletion('inject'), 'Should have completion for "inject"');
        assert.ok(!hasCompletion('div'), 'Should not have completion for "div"');
    });
});
