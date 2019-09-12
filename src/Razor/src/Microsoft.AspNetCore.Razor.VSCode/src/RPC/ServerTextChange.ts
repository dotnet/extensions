/* --------------------------------------------------------------------------------------------
* Copyright (c) Microsoft Corporation. All rights reserved.
* Licensed under the MIT License. See License.txt in the project root for license information.
* ------------------------------------------------------------------------------------------ */

import { ServerTextSpan } from './ServerTextSpan';

export interface ServerTextChange {
    readonly newText: string;
    readonly span: ServerTextSpan;
}
