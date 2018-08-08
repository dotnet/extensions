/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { Trace } from 'vscode-jsonrpc';
import { RazorLanguage } from './RazorLanguage';
import { RazorLanguageServerOptions } from './RazorLanguageServerOptions';

export function resolveRazorLanguageServerOptions() {
    const languageServerDllPath = RazorLanguage.serverConfig.get<string>('path');

    if (!languageServerDllPath) {
        throw new Error('Razor VSCode extension does not currently support dynamic resolution of language server.');
    }

    const debugLanguageServer = RazorLanguage.serverConfig.get<boolean>('debug');
    const outputChannel = vscode.window.createOutputChannel('Razor Language Server');
    const traceString = RazorLanguage.serverConfig.get<string>('trace');

    return {
        serverDllPath: languageServerDllPath,
        debug: debugLanguageServer,
        outputChannel,
        trace: parseTraceString(traceString),
    } as RazorLanguageServerOptions;
}

function parseTraceString(traceString: string | undefined) {
    switch (traceString) {
        case 'Off':
            return Trace.Off;
        case 'Messages':
            return Trace.Messages;
        case 'Verbose':
            return Trace.Verbose;
        default:
            console.log('Invalid trace setting for Razor language server. Defaulting to \'Off\'');
            return Trace.Off;
    }
}
