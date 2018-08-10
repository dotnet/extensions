/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as testRunner from 'vscode/lib/testrunner';

// See https://github.com/mochajs/mocha/wiki/Using-mocha-programmatically#set-options for more info

testRunner.configure({
    timeout: 15000,     // It takes a while to load the extension for the first test
    ui: 'bdd',          // the TDD UI is being used in extension.test.ts (suite, test, etc.)
    useColors: true,    // colored output from test results
});

declare const module: any;
module.exports = testRunner;
