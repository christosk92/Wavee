namespace Eum.UI.Validation;

public interface IValidations
{
    bool Any { get; }

    bool AnyErrors { get; }

    bool AnyWarnings { get; }

    bool AnyInfos { get; }

    IEnumerable<string> Infos { get; }

    IEnumerable<string> Warnings { get; }

    IEnumerable<string> Errors { get; }
}