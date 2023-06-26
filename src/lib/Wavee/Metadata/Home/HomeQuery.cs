using TimeZoneConverter;
using Wavee.Metadata.Artist;

namespace Wavee.Metadata.Home;

public readonly record struct HomeQuery(TimeZoneInfo Timezone) : IGraphQLQuery
{
    private const string _operationName = "home";
    private const string _operationHash = "3099d0901548aa93509318763519c57acd1a0bb533a9793ff57732fe8b91504a";
    public string OperationName => _operationName;
    public string Operationhash => _operationHash;

    public object Variables => new
    {
        timeZone = TZConvert.WindowsToIana(Timezone.Id)
    };
}