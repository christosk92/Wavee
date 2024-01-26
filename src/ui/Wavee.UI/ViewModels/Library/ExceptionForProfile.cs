using Wavee.UI.Providers;

namespace Wavee.UI.ViewModels.Library;

public record ExceptionForProfile(Exception Error, IWaveeUIAuthenticatedProfile Profile, Action? Retry);