namespace Eum.UI.Stage;

public interface IStage
{
    int StageIndex { get; }
    (IStage? Stage, object? Result) NextStage();

    string Title { get; }
    string Description { get; }
    bool CanGoNext { get; }
}