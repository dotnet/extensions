/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';

import { RazorLogger } from './RazorLogger';

export class BlazorDebugConfigurationProvider implements vscode.DebugConfigurationProvider {
    private logger: RazorLogger;
    private vscodeType: typeof vscode;

    constructor(logger: RazorLogger, vscodeType: typeof vscode) {
        this.logger = logger;
        this.vscodeType = vscodeType;
    }

    public resolveDebugConfiguration(folder: vscode.WorkspaceFolder | undefined, configuration: vscode.DebugConfiguration): vscode.ProviderResult<vscode.DebugConfiguration> {
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
            type: configuration.browser === 'edge' ? 'edge' : 'pwa-chrome',
            request: 'launch',
            timeout: 30000,
            url: configuration.url || 'https://localhost:5001',
            webRoot: '${workspaceFolder}',
            inspectUri: '{wsProtocol}://{url.hostname}:{url.port}/_framework/debug/ws-proxy?browser={browserInspectUri}',
        };

        this.vscodeType.debug.startDebugging(folder, app).then(
            appStartFulfilled => {
                if (appStartFulfilled) {
                    this.vscodeType.debug.startDebugging(folder, browser).then(
                        debugStartFulfilled => {
                            if (debugStartFulfilled) {
                                this.logger.logVerbose('Launching JavaScript debugger...');
                            }
                        },
                        error => {
                            this.logger.logError('Error when launching Chrome debugger: ', error);
                        },
                    );
                }
            },
            error => {
                this.logger.logError('Error when launching application: ', error);
            },
        );

        return undefined;
    }
}
