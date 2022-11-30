using System.Diagnostics.CodeAnalysis;
using Eum.UI.Crypto;
using Eum.UI.Crypto.Randomness;

namespace Eum.UI.Users;

public class Kitchen
{
    public Kitchen(string? ingredients = null)
    {
        if (ingredients is { })
        {
            Cook(ingredients);
        }
    }

    private string? Salt { get; set; } = null;
    private string? Soup { get; set; } = null;
    private object RefrigeratorLock { get; } = new object();

    [MemberNotNullWhen(returnValue: true, nameof(Salt), nameof(Soup))]
    public bool HasIngredients => Salt is not null && Soup is not null;

    public string SaltSoup()
    {
        if (!HasIngredients)
        {
            throw new InvalidOperationException("Ingredients are missing.");
        }

        string res;
        lock (RefrigeratorLock)
        {
            res = StringCipher.Decrypt(Soup, Salt);
        }

        Cook(res);

        return res;
    }

    public void Cook(string ingredients)
    {
        lock (RefrigeratorLock)
        {
            ingredients ??= "";

            Salt = RandomString.AlphaNumeric(21, secureRandom: true);
            Soup = StringCipher.Encrypt(ingredients, Salt);
        }
    }

    public void CleanUp()
    {
        lock (RefrigeratorLock)
        {
            Salt = null;
            Soup = null;
        }
    }

    public static string SignMessage(string input, byte[] secret)
    {
        return StringCipher.Encrypt(input,  secret);
    }

    public static string UnsignMessage(string input, byte[] secret)
    {
        return StringCipher.Decrypt(input, secret);
    }
    public static string SignMessage(string input, string secret)
    {
        return StringCipher.Encrypt(input,  secret);
    }

    public static string UnsignMessage(string input, string secret)
    {
        return StringCipher.Decrypt(input, secret);
    }
}