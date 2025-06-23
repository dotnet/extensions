/* eslint-disable @typescript-eslint/no-explicit-any */

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { IVssRestClientOptions } from "./Context";
import { issueRequest, IVssRequestOptions } from "./Fetch";
import { deserializeVssJsonObject } from "./Util/Serialization";
import { replaceRouteValues, Uri } from "./Util/Url";

/**
* Parameters for sending a WebApi request
*/
export interface RestClientRequestParams {

    /**
    * Route template that is used to form the request path. If routeTemplate is NOT specified, then locationId 
    * is used to lookup the template via an OPTIONS request.
    */
    routeTemplate: string;

    /**
    * The api version string to send in the request (e.g. "1.0" or "2.0-preview.2")
    */
    apiVersion: string;

    /**
    * Dictionary of route template replacement values
    */
    routeValues?: { [key: string]: any; };

    /**
    * Data to post. In this case of a GET, this indicates query parameters.
    * For other requests, this is the request body object (which will be serialized
    * into a JSON string unless isRawData is set to true).
    */
    body?: any;

    /**
    * Query parameters to add to the url. In the case of a GET, query parameters can
    * be supplied via 'data' or 'queryParams'. For other verbs such as POST, the
    * data object specifies the POST body, so queryParams is needed to indicate
    * parameters to add to the query string of the url (not included in the post body).
    */
    queryParams?: { [key: string]: any; };

    /**
    * HTTP verb (GET by default if not specified)
    */
    method?: string;

    /**
    * The http response (Accept) type. This is "json" (corresponds to application/json Accept header) 
    * unless otherwise specified. Other possible values are "html", "text", "zip", or "binary" or their accept
    * header equivalents (e.g. application/zip).
    */
    httpResponseType?: string;

    /**
    * Allows the caller to specify custom request headers.
    */
    customHeaders?: { [headerName: string]: any; };

    /**
     * If true, indicates that the raw Response should be returned in the request's resulting promise
     * rather than deserializing the response (the default).
     */
    returnRawResponse?: boolean;

    /**
    * If true, this indicates that no processing should be done on the 'data' object
    * before it is sent in the request. *This is rarely needed*. One case is when posting
    * an HTML5 File object. 
    */
    isRawData?: boolean;

    /**
     * Current command for activity logging. This will override the RestClient's base option.
     */
    command?: string;
}

/**
* Base class that should be used (derived from) to make requests to VSS REST apis
*/
export class RestClientBase {

    private _options: IVssRestClientOptions;
    private _rootPath: Promise<string>;

    constructor(options: IVssRestClientOptions) {

        this._options = options || {};

        if (typeof this._options.rootPath === "string") {
            this._rootPath = Promise.resolve(this._options.rootPath);
        }
        else {
            this._rootPath = this._options.rootPath || Promise.resolve("/");
        }
    }

    /**
    * Gets the root path of the Service
    *
    * @returns Promise for the resolving the root path of the service.
    */
    protected async getRootPath(): Promise<string> {
        return this._rootPath;
    }

    /**
    * Issue a request to a VSS REST endpoint.
    *
    * @param requestParams request options
    * @returns Promise for the response
    */
    protected async beginRequest<T>(requestParams: RestClientRequestParams): Promise<T> {
        return this._rootPath.then((rootPath: string) => {

            let requestUrl = rootPath + replaceRouteValues(requestParams.routeTemplate, requestParams.routeValues || {});
            if (requestParams.queryParams) {
                const uri = new Uri(requestUrl);
                uri.addQueryParams(requestParams.queryParams);
                requestUrl = uri.absoluteUri;
            }
            
            return this._issueRequest<T>(requestUrl, requestParams.apiVersion, requestParams);
        });
    }

    /**
     * Issue a request to a VSS REST endpoint at the specified location
     * 
     * @param requestUrl Resolved URL of the request
     * @param apiVersion API version
     * @param requestParams Optional request parameters
     */
    protected _issueRequest<T>(requestUrl: string, apiVersion: string, requestParams: RestClientRequestParams): Promise<T> {

        const fetchOptions: RequestInit = {};

        fetchOptions.method = requestParams.method || "GET";
        fetchOptions.mode = "cors";

        if (!requestParams.isRawData && requestParams.body && fetchOptions.method.toUpperCase() !== 'GET') {
            fetchOptions.body = JSON.stringify(requestParams.body);
        }
        else {
            fetchOptions.body = requestParams.body;
        }

        const acceptType = requestParams.httpResponseType || "application/json";
        const acceptHeaderValue = `${acceptType};api-version=${apiVersion};excludeUrls=true;enumsAsNumbers=true;msDateFormat=true;noArrayWrap=true`;

        fetchOptions.headers = Object.assign({
            "Accept": acceptHeaderValue,
            "Content-Type": requestParams.body && "application/json"
        }, requestParams.customHeaders) as any /* lib.dom.d.ts does not have the correct type for Headers */;

        const vssRequestOptions = {
            authTokenProvider: this._options.authTokenProvider,
            sessionId: this._options.sessionId,
            command: requestParams.command || this._options.command
        } as IVssRequestOptions;

        const result = issueRequest(requestUrl, fetchOptions, vssRequestOptions);
        return result.then((response: Response) => {

            if (requestParams.returnRawResponse) {
                return response;
            }
            else if (acceptType.toLowerCase().indexOf("json") >= 0) {
                // MSJSON date formats must be replaced in the raw text before JSON parsing
                return response.text().then(deserializeVssJsonObject);
            }
            else if (acceptType.toLowerCase() === "text/plain") {
                return <any>response.text();
            }
            else {
                return response.arrayBuffer();
            }
        });
    }
}