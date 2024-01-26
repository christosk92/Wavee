using System;

namespace Wavee.UI.WinUI.Navigation;

public record CachedPageRecord(Type Type, object? Value, DateTimeOffset CachedAt, CachingPolicy Policy)
{
    public object? Value { get; set; } = Value;
}