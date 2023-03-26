namespace Wavee.UI.Playback.Player;

public interface IMusicPlayer
{
    event EventHandler<PreviousTrackData?> TrackStarted;
    event EventHandler? TrackEnded;
    event EventHandler<bool>? PauseChanged;
    event EventHandler<TimeSpan>? TimeChanged;
    event EventHandler<double>? VolumeChanged;
    void Seek(TimeSpan to);
    void Resume();
    void Pause();
    Task LoadTrack(string file);
    TimeSpan Position
    {
        get;
    }

    bool Paused
    {
        get;
    }

    double Volume
    {
        get;
    }

    void SetVolume(double d);
}
