using System.Linq.Expressions;
using LanguageExt;
using LanguageExt.Effects.Traits;
using static LanguageExt.Prelude;

namespace Wavee.Spotify.Cache;


internal static class FileRepo<R>
    where R : struct,
    HasDatabase<R>
{
    public static Aff<R, EncryptedAudioFile> Create(NewAudioFile item)
        =>
            from entity in SuccessEff(new FileEntity(
                Id: item.File.FileId.ToBase64(),
                CompressedData: item.Data.ToArray(),
                FormatType: (int)item.File.Format,
                CreatedAt: item.CreatedAt))
            from id in Database<R>.Insert<FileEntity, string>(entity)
            select To(entity);


    public static Aff<R, Option<EncryptedAudioFile>> FindOne(Expression<Func<FileEntity, bool>> filter)
        => from track in Database<R>.FindOne<FileEntity>(filter)
            select track.Map(To);

    private static EncryptedAudioFile To(FileEntity track)
    {
        return new EncryptedAudioFile(
            Data: track.CompressedData,
            FileId: track.Id,
            FormatType: track.FormatType
        );
    }
    //
    // private static Eff<string> Sha256Hash(ReadOnlyMemory<byte> itemData)
    // {
    //     var hash = Convert.ToBase64String((ReadOnlySpan<byte>)SHA256.HashData(itemData.Span));
    //
    //     return Eff<string>.Success(hash);
    // }
}