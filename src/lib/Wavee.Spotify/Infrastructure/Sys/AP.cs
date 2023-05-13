using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using LanguageExt.UnsafeValueAccess;
using Wavee.Core.Infrastructure.Sys.IO;
using Wavee.Core.Infrastructure.Traits;

[assembly: InternalsVisibleTo("Wavee.Spotify.Playback")]

namespace Wavee.Spotify.Infrastructure.Sys;

internal static class AP<RT> where RT : struct, HasHttp<RT>
{
    const string AP_URL = "https://apresolve.spotify.com/?type=accesspoint&type=spclient&type=dealer";

    private static Ref<Option<(string Host, ushort Port)>>
        FETCHED_AP_URL = Ref(Option<(string Host, ushort Port)>.None);

    private static Ref<Option<(string Host, ushort Port)>> FETCHED_SP_CLIENT_URL =
        Ref(Option<(string Host, ushort Port)>.None);

    private static Ref<Option<(string Host, ushort Port)>> FETCHED_DEALER_CLIENT_URL =
        Ref(Option<(string Host, ushort Port)>.None);


    public static Aff<RT, (string Host, ushort Port)> FetchAP(CancellationToken ct = default) =>
        from potentialAff in Eff(() => FETCHED_AP_URL.Value)
            .Map(x =>
            {
                return x.Match(
                    Some: y=> SuccessAff(y),
                    None: () =>
                        from _ in Populate(ct)
                        from item in Eff(() => FETCHED_AP_URL.Value)
                        select item.ValueUnsafe());
            })
        from item in potentialAff
        select item;

    public static Aff<RT, (string Host, ushort Port)> FetchSpClient(CancellationToken ct = default) =>
        from potentialAff in Eff(() => FETCHED_SP_CLIENT_URL.Value)
            .Map(x =>
            {
                return x.Match(
                    Some: y=> SuccessAff(y),
                    None: () =>
                        from _ in Populate(ct)
                        from item in Eff(() => FETCHED_SP_CLIENT_URL.Value)
                        select item.ValueUnsafe());
            })
        from item in potentialAff
        select item;

    public static Aff<RT, (string Host, ushort Port)> FetchDealer(CancellationToken ct = default) =>
        from potentialAff in Eff(() => FETCHED_DEALER_CLIENT_URL.Value)
            .Map(x =>
            {
                return x.Match(
                    Some: y=> SuccessAff(y),
                    None: () =>
                        from _ in Populate(ct)
                        from item in Eff(() => FETCHED_DEALER_CLIENT_URL.Value)
                        select item.ValueUnsafe());
            })
        from item in potentialAff
        select item;

    // {
    //     if (!FETCHED_DEALER_CLIENT_URL.Value.IsNone)
    //     {
    //         return SuccessEff<RT, (string Host, ushort Port)>(FETCHED_DEALER_CLIENT_URL.Value.ValueUnsafe());
    //     }
    //
    //
    //     return from httpResponse in Http<RT>.Get(AP_URL, Option<AuthenticationHeaderValue>.None,
    //             Option<HashMap<string, string>>.None) 
    //         
    //         select splitted;
    // }

    private static Aff<RT, Unit> Populate(CancellationToken ct) =>
        from items in Http<RT>.Get(AP_URL, Option<AuthenticationHeaderValue>.None,
                Option<HashMap<string, string>>.None)
            .MapAsync(async x =>
            {
                x.EnsureSuccessStatusCode();
                var stream = await x.Content.ReadFromJsonAsync<ApResolveData>(ct);

                return (
                    stream.AccessPoint.Select(Parse).ToSeq(),
                    stream.Dealer.Select(Parse).ToSeq(),
                    stream.SpClient.Select(Parse).ToSeq()
                );
            })
        from __ in atomic(Eff(() =>
        {
            FETCHED_AP_URL.Swap(_ => Option<(string Host, ushort Port)>.Some(items.Item1.First()));
            FETCHED_DEALER_CLIENT_URL.Swap(_ => Option<(string Host, ushort Port)>.Some(items.Item2.First()));
            FETCHED_SP_CLIENT_URL.Swap(_ => Option<(string Host, ushort Port)>.Some(items.Item3.First()));
            return unit;
        }))
        select unit;

    private static (string Host, ushort Port) Parse(string input)
    {
        var split = input.Split(":", 2);
        return (split[0], ushort.Parse(split[1]));
    }

    private readonly record struct ApResolveData(
        [property: JsonPropertyName("accesspoint")]
        string[] AccessPoint,
        [property: JsonPropertyName("dealer")] string[] Dealer,
        [property: JsonPropertyName("spclient")]
        string[] SpClient);
}