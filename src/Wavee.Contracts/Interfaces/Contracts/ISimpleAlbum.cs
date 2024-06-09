namespace Wavee.Contracts.Interfaces.Contracts;

public interface ISimpleAlbum : IItem
{
    IContributor Contributor { get; }
}