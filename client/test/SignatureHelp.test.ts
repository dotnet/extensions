/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as assert from 'assert';
import * as path from 'path';
import * as vscode from 'vscode';
import { extensionActivated } from '../src/extension';
import {
    basicRazorAppRoot,
    csharpExtensionReady,
    dotnetRestore,
    htmlLanguageFeaturesExtensionReady,
    pollUntil,
    waitForDocumentUpdate,
} from './TestUtil';

let doc: vscode.TextDocument;
let editor: vscode.TextEditor;

describe('Signature help', () => {
    before(async () => {
        await htmlLanguageFeaturesExtensionReady();
        await csharpExtensionReady();
        await dotnetRestore(basicRazorAppRoot);
    });

    beforeEach(async () => {
        const filePath = path.join(basicRazorAppRoot, 'Pages', 'Index.cshtml');
        doc = await vscode.workspace.openTextDocument(filePath);
        editor = await vscode.window.showTextDocument(doc);
        await extensionActivated;
    });

    afterEach(async () => {
        await vscode.commands.executeCommand('workbench.action.revertAndCloseActiveEditor');
        await pollUntil(() => vscode.window.visibleTextEditors.length === 0, 1000);
    });

    it('Can get signature help for JavaScript', async () => {
        const lastLine = new vscode.Position(0, 0);
        const codeToInsert = '<script>console.log(</script>';
        await editor.edit(edit => edit.insert(lastLine, codeToInsert));
        await waitForDocumentUpdate(doc.uri, document => document.getText().indexOf(codeToInsert) >= 0);

        const signatureHelp = await vscode.commands.executeCommand<vscode.SignatureHelp>(
            'vscode.executeSignatureHelpProvider',
            doc.uri,
            new vscode.Position(0, 20));
        const signatures = signatureHelp!.signatures;

        assert.equal(signatures.length, 1);
        assert.equal(signatures[0].label, 'log(message?: any, ...optionalParams: any[]): void');
    });

    it('Can get signature help for C#', async () => {
        const csharpLine = new vscode.Position(3, 0);
        const codeToInsert = 'System.Console.WriteLine(';
        await editor.edit(edit => edit.insert(csharpLine, codeToInsert));
        await waitForDocumentUpdate(doc.uri, document => document.getText().indexOf(codeToInsert) >= 0);

        const signatureHelp = await vscode.commands.executeCommand<vscode.SignatureHelp>(
            'vscode.executeSignatureHelpProvider',
            doc.uri,
            new vscode.Position(3, codeToInsert.length),
            '(');
        const signatures = signatureHelp!.signatures;
        assert.ok(signatures.some(s => s.label === 'void Console.WriteLine(bool value)'));
    });
});
