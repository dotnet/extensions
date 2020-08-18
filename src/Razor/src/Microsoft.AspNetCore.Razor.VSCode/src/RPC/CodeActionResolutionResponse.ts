/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { SerializableWorkspaceEdit } from './SerializableWorkspaceEdit';

export interface CodeActionResolutionResponse {
    edit: SerializableWorkspaceEdit;
}
