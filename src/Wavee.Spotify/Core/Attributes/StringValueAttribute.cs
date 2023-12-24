using System.Reflection;

namespace Wavee.Spotify.Core.Attributes;

internal static class StringValueExtensions
{
    public static string? ToStringValue(this Enum enumv)
    {
        // Get the type of the enum
        Type type = enumv.GetType();

        // Get the field info for this enum value
        FieldInfo fieldInfo = type.GetField(enumv.ToString());

        // Get the StringValue attributes
        StringValueAttribute[] attributes = fieldInfo.GetCustomAttributes(typeof(StringValueAttribute), false) as StringValueAttribute[];

        // Return the first if there was a match.
        return attributes.Length > 0 ? attributes[0].Text : null;
    }
}


/// <summary>Defines an attribute containing a string representation of the member.</summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
internal class StringValueAttribute : Attribute
{
    private readonly string text;
    /// <summary>The text which belongs to this member.</summary>
    public string Text { get { return text; } }

    /// <summary>Creates a new string value attribute with the specified text.</summary>
    public StringValueAttribute(string text)
    {
        this.text = text;
    }
}