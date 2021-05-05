/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

import { SemanticTokensResponse } from './SemanticTokensResponse';

export class ProvideSemanticTokensResponse {
    // tslint:disable-next-line: variable-name
    constructor(public Result: SemanticTokensResponse, public HostDocumentSyncVersion: number | null) {}
}
