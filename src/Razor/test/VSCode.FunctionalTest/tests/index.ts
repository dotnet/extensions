/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as fs from 'fs';
import * as Mocha from 'mocha';
import * as path from 'path';
import * as vscode from 'vscode';

// This file controls which tests are run during the functional test process.
export async function run(): Promise<void> {
    const razorConfiguration = vscode.workspace.getConfiguration('razor');
    const devmode = razorConfiguration.get('devmode');
    if (!devmode) {
        await vscode.commands.executeCommand('extension.configureRazorDevMode');
    }

    // This seems to magically help with activating the extension.
    ensureRequiredExtension();

    let testFilter = process.env.testFilter;
    if (process.env.runSingleTest === 'true') {
        testFilter = await vscode.window.showInputBox({
            prompt: 'Test file filter',
            placeHolder: '**.test.js',
        });
    }

    if (!testFilter) {
        testFilter = '**.test.js';
    } else if (!testFilter.endsWith('.test.js')) {
        testFilter += '**.test.js';
    }

    process.env.testFilter = testFilter;

    // Configure Mocha
    const mocha = new Mocha({
        ui: 'tdd',
        timeout: 60000,
    });
    mocha.useColors(true);

    const testsRoot = path.resolve(__dirname, '..');
    const testArtifacts = path.join(testsRoot, '..', '..', '..', '..', 'artifacts', 'TestResults');
    ensureDirectory(testArtifacts);
    const config = process.env.config ? process.env.config : 'Debug';
    const testResults = path.join(testArtifacts, config);
    ensureDirectory(testResults);
    const resolvedTestResults = path.resolve(testResults);
    ensureDirectory(resolvedTestResults);
    const file = path.join(resolvedTestResults, 'VSCode-FunctionalTests.xml');
    mocha.reporter('xunit', { output: file });

    return new Promise((c, e) => {
        mocha.addFile(path.resolve(testsRoot, __dirname, 'Main.test.js'));

        try {
            // Run the mocha test
            mocha.run(failures => {
                if (failures > 0) {
                    e(new Error(`${failures} tests failed.`));
                } else {
                    c();
                }
            })
            .on('test', (test) => {
                console.log(`👉 Test started: ${test.parent!.title} - ${test.title}`);
            })
            .on('pending', (test) => {
                console.log(`⚠️ Test skipped: ${test.parent!.title} - ${test.title}`);
            })
            .on('pass', (test) => {
                console.log(`✅ Test passed: ${test.parent!.title} - ${test.title} (${test.duration}ms)`);
            })
            .on('fail', (test, err) => {
                console.log(`❌ Test failed: ${test.parent!.title} - ${test.title}`);
                console.log(err);
            });
        } catch (err) {
            e(err);
        }
    });

    function ensureDirectory(directory: string) {
        if (!fs.existsSync(directory)) {
            fs.mkdirSync(directory);
        }
    }

    function ensureRequiredExtension() {
        const csharpExtension = vscode.extensions.getExtension('ms-dotnettools.csharp');
        const razorExtension = vscode.extensions.getExtension('ms-dotnettools.razor-vscode');

        if (csharpExtension && razorExtension) {
            // Razor + C# installed
            return;
        }

        throw Error('CSharp extension not installed.');
    }
}
