/* ---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 * --------------------------------------------------------------------------------------------- */

import { RequestType } from 'vscode-languageclient';
import { RazorLanguageServerClient } from './RazorLanguageServerClient';

export class RazorServerReadyHandler {
    private static readonly razorServerReadyEndpoint = 'razor/serverReady';
    private razorServerReadyHandlerType: RequestType<void, void, any, any> = new RequestType(RazorServerReadyHandler.razorServerReadyEndpoint);

    constructor(private readonly serverClient: RazorLanguageServerClient) {
    }

    public register() {
        // tslint:disable-next-line: no-floating-promises
        this.serverClient.onRequestWithParams<void, void,  any, any>(
            this.razorServerReadyHandlerType,
            async (request, token) => this.handleRazorServerReady());
    }

    private handleRazorServerReady(): void {
        return;
    }
}
