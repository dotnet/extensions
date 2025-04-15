/* eslint-disable @typescript-eslint/no-explicit-any */

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Fetch polyfill for IE11
// import "whatwg-fetch";

import { IAuthorizationTokenProvider } from "./Context";

/**
* VSS-specific options for VSS requests
*/
export interface IVssRequestOptions {

    /*
    * Auth token manager that can be used to get and attach auth tokens to requests
    */
    authTokenProvider?: IAuthorizationTokenProvider;

    /**
     * Current session id.
     */
    sessionId?: string;

    /**
     * Current command for activity logging.
     */
    command?: string;
}

/**
 * An IPreRequestEvent is sent before a fetch request is made.
 */
export interface IPreRequestEvent {

    /**
     * A unique id that can be used to track this particular request (id is unique among all clients)
     */
    requestId: number;

    /**
     * Url of the request that is about to be issued
     */
    requestUrl: string;

    /**
     * Request settings being used
     */
    options?: RequestInit;

    /**
     * Additional VSS-specific options supplied in the request
     */
    vssRequestOptions?: IVssRequestOptions;
}

/**
 * An IPostRequestEvent is sent after a fetch request has completed.
 */
export interface IPostRequestEvent {

    /**
     * A unique id that can be used to track this particular request (id is unique among all clients)
     */
    requestId: number;

    /**
     * Url of the request that is about to be issued
     */
    requestUrl: string;

    /**
     * The Response returned for this request, if the request fails it will be undefined
     */
    response?: Response;

    /**
     * Additional VSS-specific options supplied in the request
     */
    vssRequestOptions?: IVssRequestOptions;
}

/**
 * When a fetch request fails, it will throw a VssServerError. Failure is defined
 * as a request that made it to the server, and the server successfully responded
 * with a failure. This will be any status return that is not a status code in
 * the success range (200-299).
 */
export interface VssServerError extends Error {

    /**
     * The status code returned from the server.
     */
    status: number;

    /**
     * The raw text that was returned from the server. If any is available.
     */
    responseText: string;

    /**
     * If the response text was sent and it was in the form of a JSON response
     * the object will be parsed and deserialized object is available here.
     *
     * This commonly has the exception details about the failure from the server.
     * Things like the message, exception type, and stack trace will be available.
     */
    serverError?: any;
}

/**
 * Issue a fetch request. This is a wrapper around the browser fetch method that handles VSS authentication
 * and triggers events that can be listened to by other modules.
 *
 * @param requestUrl Url to send the request to
 * @param options fetch settings/options for the request
 * @param vssRequestOptions VSS specific request options
 *
 * Triggered Events:
 *  afterRequest -> IPostRequestEvent is sent after the request has completed.
 *  beforeRequest -> IPreRequestEvent is sent before the request is made.
 */
export async function issueRequest(requestUrl: string, options?: RequestInit, vssRequestOptions?: IVssRequestOptions): Promise<Response> {
    let response: Response | undefined = undefined;

    // Add a X-VSS-ReauthenticationAction header to all fetch requests
    if (!options) {
        options = {};
    }

    let headers: Headers;
    
    // If options.headers is set, check if it is a Headers object, and if not, convert it to one.
    if (options.headers) {
        if (options.headers instanceof Headers) {
            headers = options.headers;
        }
        else {
            headers = new Headers();
            if (typeof options.headers.hasOwnProperty === "function") {
                for (const key in options.headers) {
                    if (Object.prototype.hasOwnProperty.call(options.headers, key)) {
                        headers.append(key, (options.headers as { [key: string]: string })[key]);
                    }
                }
            }
        }
    }
    else {
        headers = new Headers();
    }
    options.headers = headers;

    headers.append("X-VSS-ReauthenticationAction", "Suppress");

    // Add a X-TFS-Session header with the current sessionId and command for correlation
    if (vssRequestOptions) {
        const sessionId = vssRequestOptions.sessionId;
        const command = vssRequestOptions.command;

        if (sessionId) {
            if (command) {
                headers.append("X-TFS-Session", sessionId + "," + command);
            }
            else {
                headers.append("X-TFS-Session", sessionId);
            }
        }
    }

    // Send credentials to the same origin, we will use tokens for differing origins.
    options.credentials = "same-origin";

    let refreshToken = false;

    do {
        
        // Ensure we have an authorization token available from the auth manager.
        if (vssRequestOptions && vssRequestOptions.authTokenProvider) {
            const authHeader = await vssRequestOptions.authTokenProvider.getAuthorizationHeader(refreshToken);
            if (authHeader) {
                headers.append("Authorization", authHeader);
                refreshToken = true;
            }
            headers.append("X-TFS-FedAuthRedirect", "Suppress");
        }

        // Execute the http request defined by the caller.
        try {
            response = await fetch(requestUrl, options);
        }
        catch (ex) {
            console.warn(`Unhandled exception in fetch for ${requestUrl}: ${ex && ex.toString()}`);
            const error = new Error("TF400893: Unable to contact the server. Please check your network connection and try again.");
            error.name = "NetworkException";
            throw error;
        }

        // If we recieved a 401 and have a token manager, we will refresh our token and try again.
        if (response.status === 401 && !refreshToken && vssRequestOptions && vssRequestOptions.authTokenProvider) {
            refreshToken = true;
            continue;
        }

    // eslint-disable-next-line no-constant-condition
    } while (false);

    // Parse error details from requests that returned non 200-299 status codes.
    if (!response.ok) {

        // Create a VssServerError and throw it as the result of the fetch request.
        const error = new Error() as VssServerError;
        error.name = "TFS.WebApi.Exception";
        error.status = response.status;
        error.responseText = await response.text();

        // Attempt to parse the response as a json object, for many of the failures
        // the server will serialize details into the body.
        if (error.responseText && error.responseText !== "") {
            try {
                error.serverError = JSON.parse(error.responseText);
                if (error.serverError && error.serverError.message) {
                    error.message = error.serverError.message;
                }
            }
            catch { /* empty */ }
        }

        if (!error.message) {
            error.message = response.status + ": " + response.statusText;
        }

        throw error;
    }

    return response;
}