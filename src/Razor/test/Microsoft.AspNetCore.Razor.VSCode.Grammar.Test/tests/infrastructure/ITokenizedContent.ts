/* --------------------------------------------------------------------------------------------
* Copyright (c) Microsoft Corporation. All rights reserved.
* Licensed under the MIT License. See License.txt in the project root for license information.
* ------------------------------------------------------------------------------------------ */

import { ITokenizeLineResult } from 'vscode-textmate';

export interface ITokenizedContent {
    readonly source: string;
    readonly lines: string[];
    readonly tokenizedLines: ITokenizeLineResult[];
}
