using System.Diagnostics.CodeAnalysis;

namespace Wavee.Helpers;

internal static class StringExtensions
{
    /// <summary>
    /// Removes one leading occurrence of the specified string
    /// </summary>
    public static string TrimStart(this string me, string trimString, StringComparison comparisonType)
    {
        if (me.StartsWith(trimString, comparisonType))
        {
            return me[trimString.Length..];
        }
        return me;
    }

    /// <summary>
    /// Removes one trailing occurrence of the specified string
    /// </summary>
    public static string TrimEnd(this string me, string trimString, StringComparison comparisonType)
    {
        if (me.EndsWith(trimString, comparisonType))
        {
            return me.Substring(0, me.Length - trimString.Length);
        }
        return me;
    }

    /// <summary>
    /// Returns true if the string contains leading or trailing whitespace, otherwise returns false.
    /// </summary>
    public static bool IsTrimmable(this string me)
    {
        if (me.Length == 0)
        {
            return false;
        }

        return char.IsWhiteSpace(me[0]) || char.IsWhiteSpace(me[^1]);
    }

    /// <summary>
    /// Corrects the string:
    /// If the string is null, it'll be empty.
    /// Trims the string.
    /// </summary>
    [return: NotNull]
    public static string Correct(this string? str)
    {
        return string.IsNullOrWhiteSpace(str)
            ? ""
            : str.Trim();
    }
    private static void AssertCorrectParameterName(string parameterName)
    {
        if (parameterName is null)
        {
            throw new ArgumentNullException(nameof(parameterName), "Parameter cannot be null.");
        }

        if (parameterName.Length == 0)
        {
            throw new ArgumentException("Parameter cannot be empty.", nameof(parameterName));
        }

        if (parameterName.Trim().Length == 0)
        {
            throw new ArgumentException("Parameter cannot be whitespace.", nameof(parameterName));
        }
    }

    [return: NotNull]
    public static T NotNull<T>(string parameterName, T value)
    {
        AssertCorrectParameterName(parameterName);
        return value ?? throw new ArgumentNullException(parameterName, "Parameter cannot be null.");
    }
    public static IEnumerable<T> NotNullOrEmpty<T>(string parameterName, IEnumerable<T> value)
    {
        NotNull(parameterName, value);

        if (!value.Any())
        {
            throw new ArgumentException("Parameter cannot be empty.", parameterName);
        }

        return value;
    }

    public static string NotNullOrEmptyOrWhitespace(string parameterName, string value, bool trim = false)
    {
        NotNullOrEmpty(parameterName, value);

        string trimmedValue = value.Trim();
        if (trimmedValue.Length == 0)
        {
            throw new ArgumentException("Parameter cannot be whitespace.", parameterName);
        }

        if (trim)
        {
            return trimmedValue;
        }
        else
        {
            return value;
        }
    }

}