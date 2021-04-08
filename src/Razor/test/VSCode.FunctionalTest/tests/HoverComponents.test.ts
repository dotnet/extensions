/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as assert from 'assert';
import { beforeEach } from 'mocha';
import * as path from 'path';
import { error } from 'util';
import * as vscode from 'vscode';
import { componentRoot } from './TestUtil';

let cshtmlDoc: vscode.TextDocument;
let editor: vscode.TextEditor;

function RangeToStr(range: vscode.Range | undefined): string {
    if (range) {
        return `${range.start.line}:${range.start.character} to ${range.end.line}:${range.end.character}`;
    } else {
        return 'undefined';
    }
}

suite('Hover Components', () => {
    beforeEach(async () => {
        const filePath = path.join(componentRoot, 'Components', 'Shared', 'MainLayout.razor');
        cshtmlDoc = await vscode.workspace.openTextDocument(filePath);
        editor = await vscode.window.showTextDocument(cshtmlDoc);
    });

    test('Can perform hovers on directive attributes', async () => {
        const firstLine = new vscode.Position(1, 0);
        const counterPath = path.join(componentRoot, 'Components', 'Pages', 'Counter.razor');
        const counterDoc = await vscode.workspace.openTextDocument(counterPath);
        const counterEditor = await vscode.window.showTextDocument(counterDoc);
        await counterEditor.edit(edit => edit.insert(firstLine, '<button class="btn btn-primary" @onclick="@IncrementCount">Click me</button>'));

        const hoverResult = await vscode.commands.executeCommand<vscode.Hover[]>(
            'vscode.executeHoverProvider',
            counterDoc.uri,
            new vscode.Position(1, 36));

        assert.ok(hoverResult, 'Should have a hover result for @onclick');
        if (!hoverResult) {
            // Not possible, but strict TypeScript doesn't know about assert.ok above.
            return;
        }

        assert.ok(hoverResult.length > 0, 'Should have atleast one result.');

        const onClickResult = hoverResult.find(hover => (hover.contents[0] as vscode.MarkdownString).value.includes('EventCallback'));
        if (!onClickResult) {
            assert.fail('No eventhandler result was found');
            throw error();
        }
        const expectedRange = new vscode.Range(
            new vscode.Position(1, 32),
            new vscode.Position(1, 40));
        const rangeContent = counterDoc.getText(onClickResult.range);

        assert.equal(rangeContent, '@onclick');
        assert.deepEqual(onClickResult.range, expectedRange, `Directive range should be @onclick: ${RangeToStr(expectedRange)} but was ${rangeContent}: ${RangeToStr(onClickResult.range)}`);
        const mStr = onClickResult.contents[0] as vscode.MarkdownString;
        assert.ok(mStr.value.includes('EventHandlers.**onclick**'), `**onClick** not included in '${mStr.value}'`);
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
