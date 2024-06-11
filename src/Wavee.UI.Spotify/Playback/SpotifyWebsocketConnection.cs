using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Eum.Spotify.connectstate;
using Eum.Spotify.transfer;
using Nito.AsyncEx;
using Wavee.UI.Spotify.Exceptions;
using Wavee.UI.Spotify.Interfaces;

namespace Wavee.UI.Spotify.Playback;

internal sealed class SpotifyWebsocketConnection : ISpotifyWebsocketConnection
{
    private readonly CancellationTokenSource _cts = new();
    private readonly ClientWebSocket _socket;
    private readonly string _url;
    private readonly AsyncManualResetEvent _waitForConnection = new(false);
    private readonly ISpotifyMessageHandler _messageHandler;
    private readonly ISpotifyRequestHandler _requestHandler;

    public SpotifyWebsocketConnection(string url, ISpotifyMessageHandler messageHandler,
        ISpotifyRequestHandler requestHandler)
    {
        _url = url;
        _messageHandler = messageHandler;
        _requestHandler = requestHandler;
        _socket = new ClientWebSocket();
        _socket.Options.KeepAliveInterval = TimeSpan.FromHours(1);
        _socket.Options.SetRequestHeader("Origin", "https://open.spotify.com");

        Task.Factory.StartNew(async () =>
        {
            await _waitForConnection.WaitAsync(_cts.Token);
            while (!_cts.Token.IsCancellationRequested)
            {
                JsonDocument message;
                try
                {
                    message = await ReadNextMessageAsync(_cts.Token);
                }
                catch (Exception e)
                {
                    Disconnected?.Invoke(this, (e, _socket.CloseStatus));
                    break;
                }

                try
                {
                    var root = message.RootElement;
                    var messageHeaders = new Dictionary<string, string>();
                    if (root.TryGetProperty("headers", out var headersElement))
                    {
                        using var enumerator = headersElement.EnumerateObject();
                        foreach (var curr in enumerator)
                        {
                            messageHeaders.Add(curr.Name, curr.Value.GetString()!);
                        }
                    }

                    var type = root.GetProperty("type").GetString();
                    switch (type)
                    {
                        case "request":
                        {
                            var ident = root.GetProperty("message_ident").GetString();
                            _requestHandler.HandleRequest(ident, root, messageHeaders);
                            break;
                        }
                        case "message":
                        {
                            var uri = root.GetProperty("uri").GetString();
                            _messageHandler.HandleUri(uri, root, messageHeaders);
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
                finally
                {
                    message.Dispose();
                }
            }

            _cts.Dispose();
        });
    }

    public bool Connected => _socket.State == WebSocketState.Open;
    public event EventHandler<Cluster>? ClusterChanged;
    public event EventHandler<(Exception?, WebSocketCloseStatus?)>? Disconnected;

    public async Task<string> Connect(CancellationToken cancellationToken)
    {
        await _socket.ConnectAsync(new Uri(_url), cancellationToken);
        using var connectionIdMessage = await ReadNextMessageAsync(cancellationToken);
        _waitForConnection.Set();
        var headers = connectionIdMessage.RootElement.GetProperty("headers");
        var connectionIdVal = headers.GetProperty("Spotify-Connection-Id").GetString();
        if (connectionIdVal == null)
        {
            throw new UnknownSpotifyException("Spotify-Connection-Id header not found");
        }

        return connectionIdVal;
    }

    private async Task<Stream> Receive(CancellationToken cancellationToken = default)
    {
        var message = new MemoryStream();
        var endOfMessage = false;
        while (!endOfMessage)
        {
            var buffer = new byte[1024 * 4];
            var segment = new ArraySegment<byte>(buffer);
            var result = await _socket.ReceiveAsync(segment, cancellationToken: cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                throw new WebSocketException(error: WebSocketError.ConnectionClosedPrematurely);
            }

            message.Write(buffer, 0, result.Count);
            endOfMessage = result.EndOfMessage;
        }

        message.Seek(0, SeekOrigin.Begin);

        return message;
    }

    private async Task<JsonDocument> ReadNextMessageAsync(CancellationToken cancellationToken)
    {
        await using var message = await Receive(cancellationToken);
        var jsondoc = await JsonDocument.ParseAsync(message, cancellationToken: cancellationToken);
        return jsondoc;
    }

    public void Dispose()
    {
        _socket?.Dispose();
        _cts.Cancel();
    }

    private async Task HandleCommandAsync(JsonElement cmd, uint messageId, string sentBy)
    {
        var endpoint = cmd.GetProperty("endpoint").GetString();
        switch (endpoint)
        {
            case "transfer":
            {
                var data = TransferState.Parser.ParseFrom(cmd.GetProperty("data").GetBytesFromBase64());
                await HandleTransferCommandAsync(data);
                break;
            }
        }
    }

    private async Task HandleTransferCommandAsync(TransferState cmd)
    {
        throw new NotImplementedException();
    }
}