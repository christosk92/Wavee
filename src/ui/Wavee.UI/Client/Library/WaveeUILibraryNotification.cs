using LanguageExt;
using Wavee.Id;

namespace Wavee.UI.Client.Library;

public readonly record struct WaveeUILibraryNotification(Seq<WaveeUILibraryItem> Ids, bool Added);

public readonly record struct WaveeUILibraryItem(string Id, AudioItemType Type, ServiceType Source, Option<DateTimeOffset> AddedAt);