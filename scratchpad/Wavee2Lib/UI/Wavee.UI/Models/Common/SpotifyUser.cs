using LanguageExt;

namespace Wavee.UI.Models.Common;

public record SpotifyUser(string Id, string DisplayName, Option<string> ImageUrl);