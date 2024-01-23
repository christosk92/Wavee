using Eum.Spotify;
using Wavee.Spfy.DefaultServices;

namespace Wavee.Spfy;

internal static class EntityManager
{
    private readonly record struct AccessToken(string Token, DateTimeOffset Expires);

    private static readonly Dictionary<Guid, ITcpClient> Clients = new();
    private static readonly Dictionary<Guid, WaveeSpotifyClient> RootClients = new();
    private static readonly Dictionary<Guid, APWelcome> WelcomeMessages = new();
    private static readonly Dictionary<Guid, string> CountryCodes = new();
    private static readonly Dictionary<Guid, IWebsocketClient> WebsocketClients = new();
    private static readonly Dictionary<Guid, string> WebsocketConnectionIds = new();
    
    private static readonly Dictionary<Guid, uint> SendSequences = new();
    private static readonly Dictionary<Guid, uint> ReceiveSequences = new();
    private static readonly Dictionary<Guid, SemaphoreSlim> SendSequenceLocks = new();
    private static readonly Dictionary<Guid, SemaphoreSlim> ReceiveSequenceLocks = new();


    private static readonly Dictionary<Guid, uint> AesKeySequences = new();
    private static readonly Dictionary<Guid, SemaphoreSlim> AesKeySequencesLocks = new();

    private static readonly Dictionary<Guid, ulong> MercurySequences = new();
    private static readonly Dictionary<Guid, SemaphoreSlim> MercurySequenceLocks = new();

    private static readonly Dictionary<Guid, ReadOnlyMemory<byte>> SendKeys = new();
    private static readonly Dictionary<Guid, ReadOnlyMemory<byte>> ReceiveKeys = new();

    public static bool TryGetClient(Guid instanceId, out WaveeSpotifyClient client)
    {
        if (RootClients.TryGetValue(instanceId, out var c))
        {
            client = c;
            return true;
        }

        client = default;
        return false;
    }

    public static void SetRootClient(Guid instanceId, WaveeSpotifyClient cl)
    {
        RootClients[instanceId] = cl;
    }

    public static bool TryGetConnection(Guid instanceId, out ITcpClient? tcpClient, out APWelcome? welcomMessage)
    {
        if (Clients.TryGetValue(instanceId, out tcpClient))
        {
            if (!tcpClient.IsConnected)
            {
                RemoveConnection(instanceId);
                tcpClient = default;
                welcomMessage = default;
                return false;
            }

            var welcomeMessage = WelcomeMessages[instanceId];
            welcomMessage = welcomeMessage;
            return true;
        }

        welcomMessage = default;
        tcpClient = default;
        return false;
    }

    public static bool TryGetSendKey(Guid instanceId, out ReadOnlyMemory<byte> sendKey, out uint sendSequence)
    {
        if (SendKeys.TryGetValue(instanceId, out sendKey) &&
            SendSequenceLocks.TryGetValue(instanceId, out var sendSeqLock))
        {
            sendSeqLock.Wait();
            sendSequence = SendSequences[instanceId];
            SendSequences[instanceId] = sendSequence + 1;
            sendSeqLock.Release();
            return true;
        }

        sendKey = default;
        sendSequence = default;
        return false;
    }

    public static bool TryGetReceiveKey(Guid instanceId, out ReadOnlyMemory<byte> receiveKey, out uint receiveSequence)
    {
        if (ReceiveKeys.TryGetValue(instanceId, out receiveKey) &&
            ReceiveSequenceLocks.TryGetValue(instanceId, out var recvSeqLock))
        {
            recvSeqLock.Wait();
            receiveSequence = ReceiveSequences[instanceId];
            ReceiveSequences[instanceId] = receiveSequence + 1;
            recvSeqLock.Release();
            return true;
        }

        receiveKey = default;
        receiveSequence = default;
        return false;
    }

    public static void SaveConnection(Guid instanceId,
        ITcpClient tcpClient,
        ReadOnlyMemory<byte> xSendKey,
        ReadOnlyMemory<byte> xReceiveKey)
    {
        RemoveConnection(instanceId);

        Clients.Add(instanceId, tcpClient);
        SendKeys.Add(instanceId, xSendKey);
        ReceiveKeys.Add(instanceId, xReceiveKey);
        SendSequences.Add(instanceId, 0);
        AesKeySequences.Add(instanceId, 0);
        ReceiveSequences.Add(instanceId, 0);
        SendSequenceLocks.Add(instanceId, new SemaphoreSlim(1, 1));
        ReceiveSequenceLocks.Add(instanceId, new SemaphoreSlim(1, 1));
        AesKeySequencesLocks.Add(instanceId, new SemaphoreSlim(1, 1));
        MercurySequenceLocks.Add(instanceId, new SemaphoreSlim(1, 1));
        MercurySequences.Add(instanceId, 0);
    }

    internal static void RemoveConnection(Guid instanceId)
    {
        if (!Clients.ContainsKey(instanceId))
        {
            return;
        }

        var client = Clients[instanceId];
        client.Dispose();

        Clients.Remove(instanceId);
        AesKeySequences.Remove(instanceId);
        SendKeys.Remove(instanceId);
        ReceiveKeys.Remove(instanceId);
        SendSequences.Remove(instanceId);
        ReceiveSequences.Remove(instanceId);
        WelcomeMessages.Remove(instanceId);

        SendSequenceLocks.Remove(instanceId, out var sendSeqLock);
        sendSeqLock?.Dispose();
        ReceiveSequenceLocks.Remove(instanceId, out var recvSeqLock);
        recvSeqLock?.Dispose();
             
        AesKeySequencesLocks.Remove(instanceId, out var aesSeqLock);
        MercurySequenceLocks.Remove(instanceId, out var mercurySeqLock);
        mercurySeqLock?.Dispose();
        
        MercurySequences.Remove(instanceId);
        aesSeqLock?.Dispose();
    }

    public static bool TryGetAesKeySequence(Guid instanceId, out uint o)
    {
        if (AesKeySequencesLocks.TryGetValue(instanceId, out var lockObj))
        {
            lockObj.Wait();
            var seq = AesKeySequences[instanceId];
            AesKeySequences[instanceId] = seq + 1;
            lockObj.Release();

            o = seq;
            return true;
        }

        o = default;
        return false;
    }

    public static void SaveWelcomeMessage(Guid instanceId, APWelcome welcomeMessage)
    {
        WelcomeMessages[instanceId] = welcomeMessage;
    }

    public static void SetCountryCode(Guid instanceId, string countryCode)
    {
        CountryCodes[instanceId] = countryCode;
    }

    public static bool TryGetWebsocketConnection(Guid instanceId, out IWebsocketClient o)
    {
        if (WebsocketClients.TryGetValue(instanceId, out var wsClient))
        {
            if (!wsClient.IsConnected)
            {
                RemoveWebsocketConnection(instanceId);
                o = default;
                return false;
            }

            o = wsClient;
            return true;
        }

        o = default;
        return false;
    }

    internal static void RemoveWebsocketConnection(Guid instanceId)
    {
        if (!WebsocketClients.ContainsKey(instanceId))
        {
            return;
        }

        var client = WebsocketClients[instanceId];
        client.Dispose();

        WebsocketConnectionIds.Remove(instanceId);
        WebsocketClients.Remove(instanceId);
    }

    public static void SaveWebsocketConnection(Guid instanceId, IWebsocketClient ws, string connectionId)
    {
        RemoveWebsocketConnection(instanceId);
        
        WebsocketConnectionIds.Add(instanceId, connectionId);
        WebsocketClients.Add(instanceId, ws);
    }

    public static bool TryGetWebsocketConnectionId(Guid instanceId, out string connId)
    {
        if (WebsocketConnectionIds.TryGetValue(instanceId, out var id))
        {
            connId = id;
            return true;
        }

        connId = default;
        return false;
    }

    public static bool TryGetMercurySeq(Guid instanceId, out ulong o)
    {
        if (MercurySequenceLocks.TryGetValue(instanceId, out var lockObj))
        {
            lockObj.Wait();
            var seq = MercurySequences[instanceId];
            MercurySequences[instanceId] = seq + 1;
            lockObj.Release();

            o = seq;
            return true;
        }
        
        o = default;
        return false;
    }
}