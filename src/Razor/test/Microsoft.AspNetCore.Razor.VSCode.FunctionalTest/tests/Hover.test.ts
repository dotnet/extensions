/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as assert from 'assert';
import { afterEach, before, beforeEach } from 'mocha';
import * as path from 'path';
import * as vscode from 'vscode';
import {
    mvcWithComponentsRoot,
    pollUntil,
    waitForProjectReady,
} from './TestUtil';

let cshtmlDoc: vscode.TextDocument;
let editor: vscode.TextEditor;

suite('Hover', () => {
    before(async () => {
        await waitForProjectReady(mvcWithComponentsRoot);
    });

    beforeEach(async () => {
        const filePath = path.join(mvcWithComponentsRoot, 'Views', 'Home', 'Index.cshtml');
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

    test('Can perform hovers on C#', async () => {
        const firstLine = new vscode.Position(0, 0);
        await editor.edit(edit => edit.insert(firstLine, '<p>@DateTime.Now</p>\n'));
        const hoverResult = await vscode.commands.executeCommand<vscode.Hover[]>(
            'vscode.executeHoverProvider',
            cshtmlDoc.uri,
            new vscode.Position(0, 6));
        const expectedRange = new vscode.Range(
            new vscode.Position(0, 4),
            new vscode.Position(0, 12));

        assert.ok(hoverResult, 'Should have a hover result for DateTime.Now');
        if (!hoverResult) {
            // Not possible, but strict TypeScript doesn't know about assert.ok above.
            return;
        }

        assert.equal(hoverResult.length, 1, 'Someone else unexpectedly may be providing hover results');
        assert.deepEqual(hoverResult[0].range, expectedRange, 'C# hover range should be DateTime.Now');
    });

    test('Can perform hovers on HTML', async () => {
        const firstLine = new vscode.Position(0, 0);
        await editor.edit(edit => edit.insert(firstLine, '<p>@DateTime.Now</p>\n'));
        const hoverResult = await vscode.commands.executeCommand<vscode.Hover[]>(
            'vscode.executeHoverProvider',
            cshtmlDoc.uri,
            new vscode.Position(0, 1));
        const expectedRange = new vscode.Range(
            new vscode.Position(0, 1),
            new vscode.Position(0, 2));

        assert.ok(hoverResult, 'Should have a hover result for <p>');
        if (!hoverResult) {
            // Not possible, but strict TypeScript doesn't know about assert.ok above.
            return;
        }

        assert.equal(hoverResult.length, 1, 'Someone else unexpectedly may be providing hover results');
        assert.deepEqual(hoverResult[0].range, expectedRange, 'HTML hover range should be p');
    });
});
