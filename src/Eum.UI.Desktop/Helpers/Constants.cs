namespace Eum.UI.Helpers;

public static class Constants
{
	public const string AlphaNumericCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
	public const string CapitalAlphaNumericCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

	public const string ExecutableName = "eumui";
	public const string AppName = "Project Eum";
	public const int BigFileReadWriteBufferSize = 1 * 1024 * 1024;

	public static readonly Version ClientVersion = new(2, 0, 2, 0);
}
