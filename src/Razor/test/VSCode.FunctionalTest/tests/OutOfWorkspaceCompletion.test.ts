/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as fs from 'fs';
import { after, afterEach, before } from 'mocha';
import * as path from 'path';
import * as vscode from 'vscode';
import {
    assertHasCompletion,
    pollUntil,
    testAppsRoot,
} from './TestUtil';

const outsideWorkspaceFile = path.join(testAppsRoot, '..', 'OutOfWorkspaceFile.razor');

suite('Out of workspace Completions', () => {
    before(async () => {
        fs.writeFileSync(outsideWorkspaceFile, /* data */ '');
    });

    after(async () => {
        fs.unlinkSync(outsideWorkspaceFile);
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

    test('Directive completions out of Workspace works', async () => {
        const outOfWorkspaceDoc = await vscode.workspace.openTextDocument(outsideWorkspaceFile);
        const outOfWorkspaceEditor = await vscode.window.showTextDocument(outOfWorkspaceDoc);
        const firstLine = new vscode.Position(0, 0);
        await outOfWorkspaceEditor.edit(edit => edit.insert(firstLine, '@inje'));

        const completions = await vscode.commands.executeCommand<vscode.CompletionList>(
            'vscode.executeCompletionItemProvider',
            outOfWorkspaceDoc.uri,
            new vscode.Position(0, 3));

        assertHasCompletion(completions, 'inject');
    });

    test('C# completions out of Workspace work', async () => {
        const outOfWorkspaceDoc = await vscode.workspace.openTextDocument(outsideWorkspaceFile);
        const outOfWorkspaceEditor = await vscode.window.showTextDocument(outOfWorkspaceDoc);
        const firstLine = new vscode.Position(0, 0);
        await outOfWorkspaceEditor.edit(edit => edit.insert(firstLine, '@Date'));
        const completions = await vscode.commands.executeCommand<vscode.CompletionList>(
            'vscode.executeCompletionItemProvider',
            outOfWorkspaceDoc.uri,
            new vscode.Position(0, 2));

        assertHasCompletion(completions, 'DateTime');
    });

    test('HTML completions out of Workspace work', async () => {
        const outOfWorkspaceDoc = await vscode.workspace.openTextDocument(outsideWorkspaceFile);
        const outOfWorkspaceEditor = await vscode.window.showTextDocument(outOfWorkspaceDoc);
        const firstLine = new vscode.Position(0, 0);
        await outOfWorkspaceEditor.edit(edit => edit.insert(firstLine, '<a'));
        const completions = await vscode.commands.executeCommand<vscode.CompletionList>(
            'vscode.executeCompletionItemProvider',
            outOfWorkspaceDoc.uri,
            new vscode.Position(0, 2));

        assertHasCompletion(completions, 'a');
    });
});
