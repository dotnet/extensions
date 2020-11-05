/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as assert from 'assert';
import { before, beforeEach } from 'mocha';
import * as path from 'path';
import * as vscode from 'vscode';
import {
    mvcWithComponentsRoot,
    pollUntil,
} from './TestUtil';

let razorPath: string;
let razorDoc: vscode.TextDocument;
let razorEditor: vscode.TextEditor;

suite('Code Actions', () => {
    before(function(this) {
        if (process.env.ci === 'true') {
            // Skipping on the CI as this consistently fails.
            this.skip();
        }
    });

    beforeEach(async () => {
        razorPath = path.join(mvcWithComponentsRoot, 'Views', 'Shared', 'NavMenu.razor');
        razorDoc = await vscode.workspace.openTextDocument(razorPath);
        razorEditor = await vscode.window.showTextDocument(razorDoc);
    });

    test('Can provide FullQualified CodeAction .razor file', async () => {
        const firstLine = new vscode.Position(0, 0);
        await MakeEditAndFindDiagnostic('@{ var x = new HtmlString("sdf"); }\n', firstLine);

        const position = new vscode.Position(0, 21);
        const codeActions = await GetCodeActions(razorDoc.uri, new vscode.Range(position, position));

        assert.equal(codeActions.length, 1);
        const codeAction = codeActions[0];
        assert.equal(codeAction.title, 'Microsoft.AspNetCore.Html.HtmlString');

        await DoCodeAction(razorDoc.uri, codeAction);
        const reloadedDoc = await vscode.workspace.openTextDocument(razorDoc.uri);
        const editedText = reloadedDoc.getText();
        assert.ok(editedText.includes('var x = new Microsoft.AspNetCore.Html.HtmlString("sdf");'));
    });

    async function DoCodeAction(fileUri: vscode.Uri, codeAction: vscode.Command) {
        let diagnosticsChanged = false;
        vscode.languages.onDidChangeDiagnostics(diagnosticsChangedEvent => {
            const diagnostics = vscode.languages.getDiagnostics(fileUri);
            if (diagnostics.length === 0) {
                diagnosticsChanged = true;
            }
        });

        if (codeAction.command && codeAction.arguments) {
            const result = await vscode.commands.executeCommand<boolean | string>(codeAction.command, codeAction.arguments[0]);
            console.log(result);
        }

        await pollUntil(() => {
            return diagnosticsChanged;
        }, /* timeout */ 20000, /* pollInterval */ 1000, false /* suppress timeout */);
    }

    async function MakeEditAndFindDiagnostic(editText: string, position: vscode.Position) {
        let diagnosticsChanged = false;
        vscode.languages.onDidChangeDiagnostics(diagnosticsChangedEvent => {
            const diagnostics = vscode.languages.getDiagnostics(razorDoc.uri);
            if (diagnostics.length > 0) {
                diagnosticsChanged = true;
            }
        });

        for (let i = 0; i < 3; i++) {
            await razorEditor.edit(edit => edit.insert(position, editText));
            await pollUntil(() => {
                return diagnosticsChanged;
            }, /* timeout */ 5000, /* pollInterval */ 1000, true /* suppress timeout */);
            if (diagnosticsChanged) {
                break;
            }
        }
    }

    async function GetCodeActions(fileUri: vscode.Uri, position: vscode.Range): Promise<vscode.Command[]> {
        return await vscode.commands.executeCommand('vscode.executeCodeActionProvider', fileUri, position) as vscode.Command[];
    }
});
