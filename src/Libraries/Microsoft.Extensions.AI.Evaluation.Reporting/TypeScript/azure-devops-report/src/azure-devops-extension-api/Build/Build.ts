/* eslint-disable @typescript-eslint/no-explicit-any */
/*
 * ---------------------------------------------------------
 * Copyright(C) Microsoft Corporation. All rights reserved.
 * ---------------------------------------------------------
 */

import * as TfsCore from "../Core/Core";

/**
 * Represents an attachment to a build.
 */
export interface Attachment {
    _links: any;
    /**
     * The name of the attachment.
     */
    name: string;
}

/**
 * Data representation of a build.
 */
export interface Build {
    /**
     * The ID of the build.
     */
    id: number;

    project: TfsCore.TeamProjectReference;
}

