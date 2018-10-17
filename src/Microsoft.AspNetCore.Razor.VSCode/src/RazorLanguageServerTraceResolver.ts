/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { Trace } from 'vscode-jsonrpc';
import { RazorLanguage } from './RazorLanguage';

export function resolveRazorLanguageServerTrace() {
    const traceString = RazorLanguage.languageConfig.get<string>('trace');
    const trace = parseTraceString(traceString);

    return trace;
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
