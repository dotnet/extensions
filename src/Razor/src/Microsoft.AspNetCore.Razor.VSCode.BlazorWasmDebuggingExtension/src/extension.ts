/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as cp from 'child_process';
import * as vscode from 'vscode';
import { acquireDotnetInstall } from './acquireDotnetInstall';
import { getAvailablePort } from './getAvailablePort';

export function activate(context: vscode.ExtensionContext) {
    const outputChannel = vscode.window.createOutputChannel('Blazor WASM Debug Proxy');
    const pidsByUrl = new Map<string, number>();

    const launchDebugProxy = vscode.commands.registerCommand('blazorwasm-companion.launchDebugProxy', async () => {
        try {
            const debuggingPort = await getAvailablePort(9222);
            const debuggingHost = `http://localhost:${debuggingPort}`;

            const debugProxyLocalPath = `${context.extensionPath}/BlazorDebugProxy/BrowserDebugHost.dll`;
            const spawnedProxyArgs = [debugProxyLocalPath , '--DevToolsUrl', debuggingHost];

            const dotnet = await acquireDotnetInstall(outputChannel);

            outputChannel.appendLine(`Launching debugging proxy from ${debugProxyLocalPath}`);
            const spawnedProxy = cp.spawn(dotnet, spawnedProxyArgs);

            let chunksProcessed = 0;
            for await (const output of spawnedProxy.stdout) {
                // If we haven't found the URL in the first ten chunks processed
                // then bail out.
                if (chunksProcessed++ > 10) {
                    return;
                }
                outputChannel.appendLine(output);
                // The debug proxy server outputs the port it is listening on in the
                // standard output of the launched application. We need to pass this URL
                // back to the debugger so we extract the URL from stdout using a regex.
                // The debug proxy will not exit until killed via the `killDebugProxy`
                // method so parsing stdout is necessary to extract the URL.
                const matchExpr = 'Now listening on: (?<url>.*)';
                const found = `${output}`.match(matchExpr);
                const url = found?.groups?.url;
                if (url) {
                    outputChannel.appendLine(`Debugging proxy is running at: ${url}`);
                    pidsByUrl.set(url, spawnedProxy.pid);
                    return {
                        url,
                        inspectUri: `${url}{browserInspectUriPath}`,
                        debuggingPort,
                    };
                }
            }

            for await (const error of spawnedProxy.stderr) {
                outputChannel.appendLine(`ERROR: ${error}`);
                return {
                    inspectUri: '{wsProtocol}://{url.hostname}:{url.port}/_framework/debug/ws-proxy?browser={browserInspectUri}',
                };
            }

            return;
        } catch (error) {
            outputChannel.appendLine(`ERROR: ${error}`);
            return {
                inspectUri: '{wsProtocol}://{url.hostname}:{url.port}/_framework/debug/ws-proxy?browser={browserInspectUri}',
            };
        }
    });

    const killDebugProxy = vscode.commands.registerCommand('blazorwasm-companion.killDebugProxy', (url: string) => {
        const pid = pidsByUrl.get(url);

        if (!pid) {
            outputChannel.appendLine(`Unable to find PID for server running at ${url}...`);
            return;
        }

        outputChannel.appendLine(`Terminating debug proxy server running at ${url} with PID ${pid}`);
        process.kill(pid);

    });

    context.subscriptions.push(launchDebugProxy, killDebugProxy);
}
