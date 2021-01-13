/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as glob from 'glob';
import { afterEach, before } from 'mocha';
import * as path from 'path';
import * as vscode from 'vscode';
import {
    componentRoot,
    mvcWithComponentsRoot,
    pollUntil,
    simpleMvc11Root,
    simpleMvc21Root,
    simpleMvc22Root,
    waitForProjectsReady,
} from './TestUtil';

suite('Main', () => {
    before(async function(this) {
        this.timeout(300000);

        const projectList = [
            componentRoot,
            simpleMvc11Root,
            simpleMvc21Root,
            simpleMvc22Root,
            mvcWithComponentsRoot,
        ];
        await waitForProjectsReady(...projectList);
    });

    afterEach(async () => {
        await vscode.commands.executeCommand('workbench.action.revertAndCloseActiveEditor');
        await pollUntil(async () => {
            await vscode.commands.executeCommand('workbench.action.closeAllEditors');
            if (vscode.window.visibleTextEditors.length === 0) {
                return true;
            }

            return false;
        }, /* timeout */ 3000, /* pollInterval */ 500, /* suppressError */ true);
    });

    const testFilter = process.env.testFilter;
    const files = glob.sync(`**/${testFilter}`, { cwd: __dirname }).filter(f => !f.endsWith(__filename));
    console.log(`${files.length} test file(s) matched this pattern.`);

    for (const file of files) {
        require(path.resolve(__dirname, file));
    }
});
