using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using Eum.Spotify;
using Wavee.Infrastructure.Live;
using Wavee.Spotify.Sys;
using Wavee.Spotify.Sys.Connection;
using Wavee.Spotify.Sys.Connection.Contracts;

namespace Wavee.Spotify;

public static class SpotifyClient
{
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<SpotifyConnectionInfo> Authenticate(LoginCredentials credentials,
        CancellationToken cancellationToken = default)
    {
        var deviceId = Guid.NewGuid().ToString();

        var connectionId = await SpotifyConnection<WaveeRuntime>.Authenticate(
            deviceId,
            credentials,
            cancellationToken).Run(WaveeCore.Runtime);

        return connectionId
            .Match(
                Succ: g => g,
                Fail: e => throw e
            );
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async ValueTask<string> CountryCode(this SpotifyConnectionInfo connection)
    {
        var countryCode = await ConnectionListener<WaveeRuntime>.ConsumePacket(connection.ConnectionId,
                static p => p.Command is SpotifyPacketType.CountryCode,
                static () => false)
            .Run(WaveeCore.Runtime);
        return countryCode
            .Match(
                Succ: p => Encoding.UTF8.GetString(p.Data.Span),
                Fail: e => throw e
            );
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async ValueTask<HashMap<string, string>> ProductInfo(this SpotifyConnectionInfo connection)
    {
        var productInfo = await ConnectionListener<WaveeRuntime>.ConsumePacket(connection.ConnectionId,
                static p => p.Command is SpotifyPacketType.ProductInfo,
                static () => false)
            .Map(c =>
            {
                var productInfoString = Encoding.Default.GetString(@c.Data.Span);

                var attributes = new HashMap<string, string>();
                var xml = new XmlDocument();
                xml.LoadXml(productInfoString);

                var products = xml.SelectNodes("products");
                if (products != null && products.Count > 0)
                {
                    var firstItemAsProducts = products[0];

                    var product = firstItemAsProducts.ChildNodes[0];

                    var properties = product.ChildNodes;
                    for (var i = 0; i < properties.Count; i++)
                    {
                        var node = properties.Item(i);
                        //attributes[node.Name] = node.InnerText;
                        attributes = attributes.AddOrUpdate(node.Name, node.InnerText);
                    }
                }

                return attributes;
            })
            .Run(WaveeCore.Runtime);

        return productInfo
            .Match(
                Succ: p => p,
                Fail: e => throw e
            );
    }
}