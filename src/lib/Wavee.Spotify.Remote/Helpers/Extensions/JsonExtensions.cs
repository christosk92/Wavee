using System.Text.Json;
using LanguageExt.Common;

namespace Wavee.Spotify.Remote.Helpers.Extensions;

internal static class JsonExtensions
{
    public static Eff<JsonElement> GetRequiredProperty(this JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var property)
            ? SuccessEff(property)
            : FailEff<JsonElement>(Error.New($"Property {propertyName} not found"));
}