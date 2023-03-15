using System.Security.Cryptography;

namespace Wavee.UI.AudioImport;
internal class PlaycountService : IPlaycountService
{
    public ulong GetPlaycount(string id) => throw new NotImplementedException();

    public ulong[] GetPlaycounts(string[] ids)
    {
        //return random values
        return ids
            .Select(_ => (ulong)RandomNumberGenerator.GetInt32(412, 8811))
            .ToArray();
    }
}
