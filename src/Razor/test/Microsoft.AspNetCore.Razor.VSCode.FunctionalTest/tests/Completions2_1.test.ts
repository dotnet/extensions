/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as assert from 'assert';
import { afterEach, before, beforeEach } from 'mocha';
import * as path from 'path';
import * as vscode from 'vscode';
import {
    pollUntil,
    simpleMvc21Root,
    waitForDocumentUpdate,
    waitForProjectReady,
} from './TestUtil';

let doc: vscode.TextDocument;
let editor: vscode.TextEditor;

suite('Completions 2.1', () => {
    before(async () => {
        await waitForProjectReady(simpleMvc21Root);
    });

    beforeEach(async () => {
        const filePath = path.join(simpleMvc21Root, 'Views', 'Home', 'Index.cshtml');
        doc = await vscode.workspace.openTextDocument(filePath);
        editor = await vscode.window.showTextDocument(doc);
    });

    afterEach(async () => {
        await vscode.commands.executeCommand('workbench.action.revertAndCloseActiveEditor');
        await pollUntil(() => vscode.window.visibleTextEditors.length === 0, 1000);
    });

    test('Can get HTML completions on document open', async () => {
        // This test relies on the Index.cshtml document containing at least 1 HTML tag in it.
        // For the purposes of this test it locates that tag and tries to get the Html completion
        // list from it.

        const content = doc.getText();
        const tagNameIndex = content.indexOf('<') + 1;
        const docPosition = doc.positionAt(tagNameIndex);
        const completions = await vscode.commands.executeCommand<vscode.CompletionList>(
            'vscode.executeCompletionItemProvider',
            doc.uri,
            docPosition);
        const matchingCompletions = completions!.items
            .filter(item => (typeof item.insertText === 'string') && item.insertText === 'iframe')
            .map(item => item.insertText as string);

        assert.deepEqual(matchingCompletions, ['iframe']);
    });

    test('Can complete C# code blocks', async () => {
        const lastLine = new vscode.Position(doc.lineCount - 1, 0);
        await editor.edit(edit => edit.insert(lastLine, '@{}'));
        await waitForDocumentUpdate(doc.uri, document => document.getText().indexOf('@{}') >= 0);

        const completions = await vscode.commands.executeCommand<vscode.CompletionList>(
            'vscode.executeCompletionItemProvider',
            doc.uri,
            new vscode.Position(doc.lineCount - 1, 2));
        const matchingCompletions = completions!.items
            .filter(item => (typeof item.insertText === 'string') && item.insertText.startsWith('DateTime'))
            .map(item => item.insertText as string);

        assert.deepEqual(matchingCompletions, ['DateTime', 'DateTimeKind', 'DateTimeOffset']);
    });

    test('Can complete C# implicit expressions', async () => {
        const lastLine = new vscode.Position(doc.lineCount - 1, 0);
        await editor.edit(edit => edit.insert(lastLine, '@'));
        await waitForDocumentUpdate(doc.uri, document => document.lineAt(document.lineCount - 1).text === '@');

        const completions = await vscode.commands.executeCommand<vscode.CompletionList>(
            'vscode.executeCompletionItemProvider',
            doc.uri,
            new vscode.Position(doc.lineCount - 1, 1));
        const matchingCompletions = completions!.items
            .filter(item => (typeof item.insertText === 'string') && item.insertText.startsWith('DateTime'))
            .map(item => item.insertText as string);

        assert.deepEqual(matchingCompletions, ['DateTime', 'DateTimeKind', 'DateTimeOffset']);
    });

    test('Can complete imported C#', async () => {
        const lastLine = new vscode.Position(doc.lineCount - 1, 0);
        await editor.edit(edit => edit.insert(lastLine, '@'));
        await waitForDocumentUpdate(doc.uri, document => document.lineAt(document.lineCount - 1).text === '@');

        const completions = await vscode.commands.executeCommand<vscode.CompletionList>(
            'vscode.executeCompletionItemProvider',
            doc.uri,
            new vscode.Position(doc.lineCount - 1, 1));
        const matchingCompletions = completions!.items
            .filter(item => (typeof item.insertText === 'string') && item.insertText.startsWith('TheTime'))
            .map(item => item.insertText as string);

        assert.deepEqual(matchingCompletions, ['TheTime']);
    });

    test('Can complete Razor directive', async () => {
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

    test('Can complete HTML tag', async () => {
        const lastLine = new vscode.Position(0, 0);
        await editor.edit(edit => edit.insert(lastLine, '<str'));
        const completions = await vscode.commands.executeCommand<vscode.CompletionList>(
            'vscode.executeCompletionItemProvider',
            doc.uri,
            new vscode.Position(0, 4));
        const matchingCompletions = completions!.items
            .filter(item => (typeof item.insertText === 'string') && item.insertText.startsWith('str'))
            .map(item => item.insertText as string);

        assert.deepEqual(matchingCompletions, ['strong']);
    });
});
