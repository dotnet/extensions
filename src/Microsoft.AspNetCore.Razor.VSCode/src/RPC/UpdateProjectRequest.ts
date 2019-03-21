/* --------------------------------------------------------------------------------------------
* Copyright (c) Microsoft Corporation. All rights reserved.
* Licensed under the MIT License. See License.txt in the project root for license information.
* ------------------------------------------------------------------------------------------ */

export interface UpdateProjectRequest {
    readonly filePath: string;
    readonly configuration?: any;
    readonly rootNamespace?: string;
    readonly projectWorkspaceState?: any;
}
