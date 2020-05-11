/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';

import { RazorLogger } from './RazorLogger';

export class BlazorDebugConfigurationProvider implements vscode.DebugConfigurationProvider {

    constructor(private readonly logger: RazorLogger, private readonly vscodeType: typeof vscode) {}

    public async resolveDebugConfiguration(folder: vscode.WorkspaceFolder | undefined, configuration: vscode.DebugConfiguration): Promise<vscode.DebugConfiguration | undefined> {
        const app = {
            name: '.NET Core Launch (Blazor Standalone)',
            type: 'coreclr',
            request: 'launch',
            program: 'dotnet',
            args: ['run'],
            cwd: '${workspaceFolder}',
            env: {
                ASPNETCORE_ENVIRONMENT: 'Development',
                ...configuration.env,
            },
        };
        const browser = {
            name: '.NET Core Debug Blazor Web Assembly in Browser',
            type: configuration.browser === 'edge' ? 'pwa-msedge' : 'pwa-chrome',
            request: 'launch',
            timeout: configuration.timeout,
            url: configuration.url,
            webRoot: configuration.webRoot,
            inspectUri: '{wsProtocol}://{url.hostname}:{url.port}/_framework/debug/ws-proxy?browser={browserInspectUri}',
            trace: configuration.trace || false,
        };

        try {
            await this.vscodeType.debug.startDebugging(folder, app);
            try {
                await this.vscodeType.debug.startDebugging(folder, browser);
                this.logger.logVerbose('[DEBUGGER] Launching JavaScript debugger...');
            } catch (error) {
                this.logger.logError(
                  '[DEBUGGER] Error when launching browser debugger: ',
                  error,
                );
            }
        } catch (error) {
            this.logger.logError(
              '[DEBUGGER] Error when launching application: ',
              error,
            );
        }

        return undefined;
    }
}
