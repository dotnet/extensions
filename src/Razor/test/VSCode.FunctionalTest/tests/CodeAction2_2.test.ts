/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as assert from 'assert';
import { before, beforeEach } from 'mocha';
import * as path from 'path';
import * as vscode from 'vscode';
import {
    pollUntil,
    simpleMvc22Root,
} from './TestUtil';

let cshtmlDoc: vscode.TextDocument;
let editor: vscode.TextEditor;

suite('Code Actions 2.2', () => {
    before(function(this) {
        if (process.env.ci === 'true') {
            // Skipping on the CI as this consistently fails.
            this.skip();
        }
    });

    beforeEach(async () => {
        const filePath = path.join(simpleMvc22Root, 'Views', 'Home', 'Index.cshtml');
        cshtmlDoc = await vscode.workspace.openTextDocument(filePath);
        editor = await vscode.window.showTextDocument(cshtmlDoc);
    });

    test('Can provide FullQualified CodeAction 2.2 .cshtml file', async () => {
        const firstLine = new vscode.Position(0, 0);
        await MakeEditAndFindDiagnostic('@{ var x = new HtmlString("sdf"); }\n', firstLine);

        const position = new vscode.Position(0, 21);
        const codeActions = await GetCodeActions(cshtmlDoc.uri, new vscode.Range(position, position));

        assert.equal(codeActions.length, 1);
        const codeAction = codeActions[0];
        assert.equal(codeAction.title, 'Microsoft.AspNetCore.Html.HtmlString');

        await DoCodeAction(cshtmlDoc.uri, codeAction);
        const reloadedDoc = await vscode.workspace.openTextDocument(cshtmlDoc.uri);
        const editedText = reloadedDoc.getText();
        assert.ok(editedText.includes('var x = new Microsoft.AspNetCore.Html.HtmlString("sdf");'));
    });

    async function MakeEditAndFindDiagnostic(editText: string, position: vscode.Position) {
        let diagnosticsChanged = false;
        vscode.languages.onDidChangeDiagnostics(diagnosticsChangedEvent => {
            const diagnostics = vscode.languages.getDiagnostics(cshtmlDoc.uri);
            if (diagnostics.length > 0) {
                diagnosticsChanged = true;
            }
        });

        for (let i = 0; i < 3; i++) {
            await editor.edit(edit => edit.insert(position, editText));
            await pollUntil(() => {
                return diagnosticsChanged;
            }, /* timeout */ 5000, /* pollInterval */ 1000, true /* suppress timeout */);
            if (diagnosticsChanged) {
                break;
            }
        }
    }

    async function DoCodeAction(fileUri: vscode.Uri, codeAction: vscode.Command) {
        let diagnosticsChanged = false;
        vscode.languages.onDidChangeDiagnostics(diagnosticsChangedEvent => {
            const diagnostics = vscode.languages.getDiagnostics(fileUri);
            if (diagnostics.length === 0) {
                diagnosticsChanged = true;
            }
        });

        if (codeAction.command && codeAction.arguments) {
            vscode.commands.executeCommand(codeAction.command, codeAction.arguments);
        }

        await pollUntil(() => {
            return diagnosticsChanged;
        }, /* timeout */ 20000, /* pollInterval */ 1000, false /* suppress timeout */);
    }

    async function GetCodeActions(fileUri: vscode.Uri, position: vscode.Range): Promise<vscode.Command[]> {
        let diagnosticsChanged = false;
        vscode.languages.onDidChangeDiagnostics(diagnosticsChangedEvent => {
            const diagnostics = vscode.languages.getDiagnostics(fileUri);
            if (diagnostics.length > 0) {
                diagnosticsChanged = true;
            }
        });

        await pollUntil(() => {
            return diagnosticsChanged;
        }, /* timeout */ 20000, /* pollInterval */ 1000, false /* suppress timeout */);

        return await vscode.commands.executeCommand('vscode.executeCodeActionProvider', fileUri, position) as vscode.Command[];
    }
});
