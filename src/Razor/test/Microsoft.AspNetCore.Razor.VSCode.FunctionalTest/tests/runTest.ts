// /* --------------------------------------------------------------------------------------------
//  * Copyright (c) Microsoft Corporation. All rights reserved.
//  * Licensed under the MIT License. See License.txt in the project root for license information.
//  * ------------------------------------------------------------------------------------------ */

// This file is used at the command line to download VSCode insiders and run all of our functional tests.

import * as path from 'path';
import { runTests } from 'vscode-test';

async function main() {
    try {
        const extensionDevelopmentPath = path.resolve(__dirname, '../../../src/Microsoft.AspNetCore.Razor.VSCode.Extension/');
        const extensionTestsPath = path.resolve(__dirname, './index');
        const testAppFolder = path.resolve(__dirname, '../../testapps');

        // Download VS Code, unzip it and run the integration test
        await runTests({
            extensionDevelopmentPath,
            extensionTestsPath,
            version: 'insiders',
            launchArgs: [ testAppFolder ],
        });
    } catch (err) {
        console.error('Failed to run functional tests');
        process.exit(1);
    }
}

main();
