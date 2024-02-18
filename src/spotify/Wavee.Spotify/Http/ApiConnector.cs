using System.Net;
using System.Reflection;
using Eum.Spotify.context;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Spotify.Metadata;
using Wavee.Core.Extensions;
using Wavee.Spotify.Authenticators;
using Wavee.Spotify.Exceptions;
using Wavee.Spotify.Http.Interfaces;
using Wavee.Spotify.Http.Serializers;

namespace Wavee.Spotify.Http;

public sealed class ApiConnector : IAPIConnector
{
    private readonly IAuthenticator? _authenticator;
    private readonly IJsonSerializer _systemJsonSerializer;
    private readonly IProtobufDeserializer _protobufDeserializer;
    private readonly IHttpClient _httpClient;
    private readonly IRetryHandler? _retryHandler;
    private readonly IHttpLogger? _httpLogger;
    private readonly string _deviceId;

    public event EventHandler<IResponse>? ResponseReceived;

    public ApiConnector(IAuthenticator authenticator, string deviceId) :
        this(authenticator,
            new SystemTextJsonSerializer(),
            new ProtobufDeserializer(),
            new NetHttpClient(),
            null, null, deviceId)
    {
    }

    public ApiConnector(
        IAuthenticator? authenticator,
        IJsonSerializer jsonSerializer,
        IProtobufDeserializer protobufDeserializer,
        IHttpClient httpClient,
        IRetryHandler? retryHandler,
        IHttpLogger? httpLogger, string deviceId)
    {
        _authenticator = authenticator;
        _systemJsonSerializer = jsonSerializer;
        _protobufDeserializer = protobufDeserializer;
        _httpClient = httpClient;
        _retryHandler = retryHandler;
        _httpLogger = httpLogger;
        _deviceId = deviceId;
    }

    public string DeviceId => _deviceId;

    public Task<T> Delete<T>(Uri uri, CancellationToken cancel)
    {
        Guard.NotNull(nameof(uri), uri);

        return SendAPIRequest<T>(uri, HttpMethod.Delete, cancel: cancel);
    }

    public Task<T> Delete<T>(Uri uri, IDictionary<string, string>? parameters, CancellationToken cancel)
    {
        Guard.NotNull(nameof(uri), uri);

        return SendAPIRequest<T>(uri, HttpMethod.Delete, parameters, cancel: cancel);
    }

    public Task<T> Delete<T>(Uri uri, IDictionary<string, string>? parameters, object? body, CancellationToken cancel)
    {
        Guard.NotNull(nameof(uri), uri);

        return SendAPIRequest<T>(uri, HttpMethod.Delete, parameters, body, cancel: cancel);
    }

    public async Task<HttpStatusCode> Delete(Uri uri, IDictionary<string, string>? parameters, object? body,
        CancellationToken cancel)
    {
        //Ensure.ArgumentNotNull(uri, nameof(uri));
        Guard.NotNull(nameof(uri), uri);

        var response = await SendAPIRequestDetailed(uri, HttpMethod.Delete, parameters, body, cancel: cancel)
            .ConfigureAwait(false);
        return response.StatusCode;
    }

    public Task<T> Put<T>(Uri uri, Dictionary<string, string>? headers, object? body,
        RequestContentType? bodyType,
        CancellationToken cancel = default)
    {
        Guard.NotNull(nameof(uri), uri);

        return SendAPIRequest<T>(uri, HttpMethod.Put, null, body, bodyType, headers, cancel);
    }

    public Task<T> Get<T>(Uri uri, CancellationToken cancel)
    {
        Guard.NotNull(nameof(uri), uri);

        return SendAPIRequest<T>(uri, HttpMethod.Get, cancel: cancel);
    }

    public Task<T> Post<T>(Uri uri, IDictionary<string, string>? parameters, object? body,
        Dictionary<string, string>? headers, CancellationToken cancel)
    {
        //Ensure.ArgumentNotNull(uri, nameof(uri));
        Guard.NotNull(nameof(uri), uri);

        return SendAPIRequest<T>(uri, HttpMethod.Post, parameters, body,
            RequestContentType.Json,
            headers, cancel: cancel);
    }


    public Task<T> Get<T>(Uri uri, IDictionary<string, string>? parameters, CancellationToken cancel)
    {
        Guard.NotNull(nameof(uri), uri);

        return SendAPIRequest<T>(uri, HttpMethod.Get, parameters, cancel: cancel);
    }

    public async Task<HttpStatusCode> Get(Uri uri, IDictionary<string, string>? parameters, object? body,
        CancellationToken cancel)
    {
        Guard.NotNull(nameof(uri), uri);

        var response = await SendAPIRequestDetailed(uri, HttpMethod.Get, parameters, body, cancel: cancel)
            .ConfigureAwait(false);
        return response.StatusCode;
    }

    public Task<T> Post<T>(Uri uri, CancellationToken cancel)
    {
        Guard.NotNull(nameof(uri), uri);

        return SendAPIRequest<T>(uri, HttpMethod.Post, cancel: cancel);
    }

    public Task<T> Post<T>(Uri uri, IDictionary<string, string>? parameters, CancellationToken cancel)
    {
        Guard.NotNull(nameof(uri), uri);

        return SendAPIRequest<T>(uri, HttpMethod.Post, parameters, cancel: cancel);
    }

    public Task<T> Post<T>(Uri uri, IDictionary<string, string>? parameters, object? body, CancellationToken cancel)
    {
        Guard.NotNull(nameof(uri), uri);

        return SendAPIRequest<T>(uri, HttpMethod.Post, parameters, body, cancel: cancel);
    }

    public Task<T> Post<T>(Uri uri, IDictionary<string, string>? parameters, object? body,
        RequestContentType? bodyType,
        Dictionary<string, string>? headers, CancellationToken cancel)
    {
        Guard.NotNull(nameof(uri), uri);

        return SendAPIRequest<T>(uri, HttpMethod.Post, parameters, body, bodyType, headers, cancel: cancel);
    }

    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancel)
    {
        return _httpClient.SendRaw(request, cancel);
    }

    public async Task<HttpStatusCode> Post(Uri uri, IDictionary<string, string>? parameters, object? body,
        CancellationToken cancel)
    {
        Guard.NotNull(nameof(uri), uri);

        var response = await SendAPIRequestDetailed(uri, HttpMethod.Post, parameters, body, cancel: cancel)
            .ConfigureAwait(false);
        return response.StatusCode;
    }

    public Task<T> Put<T>(Uri uri, CancellationToken cancel)
    {
        Guard.NotNull(nameof(uri), uri);

        return SendAPIRequest<T>(uri, HttpMethod.Put, cancel: cancel);
    }

    public Task<T> Put<T>(Uri uri, IDictionary<string, string>? parameters, CancellationToken cancel)
    {
        //Ensure.ArgumentNotNull(uri, nameof(uri));
        Guard.NotNull(nameof(uri), uri);

        return SendAPIRequest<T>(uri, HttpMethod.Put, parameters, cancel: cancel);
    }

    public Task<T> Put<T>(Uri uri, IDictionary<string, string>? parameters, object? body, CancellationToken cancel)
    {
        Guard.NotNull(nameof(uri), uri);

        return SendAPIRequest<T>(uri, HttpMethod.Put, parameters, body, cancel: cancel);
    }

    public async Task<HttpStatusCode> Put(Uri uri, IDictionary<string, string>? parameters, object? body,
        CancellationToken cancel)
    {
        Guard.NotNull(nameof(uri), uri);

        var response = await SendAPIRequestDetailed(uri, HttpMethod.Put, parameters, body, cancel: cancel)
            .ConfigureAwait(false);
        return response.StatusCode;
    }

    public async Task<HttpStatusCode> PutRaw(Uri uri, IDictionary<string, string>? parameters, object? body,
        CancellationToken cancel)
    {
        Guard.NotNull(nameof(uri), uri);

        var response = await SendRawRequest(uri, HttpMethod.Put, parameters, body, cancel: cancel)
            .ConfigureAwait(false);
        return response.StatusCode;
    }

    public void SetRequestTimeout(TimeSpan timeout)
    {
        _httpClient.SetRequestTimeout(timeout);
    }

    private Request CreateRequest(
        Uri uri,
        HttpMethod method,
        IDictionary<string, string>? parameters,
        object? body,
        RequestContentType? bodyType,
        IDictionary<string, string>? headers
    )
    {
        Guard.NotNull(nameof(uri), uri);
        Guard.NotNull(nameof(method), method);
        if (body != null)
        {
            Guard.NotNull(nameof(bodyType), bodyType);
        }


        return new Request(
            uri,
            method,
            headers ?? new Dictionary<string, string>(),
            parameters ?? new Dictionary<string, string>())
        {
            Body = body == null ? null : (body, bodyType!.Value)
        };
    }

    private async Task<IApiResponse<T>> DoSerializedRequest<T>(IRequest request, CancellationToken cancel)
    {
        switch (request.Body?.Item2)
        {
            case RequestContentType.Json:
                _systemJsonSerializer.SerializeRequest(request);
                break;
            case RequestContentType.Protobuf:
                _protobufDeserializer.SerializeRequest(request);
                break;
            case RequestContentType.FormUrlEncoded:
                //do nothing !
                break;
            case null:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        await using var response = await DoRequest(request, cancel).ConfigureAwait(false);
        if (string.IsNullOrEmpty(response.ContentType))
        {
            //it might be protobuf
            if (response.Body is { Length: > 0 })
            {
                return _protobufDeserializer.DeserializeResponse<T>(response);
            }
        }

        if (typeof(T) == typeof(Context) && response.ContentType == "application/json")
        {
            //Special case
            await using var str = response.Body;
            using var streamReader = new StreamReader(str);
            var jsonString = await streamReader.ReadToEndAsync(cancel);
            var typ = typeof(T);
            var descriptor = (MessageDescriptor)typ.GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static)
                .GetValue(null, null); // get the static property Descriptor

            var result = descriptor.Parser.ParseJson(jsonString);
            return new ApiResponse<T>(response, (T)result);
        }

        return response.ContentType switch
        {
            "application/json" => _systemJsonSerializer.DeserializeResponse<T>(response),
            "application/x-protobuf" => _protobufDeserializer.DeserializeResponse<T>(response),
            "application/protobuf" => _protobufDeserializer.DeserializeResponse<T>(response),
            string s when s.StartsWith("vnd.spotify") => _protobufDeserializer.DeserializeResponse<T>(response),
            _ => throw new ApiException(response)
        };
    }

    private async Task<IResponse> DoRequest(IRequest request, CancellationToken cancel)
    {
        await ApplyAuthenticator(request).ConfigureAwait(false);
        _httpLogger?.OnRequest(request);
        IResponse response = await _httpClient.DoRequest(request, cancel).ConfigureAwait(false);
        _httpLogger?.OnResponse(response);
        ResponseReceived?.Invoke(this, response);
        if (_retryHandler != null)
        {
            response = await _retryHandler.HandleRetry(request, response, async (newRequest, ct) =>
            {
                await ApplyAuthenticator(request).ConfigureAwait(false);
                var newResponse = await _httpClient.DoRequest(request, ct).ConfigureAwait(false);
                _httpLogger?.OnResponse(newResponse);
                ResponseReceived?.Invoke(this, response);
                return newResponse;
            }, cancel).ConfigureAwait(false);
        }

        ProcessErrors(response);
        return response;
    }

    private async Task ApplyAuthenticator(IRequest request)
    {
        if (_authenticator != null
            && !request.Endpoint.AbsoluteUri.Contains("accounts.spotify.com")
            && !request.Endpoint.AbsoluteUri.Contains("login5.spotify.com"))
        {
            await _authenticator!.Apply(_deviceId, request, this).ConfigureAwait(false);
        }
    }

    public Task<IResponse> SendRawRequest(
        Uri uri,
        HttpMethod method,
        IDictionary<string, string>? parameters = null,
        object? body = null,
        RequestContentType? bodyType = null,
        IDictionary<string, string>? headers = null,
        CancellationToken cancel = default
    )
    {
        var request = CreateRequest(uri, method, parameters, body, bodyType, headers);
        return DoRequest(request, cancel);
    }

    public async Task<T> SendAPIRequest<T>(
        Uri uri,
        HttpMethod method,
        IDictionary<string, string>? parameters = null,
        object? body = null,
        RequestContentType? bodyType = null,
        IDictionary<string, string>? headers = null,
        CancellationToken cancel = default
    )
    {
        var request = CreateRequest(uri, method, parameters, body, bodyType, headers);
        IApiResponse<T> apiResponse = await DoSerializedRequest<T>(request, cancel).ConfigureAwait(false);
        return apiResponse.Body!;
    }

    public async Task<IResponse> SendAPIRequestDetailed(
        Uri uri,
        HttpMethod method,
        IDictionary<string, string>? parameters = null,
        object? body = null,
        RequestContentType? bodyType = null,
        IDictionary<string, string>? headers = null,
        CancellationToken cancel = default
    )
    {
        var request = CreateRequest(uri, method, parameters, body, bodyType, headers);
        var response = await DoSerializedRequest<object>(request, cancel).ConfigureAwait(false);
        return response.Response;
    }

    private static void ProcessErrors(IResponse response)
    {
        Guard.NotNull(nameof(response), response);
        if ((int)response.StatusCode >= 200 && (int)response.StatusCode < 400)
        {
            return;
        }

        throw response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new ApiUnauthorizedException(response),
            HttpStatusCode.TooManyRequests => new ApiTooManyRequestsException(response),
            _ => new ApiException(response),
        };
    }
}

public class Request : IRequest
{
    public Request(Uri endpoint, HttpMethod method)
    {
        Headers = new Dictionary<string, string>();
        Parameters = new Dictionary<string, string>();
        Endpoint = endpoint;
        Method = method;
    }

    public Request(Uri endpoint, HttpMethod method, IDictionary<string, string> headers)
    {
        Headers = headers;
        Parameters = new Dictionary<string, string>();
        Endpoint = endpoint;
        Method = method;
    }

    public Request(
        Uri endpoint,
        HttpMethod method,
        IDictionary<string, string> headers,
        IDictionary<string, string> parameters)
    {
        Headers = headers;
        Parameters = parameters;
        Endpoint = endpoint;
        Method = method;
    }

    public Uri Endpoint { get; set; }

    public IDictionary<string, string> Headers { get; }

    public IDictionary<string, string> Parameters { get; }

    public HttpMethod Method { get; set; }

    public (object Data, RequestContentType)? Body { get; set; }
}