using System;
using System.Linq;
using Spotify.Metadata;
using Wavee.Contracts.Common;
using Wavee.Contracts.Interfaces;
using Wavee.Contracts.Interfaces.Contracts;

namespace Wavee.UI.Spotify.Responses;

public sealed class SpotifySimpleTrack : ITrack
{
    public SpotifySimpleTrack(string id,
        string name,
        IContributor mainContributor,
        TimeSpan duration,
        ISimpleAlbum album, 
        IAudioFile[] audioFiles)
    {
        Id = id;
        Name = name;
        MainContributor = mainContributor;
        Duration = duration;
        SmallestImage = album.Images.OrderByDescending(x => x.Width).FirstOrDefault().Url;
        Album = album;
        AudioFiles = audioFiles;
    }

    public string Id { get; }
    public string Name { get; }
    public UrlImage[] Images => Album.Images;
    public IContributor MainContributor { get; }
    public TimeSpan Duration { get; }
    public string SmallestImage { get; }
    public IAudioFile[] AudioFiles { get; }
    public ISimpleAlbum Album { get; }
}

public sealed class SpotifyAudioFile : IAudioFile
{
    public SpotifyAudioFile(AudioFile file)
    {
        Id = file.FileId.ToBase64();
        Type = file.Format switch
        {
            AudioFile.Types.Format.OggVorbis96 or AudioFile.Types.Format.OggVorbis160 or AudioFile.Types.Format.OggVorbis320 => AudioFileType.Vorbis,
            AudioFile.Types.Format.Mp396 or AudioFile.Types.Format.Mp3320 => AudioFileType.Mp3,
            _ => AudioFileType.Unknown
        };
        Quality = file.Format switch
        {
            AudioFile.Types.Format.OggVorbis96 or AudioFile.Types.Format.Mp396 => AudioFileQuality.Low,
            AudioFile.Types.Format.OggVorbis160 or AudioFile.Types.Format.Mp3320 => AudioFileQuality.Medium,
            AudioFile.Types.Format.OggVorbis320 => AudioFileQuality.High,
            _ => AudioFileQuality.Low
        };
    }

    public string Id { get; }
    public AudioFileType Type { get; }
    public AudioFileQuality Quality { get; }
}