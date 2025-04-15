/*
 * ---------------------------------------------------------
 * Copyright(C) Microsoft Corporation. All rights reserved.
 * ---------------------------------------------------------
 */

/**
 * Interface for a class that can retrieve authorization tokens to be used in fetch requests.
 */
export interface IAuthorizationTokenProvider {
    /**
     * Gets the authorization header to use in a fetch request
     *
     * @param forceRefresh - If true, indicates that we should get a new token, if applicable for current provider.
     * @returns the value to use for the Authorization header in a request.
     */
    getAuthorizationHeader(forceRefresh?: boolean): Promise<string>;
}

/**
 * Options for a specific instance of a REST client.
 */
export interface IVssRestClientOptions {

    /**
    * Auth token manager that can be used to get and attach auth tokens to requests.
    * If not supplied, the default token provider is used if the serviceInstanceType option is supplied
    * and is different from the hosting page's service instance type.
    */
    authTokenProvider?: IAuthorizationTokenProvider;

    /**
     * The root URL path to use for this client. Can be relative or absolute.
     */
    rootPath?: string | Promise<string>;

    /**
     * Current session id.
     */
    sessionId?: string;

    /**
     * Current command for activity logging.
     */
    command?: string;
}