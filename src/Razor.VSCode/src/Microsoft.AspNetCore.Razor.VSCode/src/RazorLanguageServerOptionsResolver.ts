/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { RazorLanguage } from './RazorLanguage';
import { RazorLanguageServerOptions } from './RazorLanguageServerOptions';
import { RazorLogger } from './RazorLogger';
import { Trace } from './Trace';
import * as vscode from './vscodeAdapter';

export function resolveRazorLanguageServerOptions(
    vscodeApi: vscode.api,
    languageServerDir: string,
    trace: Trace,
    logger: RazorLogger) {
    const languageServerExecutablePath = findLanguageServerExecutable(languageServerDir);
    const serverConfig = vscodeApi.workspace.getConfiguration('razor.languageServer');
    const debugLanguageServer = serverConfig.get<boolean>('debug');

    return {
        serverPath: languageServerExecutablePath,
        debug: debugLanguageServer,
        trace,
        outputChannel: logger.outputChannel,
    } as RazorLanguageServerOptions;
}

function findLanguageServerExecutable(withinDir: string) {
    const extension = isWindows() ? '.exe' : '';
    const executablePath = path.join(
        withinDir,
        `rzls${extension}`);
    let fullPath = '';

    if (fs.existsSync(executablePath)) {
        fullPath = executablePath;
    } else {
        // Exe doesn't exist.
        const dllPath = path.join(
            withinDir,
            'rzls.dll');

        if (!fs.existsSync(dllPath)) {
            throw new Error(`Could not find Razor Language Server executable within directory '${withinDir}'`);
        }

        fullPath = dllPath;
    }

    return fullPath;
}

function isWindows() {
    return !!os.platform().match(/^win/);
}
