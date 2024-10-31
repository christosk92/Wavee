using System.Text.RegularExpressions;

namespace Wavee.Services.Playback.Remote;

internal sealed partial class SpotifyWebsocketRouter
{
    private readonly List<RouteEntry> _routes = new();

    // Internal class to hold route information
    private class RouteEntry
    {
        public Regex Regex { get; }
        public MessageHandler? MessageHandler { get; }
        public RequestHandler? RequestHandler { get; }

        public List<string> ParameterNames { get; }

        public RouteEntry(Regex regex, MessageHandler handler, List<string> parameterNames)
        {
            Regex = regex;
            MessageHandler = handler;
            ParameterNames = parameterNames;
        }

        public RouteEntry(Regex regex, RequestHandler handler, List<string> parameterNames)
        {
            Regex = regex;
            RequestHandler = handler;
            ParameterNames = parameterNames;
        }
    }

    // Method to add a message handler with a URI pattern
    public void AddMessageHandler(string uriPattern, MessageHandler handler)
    {
        var (regex, parameterNames) = CreateRegexFromPattern(uriPattern);
        _routes.Add(new RouteEntry(regex, handler, parameterNames));
    }

    public void AddRequestHandler(string path, RequestHandler handler)
    {
        var (regex, parameterNames) = CreateRegexFromPattern(path);
        _routes.Add(new RouteEntry(regex, handler, parameterNames));
    }

    // Method to route an incoming message
    public async Task RouteMessageAsync(SpotifyWebsocketMessage message, CancellationToken cancellationToken)
    {
        foreach (var route in _routes)
        {
            var match = route.Regex.Match(message.Uri);
            if (match.Success)
            {
                var parameters = new Dictionary<string, string>();
                for (int i = 0; i < route.ParameterNames.Count; i++)
                {
                    var value = Uri.UnescapeDataString(match.Groups[i + 1].Value);
                    parameters[route.ParameterNames[i]] = value;
                }

                await route.MessageHandler!(message, parameters, cancellationToken);
                return; // Handler found and invoked
            }
        }
    }

    public async Task RouteRequestAsync(SpotifyWebsocketMessage message, Reply reply, CancellationToken ct)
    {
        foreach (var route in _routes)
        {
            var match = route.Regex.Match(message.Uri);
            if (match.Success)
            {
                var parameters = new Dictionary<string, string>();
                for (int i = 0; i < route.ParameterNames.Count; i++)
                {
                    var value = Uri.UnescapeDataString(match.Groups[i + 1].Value);
                    parameters[route.ParameterNames[i]] = value;
                }

                await route.RequestHandler!(message, parameters, reply, ct);
                return; // Handler found and invoked
            }
        }
    }

    public static (Regex regex, List<string> parameterNames) CreateRegexFromPattern(string pattern)
    {
        // Escape special regex characters except for braces
        var escapedPattern = MyRegex().Replace(pattern, @"\$1"); // Exclude {} from escaping

        // Replace wildcard '*' with '.*' for matching any characters
        escapedPattern = escapedPattern.Replace("*", ".*");

        // Find parameter placeholders
        var parameterMatches = MyRegex1().Matches(pattern);
        var parameterNames = parameterMatches.Cast<Match>()
            .Select(m => m.Groups[1].Value)
            .ToList();

        // Replace parameter placeholders with regex capturing groups
        foreach (var parameter in parameterNames)
        {
            // Replace {paramName} with a capturing group
            escapedPattern = escapedPattern.Replace($"{{{parameter}}}", "([^/]+)");
        }

        // Return the final regex pattern and parameter names
        var regex = new Regex($"^{escapedPattern}$", RegexOptions.Compiled);
        return (regex, parameterNames);
    }


    public delegate Task MessageHandler(
        SpotifyWebsocketMessage message,
        IDictionary<string, string> parameters,
        CancellationToken cancellationToken);

    public delegate Task RequestHandler(
        SpotifyWebsocketMessage message,
        IDictionary<string, string> parameters,
        Reply reply,
        CancellationToken cancellationToken);

    public delegate Task Reply(string key, bool success);

    [GeneratedRegex(@"([.^$+?()[\]|])")]
    private static partial Regex MyRegex();

    [GeneratedRegex(@"\{([^}]+)\}")]
    private static partial Regex MyRegex1();

    public void RemoveMessageHandler(MessageHandler handler)
    {
        _routes.RemoveAll(r => r.MessageHandler == handler);
    }
}