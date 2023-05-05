using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Eum.Spotify;
using Eum.Spotify.connectstate;
using Google.Protobuf;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using NAudio.Utils;
using NAudio.Wave;
using ReactiveUI;
using Splat;
using Wavee.Infrastructure.Live;
using Wavee.Infrastructure.Sys.IO;
using Wavee.Spotify;
using Wavee.Spotify.Clients.Mercury;
using Wavee.Spotify.Common;
using Wavee.Spotify.Exceptions;
using Wavee.Spotify.Infrastructure.Sys;
using Wavee.Spotify.Playback;
using Wavee.Spotify.Playback.Infrastructure.Streams;
using Wavee.Spotify.Playback.Infrastructure.Sys;
using Wavee.VorbisDecoder.Convenience;
using static LanguageExt.Prelude;
using Unit = System.Reactive.Unit;

namespace Wavee.UI.WinUI.PlaybackSample;

public sealed class MainViewModel : ReactiveObject
{
    private readonly ISpotifyClient _spotifyClient;
    private bool _isSignedIn;
    private string _username;
    private string _password;
    private string _errorMessage;
    private string _countryCode;
    private string _searchTerm;
    private int _positionMs;
    private bool _isPaused;


    private readonly ObservableAsPropertyHelper<IEnumerable<TrackViewModel>> _searchResults;
    public MainViewModel()
    {
        _spotifyClient = SpotifyRuntime.Create();

        _spotifyClient
            .WelcomeMessageChanged
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(x =>
            {
                IsSignedIn = x.IsSome;
                Username = x.Match(
                    Some: u => u.CanonicalUsername,
                    None: () => ""
                );
            });

        _spotifyClient.CountryCodeChanged
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(x =>
            {
                CountryCode = x.IfNone("");
            });

        // us to have the latest results that we can expose through the property to the View.
        _searchResults = this
            .WhenAnyValue(x => x.SearchTerm)
            .Throttle(TimeSpan.FromMilliseconds(100))
            .Select(term => term?.Trim())
            .DistinctUntilChanged()
            .Where(term => !string.IsNullOrWhiteSpace(term))
            .SelectMany(SearchTracks)
            .ObserveOn(RxApp.MainThreadScheduler)
            .ToProperty(this, x => x.SearchResults);


        _searchResults.ThrownExceptions.Subscribe(error =>
        {
            /* Handle errors here */
        });

        _isAvailable = this
            .WhenAnyValue(x => x.SearchResults)
            .Select(searchResults => searchResults != null)
            .ToProperty(this, x => x.IsAvailable);

        SignInCommand = ReactiveCommand.CreateFromTask<string>(SignIn);
    }
    public IEnumerable<TrackViewModel> SearchResults => _searchResults.Value;
    private readonly ObservableAsPropertyHelper<bool> _isAvailable;
    public bool IsAvailable => _isAvailable.Value;

    public string SearchTerm
    {
        get => _searchTerm;
        set => this.RaiseAndSetIfChanged(ref _searchTerm, value);
    }

    public int PositionMs
    {
        get => (int)_positionMs;
        set => this.RaiseAndSetIfChanged(ref _positionMs, (int)value);
    }

    public bool IsPaused
    {
        get => _isPaused;
        set => this.RaiseAndSetIfChanged(ref _isPaused, value);
    }
    public TrackOrEpisode? CurrentItem
    {
        get => _currentItem;
        set => this.RaiseAndSetIfChanged(ref _currentItem, value);
    }
    public string? CountryCode
    {
        get => _countryCode;
        set => this.RaiseAndSetIfChanged(ref _countryCode, value);
    }
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }
    public bool IsSignedIn
    {
        get => _isSignedIn;
        set => this.RaiseAndSetIfChanged(ref _isSignedIn, value);
    }

    public string Username
    {
        get => _username;
        set => this.RaiseAndSetIfChanged(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => this.RaiseAndSetIfChanged(ref _password, value);
    }

    public ICommand SignInCommand { get; }

    public async Task<Unit> SignIn(string password)
    {
        try
        {
            ErrorMessage = null;
            _ = await SpotifyRuntime.Authenticate(Some(_spotifyClient), new LoginCredentials
            {
                Username = Username,
                AuthData = ByteString.CopyFromUtf8(password),
                Typ = AuthenticationType.AuthenticationUserPass
            });

            return Unit.Default;
        }
        catch (SpotifyAuthenticationException authenticationException)
        {
            ErrorMessage = authenticationException.ErrorCode.ErrorCode.ToString();
            return Unit.Default;
        }
    }

    private async Task<IEnumerable<TrackViewModel>> SearchTracks(
        string term, CancellationToken token)
    {
        //dummy, just return 10 items with term + index
        // return Enumerable.Range(0, 10)
        //     .Select(i => new TrackViewModel
        //     {
        //         Title = $"{term} {i}",
        //         Artist = $"{term} {i}",
        //         Album = $"{term} {i}",
        //         Image = $"{term} {i}"
        //     });

        var searchResults = await _spotifyClient.Mercury.Search(term, "track", 0, 10, token);
        return searchResults.Categories.First(c => c.Category is "tracks").Hits.Cast<TrackSearchHit>().Select(track => new TrackViewModel
        {
            Id = track.Id,
            Title = track.Name,
            Artist = track.Artists[0].Name,
            Album = track.Album.Name,
            Image = track.Image
        });
    }

    public async Task SelectedItem(TrackViewModel track)
    {
        //start playback
        var spotifyStream = await _spotifyClient.StreamAudio(track.Id,
            new SpotifyPlaybackConfig(
                DeviceName: "Wavee",
                DeviceType.Computer,
                PreferredQualityType.High,
                InitialVolume: ushort.MaxValue / 2
            ));

        //playback
        _ = StartPlayback(spotifyStream);
    }

    private async Task StartPlayback(ISpotifyStream spotifyStream)
    {

        try
        {
            if (_cancellationTokenSource.IsSome)
            {
                await _cancellationTokenSource.ValueUnsafe().CancelAsync();
            }
        }
        catch
        {

        }
        //   _ = Try(() => _waveReader.IfSome(x => x.Dispose()))();

        var waveReader = new VorbisWaveReader(spotifyStream.AsStream());
        _waveReader = Some(waveReader);

        _waveOutEvent.Init(waveReader);
        _waveOutEvent.Play();
        CurrentItem = spotifyStream.Metadata;

        var cts = new CancellationTokenSource();
        _cancellationTokenSource = Some(cts);
        try
        {
            while (_waveOutEvent.PlaybackState != PlaybackState.Stopped && !cts.IsCancellationRequested)
            {
                if (_waveOutEvent.PlaybackState is PlaybackState.Paused)
                {
                    IsPaused = true;
                }
                else
                {
                    IsPaused = false;
                }
                PositionMs = (int)_waveOutEvent.GetPositionTimeSpan().TotalMilliseconds;
                await Task.Delay(100, cts.Token);
            }
        }
        catch (Exception x)
        {
            //ignore
            if (x is OperationCanceledException)
            {
                cts.Dispose();
            }
        }
        finally
        {
            _waveOutEvent.Stop();
            waveReader.Dispose();
        }
    }

    private Option<CancellationTokenSource> _cancellationTokenSource = None;
    private readonly WaveOutEvent _waveOutEvent = new();
    private Option<VorbisWaveReader> _waveReader = None;
    private TrackOrEpisode? _currentItem;

    public void Cleanup()
    {

        try
        {
            if (_cancellationTokenSource.IsSome)
            {
                _cancellationTokenSource.ValueUnsafe().Cancel();
            }
        }
        catch
        {
        }

        try
        {
            _waveOutEvent.Dispose();
        }
        catch
        {

        }

        try
        {
            if (_waveReader.IsSome)
            {
                _waveReader.ValueUnsafe().Dispose();
            }
        }
        catch
        {

        }
    }
}

public sealed class TrackViewModel
{
    public required SpotifyId Id { get; init; }
    public required string Title { get; init; }
    public required string Artist { get; init; }
    public required string Image { get; init; }
    public required string Album { get; init; }
}