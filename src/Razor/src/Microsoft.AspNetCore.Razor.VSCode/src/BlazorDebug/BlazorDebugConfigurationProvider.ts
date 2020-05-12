/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { spawn } from 'child_process';
import * as vscode from 'vscode';

import { RazorLogger } from '../RazorLogger';

export class BlazorDebugConfigurationProvider implements vscode.DebugConfigurationProvider {

    constructor(private readonly logger: RazorLogger, private readonly vscodeType: typeof vscode) { }

    public async resolveDebugConfiguration(folder: vscode.WorkspaceFolder | undefined, configuration: vscode.DebugConfiguration): Promise<vscode.DebugConfiguration | undefined> {
        const output = this.vscodeType.window.createOutputChannel('Blazor WebAssembly App');
        const app = spawn('dotnet', ['run'], {
            cwd: configuration.cwd || folder && folder.uri && folder.uri.fsPath,
            env: {
                ...process.env,
                ASPNETCORE_ENVIRONMENT: 'Development',
                ...configuration.env,
            },
        });

        app.stdout.on('data', (data) => output.append(data.toString()));
        app.stderr.on('data', (data) => output.append(data.toString()));
        app.on('error', (error) => {
            output.append(error.toString());
            this.logger.logError('[DEBUGGER] Error when launch app: ', error);
        });

        output.show();

        const browser = {
            name: '.NET Core Debug Blazor Web Assembly in Browser',
            type: configuration.browser === 'edge' ? 'pwa-msedge' : 'pwa-chrome',
            request: 'launch',
            timeout: configuration.timeout || 30000,
            url: configuration.url || 'https://localhost:5001',
            webRoot: configuration.webRoot || '${workspaceFolder}',
            inspectUri: '{wsProtocol}://{url.hostname}:{url.port}/_framework/debug/ws-proxy?browser={browserInspectUri}',
            trace: configuration.trace || false,
        };

        let showErrorInfo = false;

        try {
            await this.vscodeType.debug.startDebugging(folder, browser);
            this.logger.logVerbose('[DEBUGGER] Launching browser debugger...');
        } catch (error) {
            this.logger.logError(
                '[DEBUGGER] Error when launching browser debugger: ',
                error,
            );
            showErrorInfo = true;
        }

        if (showErrorInfo) {
            const message = `There was an unexpected error while launching your debugging session. Check the console for helpful logs and visit https://aka.ms/blazorwasmcodedebug for more info.`;
            this.vscodeType.window.showErrorMessage(message);
        }

        return undefined;
    }
}
