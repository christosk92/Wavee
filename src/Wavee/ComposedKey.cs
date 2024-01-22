namespace Wavee;

public class ComposedKey : ValueObject
{
    public ComposedKey(params object[] keys)
    {
        Keys = keys;
    }
    private ComposedKey(List<object> keys)
    {
        Keys = keys.ToArray();
    }

    public object[] Keys { get; }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        return Keys;
    }

    public static ComposedKey FromKeys(List<object> keys)
    {
        return new ComposedKey(keys);
    }
}