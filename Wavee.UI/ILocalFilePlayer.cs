namespace Wavee.UI;

public interface ILocalFilePlayer
{
    event EventHandler<string> TrackStarted;
    event EventHandler? TrackEnded;
    event EventHandler<bool>? PauseChanged;
    event EventHandler<TimeSpan>? TimeChanged;
    void Seek(TimeSpan to);
    void Resume();
    void Pause();
    Task LoadTrack(string file);
    TimeSpan Position
    {
        get;
    }
}
