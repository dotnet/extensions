/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as assert from 'assert';
import { afterEach, before, beforeEach } from 'mocha';
import * as path from 'path';
import * as vscode from 'vscode';
import {
    componentRoot,
    pollUntil,
    waitForProjectReady,
} from './TestUtil';

let cshtmlDoc: vscode.TextDocument;
let editor: vscode.TextEditor;

suite('Hover Components', () => {
    before(async () => {
        await waitForProjectReady(componentRoot);
    });

    beforeEach(async () => {
        const filePath = path.join(componentRoot, 'Components', 'Shared', 'MainLayout.razor');
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

    test('Can perform hovers on Components', async () => {
        const firstLine = new vscode.Position(0, 0);
        await editor.edit(edit => edit.insert(firstLine, '<NavMenu />\n'));
        const hoverResult = await vscode.commands.executeCommand<vscode.Hover[]>(
            'vscode.executeHoverProvider',
            cshtmlDoc.uri,
            new vscode.Position(0, 3));
        const expectedRange = new vscode.Range(
            new vscode.Position(0, 1),
            new vscode.Position(0, 8));

        assert.ok(hoverResult, 'Should have a hover result for NavMenu');
        if (!hoverResult) {
            // Not possible, but strict TypeScript doesn't know about assert.ok above.
            return;
        }

        assert.equal(hoverResult.length, 1, 'Something else may be providing hover results');

        const navMenuResult = hoverResult[0];
        assert.deepEqual(navMenuResult.range, expectedRange, 'Component range should be <NavMenu>');
        const mStr = navMenuResult.contents[0] as vscode.MarkdownString;
        assert.ok(mStr.value.includes('**NavMenu**'), `**NavMenu** not included in '${mStr.value}'`);
    });
});
