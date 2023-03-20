using System.Collections.Immutable;
using Wavee.Enums;
using Wavee.Interfaces.Models;
using Wavee.Models;
using Wavee.UI.ViewModels.Album;
using Wavee.UI.ViewModels.Artist;

namespace Wavee.UI.Models.Local;

public readonly record struct ShortLocalTrack
{
    public string Id
    {
        get; init;
    }
    public DateTime DateImported
    {
        get; init;
    }

    public DateTime LastChanged
    {
        get;
        init;
    }
}

/// <summary>
/// A struct representing a track on a disk.
/// </summary>
public readonly record struct LocalTrack : ITrack
{
    public ServiceType Service => ServiceType.Local;

    public string Id
    {
        get; init;
    }

    IAlbum ITrack.Album => new LocalAlbum(
        Image: Image,
        Title: Album,
        Service: ServiceType.Local);

    ImmutableArray<IArtist> ITrack.Artists => Performers
        .Select(j => new LocalArtist(
            Image: null,
            Title: j,
            Service: ServiceType.Local
        ))
        .Cast<IArtist>()
        .ToImmutableArray();

    public ImmutableArray<DescriptionItem> Descriptions => Performers
        .Select(artist => new DescriptionItem(
            Title: artist,
            NavigateTo: typeof(ArtistViewModel),
            Parameter: artist
        )).ToImmutableArray();

    public DescriptionItem Group => new DescriptionItem(
        Title: Album,
        NavigateTo: typeof(AlbumViewModel),
        Parameter: Album);

    public string Image
    {
        get; init;
    }

    /// <summary>
    ///    Gets and sets the title for the media described by the
    ///    current instance.
    /// </summary>
    /// <value>
    ///    A <see cref="T:System.String" /> object containing the title for
    ///    the media described by the current instance or <see langword="null" /> if no value is present.
    /// </value>
    /// <remarks>
    ///    The title is most commonly the name of the song or
    ///    episode or a movie title. For example, "Daydream
    ///    Believer" (a song by the Monkies), "Space Seed" (an
    ///    episode of Star Trek), or "Harold and Kumar Go To White
    ///    Castle" (a movie).
    /// </remarks>
    public string Title
    {
        get; init;
    }

    #region Tag Properties

    /// <summary>
    ///    Gets and sets the sort name for the title of the media
    ///    described by the current instance.
    /// </summary>
    /// <value>
    ///    A <see cref="T:System.String" /> object containing the sort name for
    ///    the title of the media described by the current instance or <see langword="null" /> if no value is present.
    /// </value>
    /// <remarks>
    ///    Possibly used to sort compilations, or episodic content.
    /// </remarks>
    public string TitleSort
    {
        get; init;
    }

    /// <summary>
    ///    Gets and sets a short description, one-liner.
    ///    It represents the tagline of the Video/music.
    /// </summary>
    /// <value>
    ///    A <see cref="T:System.String" /> containing the subtitle
    ///    the media represented by the current instance
    ///    or an empty array if no value is present.
    /// </value>
    /// <remarks>
    ///    <para>This field gives a nice/short precision to
    ///    the title, which is typically below the title on the
    ///    front cover of a media.
    ///    For example, for "Back to the future", this would be
    ///    "It's About Time".
    ///    </para>
    /// </remarks>
    public string Subtitle
    {
        get; init;
    }

    /// <summary>
    ///    Gets and sets a short description of the media.
    ///    For a music, this could be the comment that the artist
    ///    made of its artwork. For a video, this should be a
    ///    short summary of the story/plot, but a spoiler. This
    ///    should give the impression of what to expect in the
    ///    media.
    /// </summary>
    /// <value>
    ///    A <see cref="T:System.String" /> containing the subtitle
    ///    the media represented by the current instance
    ///    or an empty array if no value is present.
    /// </value>
    /// <remarks>
    ///    <para>This is especially relevant for a movie.
    ///    For example, for "Back to the Future 2", this could be
    ///    "After visiting 2015, Marty McFly must repeat his visit
    ///    to 1955 to prevent disastrous changes to 1985...without
    ///    interfering with his first trip".
    ///    </para>
    /// </remarks>
    public string Description
    {
        get; init;
    }

    /// <summary>
    ///    Gets and sets the performers or artists who performed in
    ///    the media described by the current instance.
    /// </summary>
    /// <value>
    ///    A <see cref="T:string[]" /> containing the performers or
    ///    artists who performed in the media described by the
    ///    current instance or an empty array if no value is
    ///    present.
    /// </value>
    /// <remarks>
    ///    <para>This field is most commonly called "Artists" in
    ///    Audio media, or "Actor" in Video media, and should be
    ///    used to represent each artist/actor appearing in the
    ///    media. It can be simple in the form of "The Beatles"
    ///    or more complicated in the form of "John Lennon,
    ///    Paul McCartney, George Harrison, Pete Best", depending
    ///    on the preferences of the listener/spectator
    ///    and the degree to which they organize their media
    ///    collection.</para>
    ///    <para>As the preference of the user may vary,
    ///    applications should not try to limit the user in what
    ///    choice they may make.</para>
    /// </remarks>
    public string[] Performers
    {
        get; init;
    }

    /// <summary>
    ///    Gets and sets the sort names of the performers or artists
    ///    who performed in the media described by the current instance.
    /// </summary>
    /// <value>
    ///    A <see cref="T:string[]" /> containing the sort names for
    ///    the performers or artists who performed in the media
    ///    described by the current instance, or an empty array if
    ///    no value is present.
    /// </value>
    /// <remarks>
    ///    <para>This is used to provide more control over how tracks
    ///    are sorted. Typical uses are to skip common prefixes or
    ///    sort by last name. For example, "The Beatles" might be
    ///    sorted as "Beatles, The".
    ///    </para>
    /// </remarks>
    public string[] PerformersSort
    {
        get; init;
    }


    /// <summary>
    ///    Gets and sets the Charaters for a video media, or
    ///    instruments played for music media.
    ///    This should match the <see cref="P:TagLib.Tag.Performers" /> array (for
    ///    each person correspond one/more role). Several roles for
    ///    the same artist/actor can be made up with semicolons.
    ///    For example, "Marty McFly; Marty McFly Jr.; Marlene McFly".
    /// </summary>
    /// <remarks>
    ///    <para> This is typically usefull for movies, although the
    ///    instrument played by each artist in a music may be of
    ///    relevance.
    ///    </para>
    ///    <para>It is highly important to match each role to the
    ///    performers. This means that a role may be <see langword="null" /> to keep the match between a
    ///    Performers[i] and PerformersRole[i].
    ///    </para>
    /// </remarks>
    public string[] PerformersRole
    {
        get; init;
    }

    /// <summary>
    ///    Gets and sets the band or artist who is credited in the
    ///    creation of the entire album or collection containing the
    ///    media described by the current instance.
    /// </summary>
    /// <value>
    ///    A <see cref="T:string[]" /> containing the band or artist
    ///    who is credited in the creation of the entire album or
    ///    collection containing the media described by the current
    ///    instance or an empty array if no value is present.
    /// </value>
    /// <remarks>
    ///    <para>This field is typically optional but aids in the
    ///    sorting of compilations or albums with multiple artists.
    ///    For example, if an album has several artists, sorting by
    ///    artist will split up the album and sorting by album will
    ///    split up albums by the same artist. Having a single album
    ///    artist for an entire album will solve this
    ///    problem.</para>
    ///    <para>As this value is to be used as a sorting key, it
    ///    should be used with less variation than <see cref="P:TagLib.Tag.Performers" />. Where performers can be broken into
    ///    muliple artist it is best to stick with a single band
    ///    name. For example, "The Beatles".</para>
    /// </remarks>
    public string[] AlbumArtists
    {
        get; init;
    }

    /// <summary>
    ///    Gets and sets the sort names for the band or artist who
    ///    is credited in the creation of the entire album or
    ///    collection containing the media described by the
    ///    current instance.
    /// </summary>
    /// <value>
    ///    A <see cref="T:string[]" /> containing the sort names
    ///    for the band or artist who is credited in the creation
    ///    of the entire album or collection containing the media
    ///    described by the current instance or an empty array if
    ///    no value is present.
    /// </value>
    /// <remarks>
    ///    <para>This field is typically optional but aids in the
    ///    sorting of compilations or albums with multiple artists.
    ///    For example, if an album has several artists, sorting by
    ///    artist will split up the album and sorting by album will
    ///    split up albums by the same artist. Having a single album
    ///    artist for an entire album will solve this
    ///    problem.</para>
    ///    <para>As this value is to be used as a sorting key, it
    ///    should be used with less variation than <see cref="P:TagLib.Tag.Performers" />. Where performers can be broken into
    ///    muliple artist it is best to stick with a single band
    ///    name. For example, "Beatles, The".</para>
    /// </remarks>
    public string[] AlbumArtistsSort
    {
        get; init;
    }

    /// <summary>
    ///    Gets and sets the composers of the media represented by
    ///    the current instance.
    /// </summary>
    /// <value>
    ///    A <see cref="T:string[]" /> containing the composers of the
    ///    media represented by the current instance or an empty
    ///    array if no value is present.
    /// </value>
    /// <remarks>
    ///    <para>This field represents the composers, song writers,
    ///    script writers, or persons who claim authorship of the
    ///    media.</para>
    /// </remarks>
    public string[] Composers
    {
        get; init;
    }

    /// <summary>
    ///    Gets and sets the sort names for the composers of the
    ///    media represented by the current instance.
    /// </summary>
    /// <value>
    ///    A <see cref="T:string[]" /> containing the sort names
    ///    for the composers of the media represented by the
    ///    current instance or an empty array if no value is present.
    /// </value>
    /// <remarks>
    ///    <para>This field is typically optional but aids in the
    ///    sorting of compilations or albums with multiple Composers.
    ///    </para>
    ///    <para>As this value is to be used as a sorting key, it
    ///    should be used with less variation than <see cref="P:TagLib.Tag.Composers" />. Where performers can be broken into
    ///    muliple artist it is best to stick with a single composer.
    ///    For example, "McCartney, Paul".</para>
    /// </remarks>
    public string[] ComposersSort
    {
        get; init;
    }

    /// <summary>
    ///    Gets and sets the album of the media represented by the
    ///    current instance. For a video media, this represent the
    ///    collection the video belongs to.
    /// </summary>
    /// <value>
    ///    A <see cref="T:System.String" /> object containing the album of
    ///    the media represented by the current instance or <see langword="null" /> if no value is present.
    /// </value>
    /// <remarks>
    ///    <para>This field represents the name of the album the
    ///    media belongs to. In the case of a boxed set, it should
    ///    be the name of the entire set rather than the individual
    ///    disc. In case of a Serie, this should be name of the serie,
    ///    rather than the season of a serie.</para>
    ///    <para>For example, "Rubber Soul" (an album by the
    ///    Beatles), "The Sopranos: Complete First Season" (a boxed
    ///    set of TV episodes), "Back To The Future" (a
    ///    serie of movies/sequels), or "Game of Thrones" (a serie
    ///    with several seasons).</para>
    /// </remarks>
    public string Album
    {
        get; init;
    }

    /// <summary>
    ///    Gets and sets the sort names for the Album Title of the
    ///    media represented by the current instance.
    /// </summary>
    /// <value>
    ///    A <see cref="T:System.String" /> containing the sort names
    ///    for the Album Title of the media represented by the
    ///    current instance or an empty array if no value is present.
    /// </value>
    /// <remarks>
    ///    <para>This field is typically optional but aids in the
    ///    sorting of compilations or albums with Similar Titles.
    ///    </para>
    /// </remarks>
    public string AlbumSort
    {
        get; init;
    }

    /// <summary>
    ///    Gets and sets a user comment on the media represented by
    ///    the current instance.
    /// </summary>
    /// <value>
    ///    A <see cref="T:System.String" /> object containing user comments
    ///    on the media represented by the current instance or <see langword="null" /> if no value is present.
    /// </value>
    /// <remarks>
    ///    <para>This field should be used to store user notes and
    ///    comments. There is no constraint on what text can be
    ///    stored here, but it should not contain program
    ///    information.</para>
    ///    <para>Because this field contains notes that the user
    ///    might think of while listening to the media, it may be
    ///    useful for an application to make this field easily
    ///    accessible, perhaps even including it in the main
    ///    interface.</para>
    /// </remarks>
    public string Comment
    {
        get; init;
    }

    /// <summary>
    ///    Gets and sets the genres of the media represented by the
    ///    current instance.
    /// </summary>
    /// <value>
    ///    A <see cref="T:string[]" /> containing the genres of the
    ///    media represented by the current instance or an empty
    ///    array if no value is present.
    /// </value>
    /// <remarks>
    ///    <para>This field represents genres that apply to the song,
    ///    album or video. This is often used for filtering media.
    ///    </para>
    ///    <para>A list of common audio genres as popularized by
    ///    ID3v1, are stored in <see cref="P:TagLib.Genres.Audio" />.
    ///    Additionally, <see cref="P:TagLib.Genres.Video" /> contains video
    ///    genres as used by DivX.</para>
    /// </remarks>
    public string[] Genres
    {
        get; init;
    }

    /// <summary>
    ///    Gets and sets the year that the media represented by the
    ///    current instance was recorded.
    /// </summary>
    /// <value>
    ///    A <see cref="T:System.UInt32" /> containing the year that the media
    ///    represented by the current instance was created or zero
    ///    if no value is present.
    /// </value>
    /// <remarks>
    ///    <para>Years greater than 9999 cannot be stored by most
    ///    tagging formats and will be cleared if a higher value is
    ///    set.</para>
    ///    <para>Some tagging formats store higher precision dates
    ///    which will be truncated when this property is set. Format
    ///    specific implementations are necessary access the higher
    ///    precision values.</para>
    /// </remarks>
    public uint Year
    {
        get; init;
    }


    /// <summary>
    ///    Gets and sets the position of the media represented by
    ///    the current instance in its containing album, or season
    ///    (for series).
    /// </summary>
    /// <value>
    ///    A <see cref="T:System.UInt32" /> containing the position of the
    ///    media represented by the current instance in its
    ///    containing album or zero if not specified.
    /// </value>
    /// <remarks>
    ///    <para>This value should be the same as is listed on the
    ///    album cover and no more than <see cref="P:TagLib.Tag.TrackCount" /> if <see cref="P:TagLib.Tag.TrackCount" /> is non-zero.</para>
    ///    <para>Most tagging formats store this as a string. To
    ///    help sorting, a two-digit zero-filled value is used
    ///    in the resulting tag.</para>
    ///    <para>For a serie, this property represents the episode
    ///    in a season of the serie.
    ///    </para>
    /// </remarks>
    public uint Track
    {
        get; init;
    }

    /// <summary>
    ///    Gets and sets the number of tracks in the album, or the
    ///    number of episodes in a serie, of the media represented
    ///    by the current instance.
    /// </summary>
    /// <value>
    ///    A <see cref="T:System.UInt32" /> containing the number of tracks in
    ///    the album, or the number of episodes in a serie, of the
    ///    media represented by the current instance or zero if not
    ///    specified.
    /// </value>
    /// <remarks>
    ///    <para>If non-zero, this value should be at least equal to
    ///    <see cref="P:TagLib.Tag.Track" />. If <see cref="P:TagLib.Tag.Track" /> is zero,
    ///    this value should also be zero.</para>
    /// </remarks>
    public uint TrackCount
    {
        get; init;
    }

    /// <summary>
    ///    Gets and sets the number of the disc containing the media
    ///    represented by the current instance in the boxed set. For
    ///    a serie, this represents the season number.
    /// </summary>
    /// <value>
    ///    A <see cref="T:System.UInt32" /> containing the number of the disc
    ///    or season of the media represented by the current instance
    ///    in the boxed set.
    /// </value>
    /// <remarks>
    ///    <para>This value should be the same as is number that
    ///    appears on the disc. For example, if the disc is the
    ///    first of three, the value should be <c>1</c>. It should
    ///    be no more than <see cref="P:TagLib.Tag.DiscCount" /> if <see cref="P:TagLib.Tag.DiscCount" /> is non-zero.</para>
    /// </remarks>
    public uint Disc
    {
        get; init;
    }

    /// <summary>
    ///    Gets and sets the number of discs or seasons in the
    ///    boxed set containing the media represented by the
    ///    current instance.
    /// </summary>
    /// <value>
    ///    A <see cref="T:System.UInt32" /> containing the number of discs
    ///    or seasons in the boxed set containing the media
    ///    represented by the current instance or zero if not
    ///    specified.
    /// </value>
    /// <remarks>
    ///    <para>If non-zero, this value should be at least equal to
    ///    <see cref="P:TagLib.Tag.Disc" />. If <see cref="P:TagLib.Tag.Disc" /> is zero,
    ///    this value should also be zero.</para>
    /// </remarks>
    public uint DiscCount
    {
        get; init;
    }

    /// <summary>
    ///    Gets and sets the lyrics or script of the media
    ///    represented by the current instance.
    /// </summary>
    /// <value>
    ///    A <see cref="T:System.String" /> object containing the lyrics or
    ///    script of the media represented by the current instance
    ///    or <see langword="null" /> if no value is present.
    /// </value>
    /// <remarks>
    ///    <para>This field contains a plain text representation of
    ///    the lyrics or scripts with line breaks and whitespace
    ///    being the only formatting marks.</para>
    ///    <para>Some formats support more advances lyrics, like
    ///    synchronized lyrics, but those must be accessed using
    ///    format specific implementations.</para>
    /// </remarks>
    public string Lyrics
    {
        get; init;
    }


    /// <summary>
    ///    Gets and sets the grouping on the album which the media
    ///    in the current instance belongs to.
    /// </summary>
    /// <value>
    ///    A <see cref="T:System.String" /> object containing the grouping on
    ///    the album which the media in the current instance belongs
    ///    to or <see langword="null" /> if no value is present.
    /// </value>
    /// <remarks>
    ///    <para>This field contains a non-physical grouping to
    ///    which the track belongs. In classical music, this could
    ///    be a movement. It could also be parts of a series like
    ///    "Introduction", "Closing Remarks", etc.</para>
    /// </remarks>
    public string Grouping
    {
        get; init;
    }

    /// <summary>
    ///    Gets and sets the number of beats per minute in the audio
    ///    of the media represented by the current instance.
    /// </summary>
    /// <value>
    ///    A <see cref="T:System.UInt32" /> containing the number of beats per
    ///    minute in the audio of the media represented by the
    ///    current instance, or zero if not specified.
    /// </value>
    /// <remarks>
    ///    <para>This field is useful for DJ's who are trying to
    ///    match songs. It should be calculated from the audio or
    ///    pulled from a database.</para>
    /// </remarks>
    public uint BeatsPerMinute
    {
        get; init;
    }

    /// <summary>
    ///    Gets and sets the conductor or director of the media
    ///    represented by the current instance.
    /// </summary>
    /// <value>
    ///    A <see cref="T:System.String" /> object containing the conductor
    ///    or director of the media represented by the current
    ///    instance or <see langword="null" /> if no value present.
    /// </value>
    /// <remarks>
    ///    <para>This field is most useful for organizing classical
    ///    music and movies.</para>
    /// </remarks>
    public string Conductor
    {
        get; init;
    }

    /// <summary>
    ///    Gets and sets the copyright information for the media
    ///    represented by the current instance.
    /// </summary>
    /// <value>
    ///    A <see cref="T:System.String" /> object containing the copyright
    ///    information for the media represented by the current
    ///    instance or <see langword="null" /> if no value present.
    /// </value>
    /// <remarks>
    ///    <para>This field should be used for storing copyright
    ///    information. It may be useful to show this information
    ///    somewhere in the program while the media is
    ///    playing.</para>
    ///    <para>Players should not support editing this field, but
    ///    media creation tools should definitely allow
    ///    modification.</para>
    /// </remarks>
    public string Copyright
    {
        get; init;
    }

    /// <summary>
    ///    Gets and sets the date at which the tag has been written.
    /// </summary>
    /// <value>
    ///    A nullable <see cref="T:System.DateTime" /> object containing the
    ///    date at which the tag has been written, or <see langword="null" /> if no value present.
    /// </value>
    public DateTime? DateTagged
    {
        get; init;
    }

    public DateTime DateImported
    {
        get; init;
    }

    public DateTime LastChanged
    {
        get; init;
    }

    /// <summary>Gets and sets the publisher of the song.</summary>
    /// <value>
    ///    A <see cref="T:System.String" /> value for the publisher
    ///    of the song.
    /// </value>
    public string Publisher
    {
        get; init;
    }

    /// <summary>
    ///    Gets and sets the ISRC (International Standard Recording Code) of the song.
    /// </summary>
    /// <value>
    ///    A <see cref="T:System.String" /> value containing the ISRC of the song.
    /// </value>
    public string ISRC
    {
        get; init;
    }

    public double Duration
    {
        get; init;
    }

    public DateTime LastPlayed
    {
        get; init;
    }
    public long Playcount
    {
        get; init;
    }

    #endregion


    public bool Equals(IAudioItem? other)
    {
        return Id == other?.Id && Service == other.Service;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public bool Equals(LocalTrack other)
    {
        return Id == other.Id;
    }
}