using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Eum.Spotify;
using Eum.Spotify.login5v3;
using Google.Protobuf;
using Microsoft.VisualBasic;
using Refit;
using Wavee.UI.Spotify.Common;
using Wavee.UI.Spotify.Exceptions;
using Wavee.UI.Spotify.Interfaces;
using TimeSpan = System.TimeSpan;

namespace Wavee.UI.Spotify.Clients;

internal sealed class SpotifyTokenClient
{
    private StoredToken? _token;
    private readonly Dictionary<string, byte[]?> _audioKeys = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ISpotifyAuthModule _authModule;
    private readonly string _deviceId;

    public SpotifyTokenClient(ISpotifyAuthModule authModule, string deviceId)
    {
        _authModule = authModule;
        _deviceId = deviceId;
    }

    public ValueTask<byte[]> GetAudioKey(RegularSpotifyId itemId, string fileId, CancellationToken cancellationToken)
    {
        _semaphore.Wait(cancellationToken);
        if (_audioKeys.TryGetValue(fileId, out var key))
        {
            _semaphore.Release();
            return new ValueTask<byte[]>(key);
        }

        // Fetch, check if connection is valid, and return the key.
        return new ValueTask<byte[]>(GetAudioKeyInternal(itemId, fileId, cancellationToken));
    }

    private async Task<byte[]> GetAudioKeyInternal(RegularSpotifyId itemId, string fileId,
        CancellationToken cancellationToken)
    {
        try
        {
            //Check if connection is active
            if (_token?.Connection is not { IsConnected: true })
            {
                _token?.Dispose();
                _token = null;

                _ = await GetTokenInternal(cancellationToken);
            }

            using var timeout = new CancellationTokenSource();
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
            var connection = _token!.Value.Connection;
            var key = await connection.GetAudioKey(itemId, fileId, linked.Token);

            _audioKeys[fileId] = key;
            return key;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public ValueTask<string> GetToken(CancellationToken cancellationToken)
    {
        _semaphore.Wait(cancellationToken);
        if (_token is { IsValid: true })
        {
            _semaphore.Release();
            return new ValueTask<string>(_token.Value.Value);
        }

        _token?.Dispose();
        _token = null;

        return new ValueTask<string>(GetTokenInternal(cancellationToken));
    }

    /// <summary>
    /// Gets the Spotify access token.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The Spotify access token.</returns>
    /// <exception cref="SpotifyException">Thrown when there is an authentication failure.</exception>
    /// <exception cref="UnknownSpotifyException">Thrown when an unknown error occurs.</exception>
    private async Task<string> GetTokenInternal(CancellationToken cancellationToken)
    {
        bool usingStoredCredentials = true;
        int retryCount = 0;
        const int maxRetryCount = 3;
        while (true)
        {
            try
            {
                LoginCredentials? credentials = null;
                ISpotifyConnection? connection = null;
                if (usingStoredCredentials && _token?.ReusableCredentials is not null)
                {
                    credentials = _token.Value.ReusableCredentials;
                    connection = _token.Value.Connection;
                    usingStoredCredentials = true;
                }
                else
                {
                    connection = await _authModule.Login(cancellationToken);
                    credentials = connection.AuthenticatedCredentials;
                    usingStoredCredentials = false;
                }

                var loginRequest = new LoginRequest
                {
                    ClientInfo = new Eum.Spotify.login5v3.ClientInfo
                    {
                        ClientId = SpotifyConstants.SpotifyClientId,
                        DeviceId = _deviceId,
                    },
                    StoredCredential = new StoredCredential
                    {
                        Data = credentials.AuthData,
                        Username = credentials.Username
                    }
                };
                var response = await _authModule.LoginClient.ExchangeToken(loginRequest, cancellationToken);
                if (response.Error is not LoginError.UnknownError)
                {
                    throw new SpotifyException(SpotifyFailureReason.AuthFailure, response.Error.ToString());
                }

                var token = response.Ok.AccessToken;
                var expiryTime = DateTimeOffset.UtcNow.AddSeconds(response.Ok.AccessTokenExpiresIn);
                _token = new StoredToken(token, expiryTime,
                    connection,
                    new LoginCredentials
                    {
                        AuthData = response.Ok.StoredCredential,
                        Username = credentials.Username,
                        Typ = AuthenticationType.AuthenticationStoredSpotifyCredentials
                    });
                break;
            }
            catch (ApiException apiException)
            {
                if (usingStoredCredentials)
                {
                    if (++retryCount < maxRetryCount)
                        continue;
                }

                //TODO:
                Debugger.Break();
                throw new UnknownSpotifyException(apiException);
            }
            catch (Exception unknown)
            {
                if (usingStoredCredentials)
                {
                    if (++retryCount < maxRetryCount)
                        continue;
                }

                throw new UnknownSpotifyException(unknown);
            }
        }

        _semaphore.Release();
        return _token.Value.Value;
    }


    private readonly record struct StoredToken(
        string Value,
        DateTimeOffset ExpiryTime,
        ISpotifyConnection Connection,
        LoginCredentials ReusableCredentials) : IDisposable
    {
        private static readonly TimeSpan Offset = TimeSpan.FromMinutes(1);
        public bool IsValid => DateTimeOffset.UtcNow < ExpiryTime - Offset;

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                Connection?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}