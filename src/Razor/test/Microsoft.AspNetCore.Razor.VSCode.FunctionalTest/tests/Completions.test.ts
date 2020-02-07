/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { afterEach, before, beforeEach } from 'mocha';
import * as path from 'path';
import * as vscode from 'vscode';
import {
    assertHasCompletion,
    assertHasNoCompletion,
    mvcWithComponentsRoot,
    pollUntil,
    waitForDocumentUpdate,
    waitForProjectReady,
} from './TestUtil';

let cshtmlDoc: vscode.TextDocument;
let editor: vscode.TextEditor;
const homeDirectory = path.join(mvcWithComponentsRoot, 'Views', 'Home');

suite('Completions', () => {
    before(async () => {
        await waitForProjectReady(mvcWithComponentsRoot);
    });

    beforeEach(async () => {
        const filePath = path.join(homeDirectory, 'Index.cshtml');
        cshtmlDoc = await vscode.workspace.openTextDocument(filePath);
        editor = await vscode.window.showTextDocument(cshtmlDoc);
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

    test('Can complete Razor directive in .razor', async () => {
        const razorFilePath = path.join(mvcWithComponentsRoot, 'Views', 'Shared', 'NavMenu.razor');
        const razorDoc = await vscode.workspace.openTextDocument(razorFilePath);
        const razorEditor = await vscode.window.showTextDocument(razorDoc);
        const firstLine = new vscode.Position(0, 0);
        await razorEditor.edit(edit => edit.insert(firstLine, '@\n'));
        const completions = await vscode.commands.executeCommand<vscode.CompletionList>(
            'vscode.executeCompletionItemProvider',
            razorDoc.uri,
            new vscode.Position(0, 1));

        assertHasCompletion(completions, 'page');
        assertHasCompletion(completions, 'inject');
        assertHasNoCompletion(completions, 'div');
    });

    test('Can complete Razor directive in .cshtml', async () => {
        const firstLine = new vscode.Position(0, 0);
        await editor.edit(edit => edit.insert(firstLine, '@\n'));
        const completions = await vscode.commands.executeCommand<vscode.CompletionList>(
            'vscode.executeCompletionItemProvider',
            cshtmlDoc.uri,
            new vscode.Position(0, 1));

        assertHasCompletion(completions, 'page');
        assertHasCompletion(completions, 'inject');
        assertHasNoCompletion(completions, 'div');
    });

    test('Can complete C# code blocks in .cshtml', async () => {
        const lastLine = new vscode.Position(cshtmlDoc.lineCount - 1, 0);
        await editor.edit(edit => edit.insert(lastLine, '@{}'));
        await waitForDocumentUpdate(cshtmlDoc.uri, document => document.getText().indexOf('@{}') >= 0);

        const completions = await vscode.commands.executeCommand<vscode.CompletionList>(
            'vscode.executeCompletionItemProvider',
            cshtmlDoc.uri,
            new vscode.Position(cshtmlDoc.lineCount - 1, 2));

        assertHasCompletion(completions, 'DateTime');
        assertHasCompletion(completions, 'DateTimeKind');
        assertHasCompletion(completions, 'DateTimeOffset');
    });

    test('Can complete C# implicit expressions in .cshtml', async () => {
        const lastLine = new vscode.Position(cshtmlDoc.lineCount - 1, 0);
        await editor.edit(edit => edit.insert(lastLine, '@'));
        await waitForDocumentUpdate(cshtmlDoc.uri, document => document.lineAt(document.lineCount - 1).text === '@');

        const completions = await vscode.commands.executeCommand<vscode.CompletionList>(
            'vscode.executeCompletionItemProvider',
            cshtmlDoc.uri,
            new vscode.Position(lastLine.line, 1));

        assertHasCompletion(completions, 'DateTime');
        assertHasCompletion(completions, 'DateTimeKind');
        assertHasCompletion(completions, 'DateTimeOffset');
    });

    test('Can complete imported C# in .cshtml', async () => {
        const lastLine = new vscode.Position(cshtmlDoc.lineCount - 1, 0);
        await editor.edit(edit => edit.insert(lastLine, '@'));
        await waitForDocumentUpdate(cshtmlDoc.uri, document => document.lineAt(document.lineCount - 1).text === '@');

        const completions = await vscode.commands.executeCommand<vscode.CompletionList>(
            'vscode.executeCompletionItemProvider',
            cshtmlDoc.uri,
            new vscode.Position(cshtmlDoc.lineCount - 1, 1));

        assertHasCompletion(completions, 'TheTime');
    });

    test('Can complete HTML tag in .cshtml', async () => {
        const lastLine = new vscode.Position(0, 0);
        await editor.edit(edit => edit.insert(lastLine, '<str'));
        const completions = await vscode.commands.executeCommand<vscode.CompletionList>(
            'vscode.executeCompletionItemProvider',
            cshtmlDoc.uri,
            new vscode.Position(0, 4));

        assertHasCompletion(completions, 'strong');
    });
});
