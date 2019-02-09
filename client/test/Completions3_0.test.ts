/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as assert from 'assert';
import * as path from 'path';
import * as vscode from 'vscode';
import { extensionActivated } from '../src/extension';
import {
    basicRazorApp30Root,
    csharpExtensionReady,
    dotnetRestore,
    htmlLanguageFeaturesExtensionReady,
    pollUntil,
} from './TestUtil';

describe('Completions 3.0', () => {
    before(async () => {
        await csharpExtensionReady();
        await htmlLanguageFeaturesExtensionReady();
        await dotnetRestore(basicRazorApp30Root);
    });

    beforeEach(async () => {
        await extensionActivated;
    });

    afterEach(async () => {
        await vscode.commands.executeCommand('workbench.action.revertAndCloseActiveEditor');
        await pollUntil(() => vscode.window.visibleTextEditors.length === 0, 1000);
    });

    it('Can complete Razor directive in .cshtml', async () => {
        const cshtmlFilePath = path.join(basicRazorApp30Root, 'Pages', 'Index.cshtml');
        const cshtmlDoc = await vscode.workspace.openTextDocument(cshtmlFilePath);
        const cshtmlEditor = await vscode.window.showTextDocument(cshtmlDoc);
        const firstLine = new vscode.Position(0, 0);
        await cshtmlEditor.edit(edit => edit.insert(firstLine, '@\n'));
        const completions = await vscode.commands.executeCommand<vscode.CompletionList>(
            'vscode.executeCompletionItemProvider',
            cshtmlDoc.uri,
            new vscode.Position(0, 1));

        const hasCompletion = (text: string) => completions!.items.some(item => item.insertText === text);

        assert.ok(hasCompletion('page'), 'Should have completion for "page"');
        assert.ok(hasCompletion('inject'), 'Should have completion for "inject"');
        assert.ok(!hasCompletion('div'), 'Should not have completion for "div"');
    });

    it('Can complete Razor directive in .razor', async () => {
        const razorFilePath = path.join(basicRazorApp30Root, 'Pages', 'Index.razor');
        const razorDoc = await vscode.workspace.openTextDocument(razorFilePath);
        const razorEditor = await vscode.window.showTextDocument(razorDoc);
        const firstLine = new vscode.Position(0, 0);
        await razorEditor.edit(edit => edit.insert(firstLine, '@\n'));
        const completions = await vscode.commands.executeCommand<vscode.CompletionList>(
            'vscode.executeCompletionItemProvider',
            razorDoc.uri,
            new vscode.Position(0, 1));

        const hasCompletion = (text: string) => completions!.items.some(item => item.insertText === text);

        assert.ok(hasCompletion('page'), 'Should have completion for "page"');
        assert.ok(hasCompletion('inject'), 'Should have completion for "inject"');
        assert.ok(!hasCompletion('div'), 'Should not have completion for "div"');
    });
});
