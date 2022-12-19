namespace Eum.UI.ViewModels.Search.Patterns;

public class ComposedKey : ValueObject
{
	public ComposedKey(params object[] keys)
	{
		Keys = keys;
	}

	public object[] Keys { get; }

	protected override IEnumerable<object> GetEqualityComponents()
	{
		return Keys;
	}
}
