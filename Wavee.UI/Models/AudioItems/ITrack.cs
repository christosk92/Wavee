namespace Wavee.UI.Models.AudioItems;

public interface ITrack
{
    string Name
    {
        get;
    }
    string Album
    {
        get;
    }
    IDescriptionaryItem[] Artists
    {
        get;
    }
    string ImagePath
    {
        get;
    }
    public double Duration
    {
        get;
    }
}