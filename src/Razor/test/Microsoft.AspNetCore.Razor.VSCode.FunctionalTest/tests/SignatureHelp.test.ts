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

suite('Signature help', () => {
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

    test('Can get signature help for JavaScript', async () => {
        const firstLine = new vscode.Position(0, 0);
        const codeToInsert = '<script>console.log(</script>';
        await editor.edit(edit => edit.insert(firstLine, codeToInsert));
        await waitForDocumentUpdate(doc.uri, document => document.getText().indexOf(codeToInsert) >= 0);

        const signatureHelp = await vscode.commands.executeCommand<vscode.SignatureHelp>(
            'vscode.executeSignatureHelpProvider',
            doc.uri,
            new vscode.Position(0, 20));
        const signatures = signatureHelp!.signatures;

        assert.equal(signatures.length, 1);
        assert.equal(signatures[0].label, 'log(message?: any, ...optionalParams: any[]): void');
    });

    test('Can get signature help for C#', async () => {
        const firstLine = new vscode.Position(0, 0);
        const codeToInsert = '@{ System.Console.WriteLine( }';
        await editor.edit(edit => edit.insert(firstLine, codeToInsert));
        await waitForDocumentUpdate(doc.uri, document => document.getText().indexOf(codeToInsert) >= 0);

        const signatureHelp = await vscode.commands.executeCommand<vscode.SignatureHelp>(
            'vscode.executeSignatureHelpProvider',
            doc.uri,
            new vscode.Position(firstLine.line, 28),
            '(');
        const signatures = signatureHelp!.signatures;
        assert.ok(signatures.some(s => s.label === 'void Console.WriteLine(bool value)'));
    });
});
