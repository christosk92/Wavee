using System.Text;
using Eum.Spotify;
using Serilog;
using Wavee.Infrastructure.Connection;
using Wavee.Infrastructure.Remote;
using Wavee.Remote;
using Wavee.Remote.Live;
using Wavee.Spotify.Infrastructure.Connection;
using Wavee.Token;
using Wavee.Token.Live;

namespace Wavee;

public class SpotifyClient
{
    private Guid _connectionId;
    private TaskCompletionSource<string> _countryCodeTask;
    private TaskCompletionSource _waitForConnectionTask;

    public SpotifyClient(LoginCredentials credentials, SpotifyConfig config)
    {
        var deviceId = Guid.NewGuid().ToString("N");
        _waitForConnectionTask = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        void CreateConnectionRecursively()
        {
            _countryCodeTask = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            _connectionId = ConnectionFactory(credentials, config, deviceId, async (err) =>
            {
                Log.Logger.Error(err, "Connection lost");
                await Task.Delay(2000);
                CreateConnectionRecursively();
            });
            //setup country code
            var listener = _connectionId.CreateListener((ref SpotifyUnencryptedPackage package) =>
                package.Type is SpotifyPacketType.CountryCode);
            Task.Run(async () =>
            {
                try
                {
                    if (await listener.Reader.WaitToReadAsync())
                    {
                        var countryCode = await listener.Reader.ReadAsync();
                        var countryCodeString = Encoding.UTF8.GetString(countryCode.Payload.Span);
                        _countryCodeTask.TrySetResult(countryCodeString);
                    }
                }
                catch (Exception e)
                {
                    _countryCodeTask.SetException(e);
                }
                finally
                {
                    listener.Finished();
                }
            });
        }

        CreateConnectionRecursively();
        Task.Run(async () =>
        {
            await SpotifyRemoteConnection.Create(
                deviceId: deviceId,
                connectionId: _connectionId,
                accessToken: Token.GetToken,
                config: config
            );
            _waitForConnectionTask.TrySetResult();
        });
    }

    public ITokenClient Token => new LiveTokenClient(connId: _connectionId);
    public IMercuryClient Mercury => new LiveTokenClient(connId: _connectionId);
    public ISpotifyRemoteClient Remote => new LiveSpotifyRemoteClient(_connectionId, _waitForConnectionTask);

    public ValueTask<string> Country => _countryCodeTask.Task.IsCompleted
        ? new ValueTask<string>(_countryCodeTask.Task.Result)
        : new ValueTask<string>(_countryCodeTask.Task);

    private static Guid ConnectionFactory(LoginCredentials credentials, SpotifyConfig config,
        string deviceId,
        Action<Exception> onConnectionLost)
    {
        Guid? connectionId = null;
        connectionId = SpotifyConnection.Create(credentials, config, deviceId, onConnectionLost, connectionId);
        return connectionId.Value;
    }
}