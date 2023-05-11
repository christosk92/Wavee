using System.Text;
using Microsoft.Extensions.Logging;
using Wavee.Helpers;

namespace Wavee.Infrastructure.Live;

public readonly struct LogIO : Traits.LogIO
{
    public string EntrySeparator { get; } = Environment.NewLine;

    private readonly ILogger logger;

    public LogIO(ILogger logger)
    {
        this.logger = logger;
    }


    public Unit Log(
        LogLevel level,
        string message,
        int additionalEntrySeparators = 0,
        bool additionalEntrySeparatorsLogFileOnlyMode = true,
        string callerFilePath = "",
        string callerMemberName = "",
        int callerLineNumber = -1)
    {
        message = Guard.Correct(message);
        var category = string.IsNullOrWhiteSpace(callerFilePath)
            ? ""
            : $"{EnvironmentHelpers.ExtractFileName(callerFilePath)}.{callerMemberName} ({callerLineNumber})";
        var messageBuilder = new StringBuilder();
        messageBuilder.Append(
            $"{DateTime.UtcNow.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff} [{Environment.CurrentManagedThreadId}]\t");
        if (message.Length == 0)
        {
            if (category.Length == 0) // If both empty. It probably never happens though.
            {
                messageBuilder.Append($"{EntrySeparator}");
            }
            else // If only the message is empty.
            {
                messageBuilder.Append($"{category}{EntrySeparator}");
            }
        }
        else
        {
            if (category.Length == 0) // If only the category is empty.
            {
                messageBuilder.Append($"{message}{EntrySeparator}");
            }
            else // If none of them empty.
            {
                messageBuilder.Append($"{category}\t{message}{EntrySeparator}");
            }
        }

        var finalMessage = messageBuilder.ToString();

        for (int i = 0; i < additionalEntrySeparators; i++)
        {
            messageBuilder.Insert(0, EntrySeparator);
        }

        var finalFileMessage = messageBuilder.ToString();
        if (!additionalEntrySeparatorsLogFileOnlyMode)
        {
            finalMessage = finalFileMessage;
        }

        switch (level)
        {
            case LogLevel.Trace:
                logger.LogTrace(finalMessage);
                break;
            case LogLevel.Debug:
                logger.LogDebug(finalMessage);
                break;
            case LogLevel.Information:
                logger.LogInformation(finalMessage);
                break;
            case LogLevel.Warning:
                logger.LogWarning(finalMessage);
                break;
            case LogLevel.Error:
                logger.LogError(finalMessage);
                break;
            case LogLevel.Critical:
                logger.LogCritical(finalMessage);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(level), level, null);
        }

        return unit;
    }
}