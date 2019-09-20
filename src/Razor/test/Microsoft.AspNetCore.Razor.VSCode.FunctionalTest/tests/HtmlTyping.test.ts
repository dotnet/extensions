/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as assert from 'assert';
import { afterEach, before, beforeEach } from 'mocha';
import * as path from 'path';
import * as vscode from 'vscode';
import {
    ensureNoChangesFor,
    pollUntil,
    simpleMvc21Root,
    waitForDocumentUpdate,
    waitForProjectReady,
} from './TestUtil';

let doc: vscode.TextDocument;
let editor: vscode.TextEditor;

suite('Html Typing', () => {
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

    test('Can auto-close start and end Html tags', async () => {
        const lastLine = new vscode.Position(doc.lineCount - 1, 0);
        await editor.edit(edit => edit.insert(lastLine, '<strong'));
        const lastLineEnd = new vscode.Position(doc.lineCount - 1, 7);
        await editor.edit(edit => edit.insert(lastLineEnd, '>'));

        doc = await waitForDocumentUpdate(doc.uri, document => document.getText().indexOf('</strong>') >= 0);

        const docLine = doc.lineAt(doc.lineCount - 1);
        assert.deepEqual(docLine.text, '<strong></strong>');
    });

    test('Does not auto-close self-closing Html tags', async () => {
        const lastLine = new vscode.Position(doc.lineCount - 1, 0);
        await editor.edit(edit => edit.insert(lastLine, '<input /'));
        const lastLineEnd = new vscode.Position(doc.lineCount - 1, 8);
        await editor.edit(edit => edit.insert(lastLineEnd, '>'));

        doc = await waitForDocumentUpdate(doc.uri, document => document.getText().indexOf('<input />') >= 0);

        await ensureNoChangesFor(doc.uri, 300);

        const docLine = doc.lineAt(doc.lineCount - 1);
        assert.deepEqual(docLine.text, '<input />');
    });

    test('Does not auto-close C# generics', async () => {
        const lastLine = new vscode.Position(doc.lineCount - 1, 0);
        await editor.edit(edit => edit.insert(lastLine, '@{new List<string}'));
        const lastLineEnd = new vscode.Position(doc.lineCount - 1, 17);
        await editor.edit(edit => edit.insert(lastLineEnd, '>'));

        doc = await waitForDocumentUpdate(doc.uri, document => document.getText().indexOf('<string>') >= 0);

        await ensureNoChangesFor(doc.uri, 300);

        const docLine = doc.lineAt(doc.lineCount - 1);
        assert.deepEqual(docLine.text, '@{new List<string>}');
    });
});
