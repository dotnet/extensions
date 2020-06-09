/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { beforeEach } from 'mocha';
import * as path from 'path';
import * as vscode from 'vscode';
import {
    assertHasCompletion,
    assertHasNoCompletion,
    simpleMvc21Root,
    waitForDocumentUpdate,
} from './TestUtil';

let doc: vscode.TextDocument;
let editor: vscode.TextEditor;

suite('Completions 2.1', () => {
    beforeEach(async () => {
        const filePath = path.join(simpleMvc21Root, 'Views', 'Home', 'Index.cshtml');
        doc = await vscode.workspace.openTextDocument(filePath);
        editor = await vscode.window.showTextDocument(doc);
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

        assertHasCompletion(completions, 'iframe');
    });

    test('Can complete C# code blocks', async () => {
        const lastLine = new vscode.Position(doc.lineCount - 1, 0);
        await editor.edit(edit => edit.insert(lastLine, '@{}'));
        await waitForDocumentUpdate(doc.uri, document => document.getText().indexOf('@{}') >= 0);

        const completions = await vscode.commands.executeCommand<vscode.CompletionList>(
            'vscode.executeCompletionItemProvider',
            doc.uri,
            new vscode.Position(doc.lineCount - 1, 2));

        assertHasCompletion(completions, 'DateTime');
        assertHasCompletion(completions, 'DateTimeKind');
        assertHasCompletion(completions, 'DateTimeOffset');
    });

    test('Can complete C# implicit expressions', async () => {
        const lastLine = new vscode.Position(doc.lineCount - 1, 0);
        await editor.edit(edit => edit.insert(lastLine, '@'));
        await waitForDocumentUpdate(doc.uri, document => document.lineAt(document.lineCount - 1).text === '@');

        const completions = await vscode.commands.executeCommand<vscode.CompletionList>(
            'vscode.executeCompletionItemProvider',
            doc.uri,
            new vscode.Position(doc.lineCount - 1, 1));

        assertHasCompletion(completions, 'DateTime');
        assertHasCompletion(completions, 'DateTimeKind');
        assertHasCompletion(completions, 'DateTimeOffset');
    });

    test('Can complete imported C#', async () => {
        const lastLine = new vscode.Position(doc.lineCount - 1, 0);
        await editor.edit(edit => edit.insert(lastLine, '@'));
        await waitForDocumentUpdate(doc.uri, document => document.lineAt(document.lineCount - 1).text === '@');

        const completions = await vscode.commands.executeCommand<vscode.CompletionList>(
            'vscode.executeCompletionItemProvider',
            doc.uri,
            new vscode.Position(doc.lineCount - 1, 1));

        assertHasCompletion(completions, 'TheTime');
    });

    test('Can complete Razor directive', async () => {
        const firstLine = new vscode.Position(0, 0);
        await editor.edit(edit => edit.insert(firstLine, '@\n'));
        const completions = await vscode.commands.executeCommand<vscode.CompletionList>(
            'vscode.executeCompletionItemProvider',
            doc.uri,
            new vscode.Position(0, 1));

        assertHasCompletion(completions, 'page');
        assertHasCompletion(completions, 'inject');
        assertHasNoCompletion(completions, 'div');
    });

    test('Can complete HTML tag', async () => {
        const lastLine = new vscode.Position(0, 0);
        await editor.edit(edit => edit.insert(lastLine, '<str'));
        const completions = await vscode.commands.executeCommand<vscode.CompletionList>(
            'vscode.executeCompletionItemProvider',
            doc.uri,
            new vscode.Position(0, 4));

        assertHasCompletion(completions, 'strong');
    });
});
