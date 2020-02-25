/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as assert from 'assert';
import { beforeEach } from 'mocha';
import * as path from 'path';
import * as vscode from 'vscode';
import { mvcWithComponentsRoot } from './TestUtil';

let cshtmlDoc: vscode.TextDocument;
let editor: vscode.TextEditor;
let cshtmlPath: string;

suite('Implementation', () => {
    beforeEach(async () => {
        cshtmlPath = path.join(mvcWithComponentsRoot, 'Views', 'Home', 'Index.cshtml');
        cshtmlDoc = await vscode.workspace.openTextDocument(cshtmlPath);
        editor = await vscode.window.showTextDocument(cshtmlDoc);
    });

    test('Implementation inside file works', async () => {
        const firstLine = new vscode.Position(0, 0);

        await editor.edit(edit => edit.insert(firstLine,
`@functions{
    public abstract class Cheese {}
    public class Cheddar : Cheese {}
}`));

        const implementations = await vscode.commands.executeCommand<vscode.Location[]>(
            'vscode.executeImplementationProvider',
            cshtmlDoc.uri,
            new vscode.Position(1, 30));

        assert.equal(implementations!.length, 1, 'Should have had exactly one result');
        const implementation = implementations![0];
        assert.ok(implementation.uri.path.endsWith('Index.cshtml'), `Expected to find 'Index.cshtml' but found '${implementation.uri.path}'`);
        assert.equal(implementation.range.start.line, 2);
    });

    test('Implementation outside file works', async () => {
        const firstLine = new vscode.Position(0, 0);
        await editor.edit(edit => edit.insert(firstLine, `@{
    var x = typeof(Cheese);
}`));

        const programPath = path.join(mvcWithComponentsRoot, 'Program.cs');
        const programDoc = await vscode.workspace.openTextDocument(programPath);
        const programEditor = await vscode.window.showTextDocument(programDoc);
        await programEditor.edit(edit => edit.insert(new vscode.Position(3, 0), `    public abstract class Cheese {}
    public class Cheddar : Cheese {}
`));

        const position = new vscode.Position(1, 23);
        const implementations = await vscode.commands.executeCommand<vscode.Location[]>(
            'vscode.executeImplementationProvider',
            cshtmlDoc.uri,
            position);

        await vscode.commands.executeCommand('workbench.action.revertAndCloseActiveEditor');

        assert.equal(implementations!.length, 1, 'Should have had exactly one result');
        const implementation = implementations![0];
        assert.ok(implementation.uri.path.endsWith('Program.cs'), `Expected def to point to "Program.cs", but it pointed to ${implementation.uri.path}`);
        assert.equal(implementation.range.start.line, 4);
    });
});
