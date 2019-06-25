/* --------------------------------------------------------------------------------------------
* Copyright (c) Microsoft Corporation. All rights reserved.
* Licensed under the MIT License. See License.txt in the project root for license information.
* ------------------------------------------------------------------------------------------ */

// Everything here is pascal cased in order to correspond with the language server invocation we're performing.
export interface UpdateProjectRequest {
    readonly ProjectSnapshotHandle: SerializedProjectSnapshotHandle;
}

interface SerializedProjectSnapshotHandle {
    readonly FilePath: string;
    readonly Configuration?: any;
    readonly RootNamespace?: string;
    readonly ProjectWorkspaceState?: any;
    readonly Documents?: any;
    readonly SerializationFormat: string | null;
}
