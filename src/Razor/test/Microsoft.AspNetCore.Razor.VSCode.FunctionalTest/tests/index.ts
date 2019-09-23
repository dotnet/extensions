/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as glob from 'glob';
import * as Mocha from 'mocha';
import * as path from 'path';
import * as vscode from 'vscode';

// This file controls which tests are run during the functional test process.

export async function run(): Promise<void> {
    const mocha = new Mocha({
        ui: 'tdd',
        timeout: 60000,
    });
    mocha.useColors(true);

    const testsRoot = path.resolve(__dirname, '..');

    const razorConfiguration = vscode.workspace.getConfiguration('razor');
    const devmode = razorConfiguration.get('devmode');

    if (!devmode) {
        await vscode.commands.executeCommand('extension.configureRazorDevMode');
    }

    return new Promise((c, e) => {
        glob('**/**.test.js', { cwd: testsRoot }, (err, files) => {
            if (err) {
                return e(err);
            }

            // Add files to the test suite
            files.forEach(f => mocha.addFile(path.resolve(testsRoot, f)));

            try {
                // Run the mocha test
                mocha.run(failures => {
                    if (failures > 0) {
                        e(new Error(`${failures} tests failed.`));
                    } else {
                        c();
                    }
                });
            } catch (err) {
                e(err);
            }
        });
    });
}
