/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { Trace } from 'vscode-jsonrpc';
import { RazorLanguage } from './RazorLanguage';
import { RazorLanguageServerOptions } from './RazorLanguageServerOptions';

export function resolveRazorLanguageServerOptions(): RazorLanguageServerOptions {
    let languageServerDllPath = RazorLanguage.serverConfig.get<string>("path");

    if (!languageServerDllPath) {
        throw new Error("Razor VSCode extension does not currently support dynamic resolution of language server.");
    }

    let debugLanguageServer = RazorLanguage.serverConfig.get<boolean>("debug");
    let outputChannel = vscode.window.createOutputChannel("Razor Language Server");
    let traceString = RazorLanguage.serverConfig.get<string>("trace");
    let trace: Trace;

    if (traceString === "Off") {
        trace = Trace.Off;
    }
    else if (traceString === "Messages") {
        trace = Trace.Messages;
    }
    else if (traceString === "Verbose") {
        trace = Trace.Verbose;
    }
    else {
        console.log("Invalid trace settign for Razor language server. Defaulting to 'Off'");
        trace = Trace.Off;
    }

    let options: RazorLanguageServerOptions = {
        serverDllPath: languageServerDllPath,
        debug: debugLanguageServer,
        outputChannel: outputChannel,
        trace: trace
    };

    return options;
}