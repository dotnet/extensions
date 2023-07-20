// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Gen.AutoClient.Model;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.AutoClient;

// Stryker disable all

internal sealed class Emitter : EmitterBase
{
    private const string IServiceCollection = "global::Microsoft.Extensions.DependencyInjection.IServiceCollection";
    private const string Task = "global::System.Threading.Tasks.Task";
    private const string HttpMethod = "global::System.Net.Http.HttpMethod";
    private const string HttpRequestMessage = "global::System.Net.Http.HttpRequestMessage";
    private const string HttpResponseMessage = "global::System.Net.Http.HttpResponseMessage";
    private const string Uri = "global::System.Uri";
    private const string StringContent = "global::System.Net.Http.StringContent";
    private const string Encoding = "global::System.Text.Encoding";
    private const string RequestMetadata = "global::Microsoft.Extensions.Http.Telemetry.RequestMetadata";
    private const string TelemetryExtensions = "global::Microsoft.Extensions.Telemetry.TelemetryExtensions";
    private const string RestApiException = "global::Microsoft.Extensions.Http.AutoClient.AutoClientException";
    private const string HttpClient = "global::System.Net.Http.HttpClient";
    private const string IHttpClientFactory = "global::System.Net.Http.IHttpClientFactory";
    private const string CancellationToken = "global::System.Threading.CancellationToken";
    private const string AutoClientOptions = "global::Microsoft.Extensions.Http.AutoClient.AutoClientOptions";
    private const string AutoClientOptionsValidator = "global::Microsoft.Extensions.Http.AutoClient.AutoClientOptionsValidator";
    private const string Action = "global::System.Action";
    private const string OptionsBuilderExtensions = "global::Microsoft.Extensions.Options.Validation.OptionsBuilderExtensions";
    private const string IOptionsMonitor = "global::Microsoft.Extensions.Options.IOptionsMonitor";
    private const string ServiceProviderServiceExtensions = "global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions";
    private const string MediaTypeHeaderValue = "global::System.Net.Http.Headers.MediaTypeHeaderValue";
    private const string HttpContentJsonExtensions = "global::System.Net.Http.Json.HttpContentJsonExtensions";
    private const string JsonContent = "global::System.Net.Http.Json.JsonContent";
    private const string ServiceCollectionDescriptorExtensions = "global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions";
    private const string AutoClientHttpError = "global::Microsoft.Extensions.Http.AutoClient.AutoClientHttpError";
    private const string Invariant = "global::System.FormattableString.Invariant";
    private const string UriKind = "global::System.UriKind";

    public string EmitRestApis(IReadOnlyList<RestApiType> restApiTypes, CancellationToken cancellationToken)
    {
        Dictionary<string, List<RestApiType>> metricClassesDict = new();
        foreach (var cl in restApiTypes)
        {
            if (!metricClassesDict.TryGetValue(cl.Namespace, out var list))
            {
                list = new List<RestApiType>();
                metricClassesDict.Add(cl.Namespace, list);
            }

            list.Add(cl);
        }

        foreach (var entry in metricClassesDict.OrderBy(static x => x.Key))
        {
            cancellationToken.ThrowIfCancellationRequested();
            GenTypeByNamespace(entry.Key, entry.Value, cancellationToken);
        }

        return Capture();
    }

    private static string GetPathTemplate(RestApiMethod restApiMethod)
    {
        var pathTemplateSb = new StringBuilder(restApiMethod.Path);

        var firstQuery = true;
        foreach (var param in restApiMethod.AllParameters.Where(m => m.IsQuery))
        {
            if (firstQuery)
            {
                _ = pathTemplateSb.Append($"?{param.QueryKey}={{{param.QueryKey}}}");
            }
            else
            {
                _ = pathTemplateSb.Append($"&{param.QueryKey}={{{param.QueryKey}}}");
            }

            firstQuery = false;
        }

        return pathTemplateSb.ToString();
    }

    private void GenTypeByNamespace(string nspace, IEnumerable<RestApiType> restApiTypes, CancellationToken cancellationToken)
    {
        OutLn();
        if (!string.IsNullOrWhiteSpace(nspace))
        {
            OutLn($"namespace {nspace}");
            OutOpenBrace();
        }

        foreach (var restApiClass in restApiTypes.OrderBy(static x => x.Name))
        {
            cancellationToken.ThrowIfCancellationRequested();
            GenType(restApiClass);
        }

        EmitExtensions(restApiTypes);

        if (!string.IsNullOrWhiteSpace(nspace))
        {
            OutCloseBrace();
        }

        OutLn();
    }

    private void EmitExtensions(IEnumerable<RestApiType> restApiTypes)
    {
        OutGeneratedCodeAttribute();
        OutLn("public static class AutoClientsExtensions");
        OutOpenBrace();

        foreach (var restApiType in restApiTypes.OrderBy(static x => x.Namespace + "." + x.Name))
        {
            OutLn(@$"public static {IServiceCollection} Add{restApiType.Name}(this {IServiceCollection} services)");
            OutOpenBrace();
            OutLn($"return services.Add{restApiType.Name}(_ => {{ }});");
            OutCloseBrace();
            OutLn();

            OutLn(@$"public static {IServiceCollection} Add{restApiType.Name}(
                this {IServiceCollection} services,
                {Action}<{AutoClientOptions}> configureOptions)");
            OutOpenBrace();
            OutLn(@$"{OptionsBuilderExtensions}.AddValidatedOptions<{AutoClientOptions}, {AutoClientOptionsValidator}>(services, ""{restApiType.Name}"").Configure(configureOptions);");
            OutLn($"{ServiceCollectionDescriptorExtensions}.TryAddSingleton<I{restApiType.Name}>(services, provider =>");
            OutOpenBrace();
            OutLn(@$"var httpClient = {ServiceProviderServiceExtensions}.GetRequiredService<{IHttpClientFactory}>(provider).CreateClient(""{restApiType.HttpClientName}"");");
            OutLn(@$"var autoClientOptions = {ServiceProviderServiceExtensions}.GetRequiredService<{IOptionsMonitor}<{AutoClientOptions}>>(provider).Get(""{restApiType.Name}"");");
            OutLn($"return new {restApiType.Name}(httpClient, autoClientOptions);");
            OutCloseBraceWithExtra(");");
            OutLn($"return services;");
            OutCloseBrace();
            OutLn();
        }

        OutCloseBrace();
    }

    private void GenType(RestApiType restApiType)
    {
        GetRestApiMethods(restApiType);
        OutLn();
    }

    private void GetRestApiMethods(RestApiType restApiType)
    {
        OutGeneratedCodeAttribute();

        OutLn($"{restApiType.Modifiers} {restApiType.Keyword} {restApiType.Name} {restApiType.Constraints} : I{restApiType.Name}");
        OutOpenBrace();

        EmitClassVariablesAndConstructor(restApiType);

        var dependencyName = restApiType.DependencyName;

        foreach (var restApiMethod in restApiType.Methods.OrderBy(static x => x.MethodName))
        {
            GetRestApiMethod(restApiMethod, restApiType, dependencyName);
        }

        EmitSendRequestMethod();

        OutCloseBrace();
    }

    private void GetRestApiMethod(RestApiMethod restApiMethod, RestApiType restApiType, string dependencyName)
    {
        string? ctParameter = null;

        OutLn();
        OutIndent();
        Out($"public async {Task}<{restApiMethod.ReturnType}> {restApiMethod.MethodName}(");
        foreach (var p in restApiMethod.AllParameters)
        {
            if (p != restApiMethod.AllParameters[0])
            {
                Out(", ");
            }

            Out($"{p.Type}{(p.Nullable && p.Type[p.Type.Length - 1] != '?' ? "?" : string.Empty)} {p.Name}");

            if (p.IsCancellationToken)
            {
                ctParameter = p.Name;
            }
        }

        Out(")");
        OutLn();
        OutOpenBrace();

        var pathSb = new StringBuilder(restApiMethod.Path);

        var firstQuery = true;
        foreach (var param in restApiMethod.AllParameters.Where(m => m.IsQuery))
        {
            if (firstQuery)
            {
                _ = pathSb.Append($"?{param.QueryKey}={{{param.Name}}}");
            }
            else
            {
                _ = pathSb.Append($"&{param.QueryKey}={{{param.Name}}}");
            }

            firstQuery = false;
        }

        var definePath = restApiMethod.FormatParameters.Count > 0 || !firstQuery;
        if (definePath)
        {
            OutLn(@$"var _path = {Invariant}($""{pathSb}"");");
            OutLn();
        }

        var body = restApiMethod.AllParameters.FirstOrDefault(m => m.IsBody);

        var requestName = restApiMethod.RequestName;

        OutLn($"var _httpRequestMessage = new {HttpRequestMessage}()");
        OutOpenBrace();
        if (restApiMethod.HttpMethod == "Patch")
        {
            OutPP("#if NETCOREAPP2_1_OR_GREATER");
            OutLn($"Method = {HttpMethod}.{restApiMethod.HttpMethod},");
            OutPP("#else");
            OutLn(@$"Method = new {HttpMethod}(""PATCH""),");
            OutPP("#endif");
        }
        else
        {
            OutLn($"Method = {HttpMethod}.{restApiMethod.HttpMethod},");
        }

        if (definePath)
        {
            OutLn($"RequestUri = new {Uri}(_path, {UriKind}.Relative),");
        }
        else
        {
            OutLn($"RequestUri = _uri{requestName},");
        }

        OutCloseBraceWithExtra(";");

        OutLn();

        if (body != null)
        {
            switch (body.BodyType)
            {
                case BodyContentTypeParam.ApplicationJson:
                    OutLn($@"_httpRequestMessage.Content = {JsonContent}.Create({body.Name}, _applicationJsonHeader, _autoClientOptions.JsonSerializerOptions);");
                    OutLn();
                    break;

                case BodyContentTypeParam.TextPlain:
                    OutLn(@$"string _payload = {body.Name}.ToString() ?? """";");
                    OutPP("#if NET7_0_OR_GREATER");
                    OutLn($@"_httpRequestMessage.Content = new {StringContent}(_payload, {Encoding}.UTF8, _textPlainHeader);");
                    OutPP("#else");
                    OutLn($@"_httpRequestMessage.Content = new {StringContent}(_payload, {Encoding}.UTF8, _textPlainHeader.MediaType);");
                    OutPP("#endif");
                    OutLn();
                    break;
            }
        }

        OutLn("try");
        OutOpenBrace();

        OutLn($"{TelemetryExtensions}.SetRequestMetadata(_httpRequestMessage, _requestMetadata{requestName});");

        foreach (var header in restApiType.StaticHeaders.OrderBy(static h => h.Key))
        {
            OutLn(@$"_httpRequestMessage.Headers.Add(""{header.Key}"", ""{header.Value.Replace("\"", "\\\"")}"");");
        }

        foreach (var header in restApiMethod.StaticHeaders.OrderBy(static h => h.Key))
        {
            OutLn(@$"_httpRequestMessage.Headers.Add(""{header.Key}"", ""{header.Value.Replace("\"", "\\\"")}"");");
        }

        foreach (var param in restApiMethod.AllParameters.Where(m => m.IsHeader))
        {
            OutLn($"if ({param.Name} != null)");
            OutOpenBrace();
            OutLn(@$"_httpRequestMessage.Headers.Add(""{param.HeaderName}"", {param.Name}.ToString());");
            OutCloseBrace();
        }

        OutLn();
        OutLn(@$"var _response = await SendRequest<{restApiMethod.ReturnType}>(""{dependencyName}"", _requestMetadata{requestName}.RequestRoute, _httpRequestMessage, {ctParameter ?? "default"})
                    .ConfigureAwait(false);");

        OutLn($"return _response;");
        OutCloseBrace();
        OutLn("finally");
        OutOpenBrace();
        OutLn("_httpRequestMessage.Dispose();");
        OutCloseBrace();

        OutCloseBrace();
    }

    private void EmitSendRequestMethod()
    {
        OutLn();

        OutLn(@$"private async {Task}<TResponse> SendRequest<TResponse>(
                    string dependencyName,
                    string path,
                    {HttpRequestMessage} httpRequestMessage,
                    {CancellationToken} cancellationToken)");

        Indent();
        OutLn("where TResponse : class");
        Unindent();
        OutOpenBrace();

        OutLn();
        OutLn("var response = await _httpClient.SendAsync(httpRequestMessage, cancellationToken).ConfigureAwait(false);");
        OutLn();

        OutLn($"if (typeof(TResponse) == typeof({HttpResponseMessage}))");
        OutOpenBrace();
        OutLn("return (response as TResponse)!;");
        OutCloseBrace();

        OutLn();
        OutLn("try");
        OutOpenBrace();

        OutLn("if (!response.IsSuccessStatusCode)");
        OutOpenBrace();
        OutLn($"var error = await {AutoClientHttpError}.CreateAsync(response, cancellationToken).ConfigureAwait(false);");
        OutLn($@"throw new {RestApiException}({Invariant}($""The '{{dependencyName}}' HTTP client failed with '{{response.StatusCode}}' status code.""), path, error);");
        OutCloseBrace();

        OutLn();
        OutLn(@"if (typeof(TResponse) == typeof(string))");
        OutOpenBrace();
        OutPP("#if NET5_0_OR_GREATER");
        OutLn($"var rawContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);");
        OutPP("#else");
        OutLn("cancellationToken.ThrowIfCancellationRequested();");
        OutLn($"var rawContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);");
        OutPP("#endif");
        OutLn();
        OutLn("return (rawContent as TResponse)!;");
        OutCloseBrace();

        OutLn();
        OutLn("var mediaType = response.Content.Headers.ContentType?.MediaType;");
        OutLn(@"if (mediaType == ""application/json"")");
        OutOpenBrace();
        OutLn(@$"var deserializedResponse = await {HttpContentJsonExtensions}.ReadFromJsonAsync<TResponse>(response.Content, _autoClientOptions.JsonSerializerOptions, cancellationToken)
                    .ConfigureAwait(false);");
        OutLn("if (deserializedResponse == null)");
        OutOpenBrace();
        OutLn($"var error = await {AutoClientHttpError}.CreateAsync(response, cancellationToken).ConfigureAwait(false);");
        OutLn($@"throw new {RestApiException}({Invariant}($""The '{{dependencyName}}' REST API failed to deserialize response.""), path, error);");
        OutCloseBrace();
        OutLn();
        OutLn("return deserializedResponse;");
        OutCloseBrace();

        OutLn();
        OutLn($"var err = await {AutoClientHttpError}.CreateAsync(response, cancellationToken).ConfigureAwait(false);");
        OutLn(@$"throw new {RestApiException}({Invariant}($""The '{{dependencyName}}' REST API returned an unsupported content type ('{{mediaType}}').""), path, err);");

        OutLn();
        OutCloseBrace();
        OutLn("finally");
        OutOpenBrace();
        OutLn("response.Dispose();");
        OutCloseBrace();

        OutCloseBrace();
    }

    private void EmitClassVariablesAndConstructor(RestApiType restApiType)
    {
        OutLn(@$"private static readonly {MediaTypeHeaderValue} _applicationJsonHeader = new(""application/json"")");
        OutOpenBrace();
        OutLn($@"CharSet = {Encoding}.UTF8.WebName");
        OutCloseBraceWithExtra(";");
        OutLn();

        OutLn(@$"private static readonly {MediaTypeHeaderValue} _textPlainHeader = new(""text/plain"")");
        OutOpenBrace();
        OutLn($@"CharSet = {Encoding}.UTF8.WebName");
        OutCloseBraceWithExtra(";");

        OutLn();
        var simpleMethods = restApiType.Methods.Where(m => m.FormatParameters.Count == 0 && !m.AllParameters.Any(p => p.IsQuery));
        var dependencyName = restApiType.DependencyName;
        foreach (var restApiMethod in restApiType.Methods.OrderBy(static x => x.MethodName))
        {
            var requestName = restApiMethod.RequestName;
            var path = GetPathTemplate(restApiMethod);

            if (restApiMethod.FormatParameters.Count == 0 && !restApiMethod.AllParameters.Any(p => p.IsQuery))
            {
                OutLn(@$"private static readonly {Uri} _uri{requestName} = new(""{restApiMethod.Path}"", {UriKind}.Relative);");
            }

            OutLn(@$"private static readonly {RequestMetadata} _requestMetadata{requestName} = new()");
            OutOpenBrace();
            OutLn(@$"DependencyName = ""{dependencyName}"",");
            OutLn(@$"RequestName = ""{requestName}"",");
            OutLn(@$"RequestRoute = ""{path}""");
            OutCloseBraceWithExtra(";");

            OutLn();
        }

        OutLn($"private readonly {HttpClient} _httpClient;");
        OutLn($"private readonly {AutoClientOptions} _autoClientOptions;");
        OutLn();

        OutLn($"public {restApiType.Name}({HttpClient} httpClient, {AutoClientOptions} autoClientOptions)");
        OutOpenBrace();
        OutLn(@$"_httpClient = httpClient;");
        OutLn($"_autoClientOptions = autoClientOptions;");
        OutCloseBrace();
    }
}
