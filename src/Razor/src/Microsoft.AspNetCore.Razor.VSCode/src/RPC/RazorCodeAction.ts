/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { RazorCodeActionResolutionParams } from './RazorCodeActionResolutionParams';
import { SerializableWorkspaceEdit } from './SerializableWorkspaceEdit';

export interface RazorCodeAction {
    title: string;
    edit: SerializableWorkspaceEdit;
    data: RazorCodeActionResolutionParams;
}
