using System.Text.Json;
using System.Text.Json.Serialization;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.UI.Infrastructure.Sys.IO;
using Wavee.UI.Infrastructure.Traits;

namespace Wavee.UI.Infrastructure.Sys;

public static class UserManagment<RT> where RT : struct, HasFile<RT>, HasDirectory<RT>, HasLocalPath<RT>
{
    //semaphore for file access
    private static readonly SemaphoreSlim FileLock = new(1, 1);

    private static Eff<Unit> Lock() =>
        Eff(() =>
        {
            FileLock.Wait();
            return unit;
        });

    private static Eff<Unit> Release() =>
        Eff(() =>
        {
            FileLock.Release();
            return unit;
        });

    public static Eff<RT, string> UsersPath =>
        from appData in Local<RT>.localDir
        let path = Path.Combine(appData, "users")
        from _ in Directory<RT>.create(path)
        select path;

    public static Aff<RT, User> GetUser(string userId) =>
        from usersPath in UsersPath
        let file = Path.Combine(usersPath, userId)
        from user in Deserialize(file)
        select user;

    public static Aff<RT, Unit> CreateOrOverwriteUser(User user) =>
        from usersPath in UsersPath
        let file = Path.Combine(usersPath, user.Id)
        from _ in SerializeAndWrite(file, user)
        select unit;

    public static Aff<RT, Unit> SetDisplayName(string userId, Option<string> displayName) =>
        from user in GetUser(userId)
        from newUser in SuccessEff(user.SetDisplayName(displayName))
        from serialized in CreateOrOverwriteUser(newUser)
        select unit;

    public static Aff<RT, Unit> SetImageId(string userId, Option<string> imageId) =>
        from user in GetUser(userId)
        from newUser in SuccessEff(user with { ImageId = imageId })
        from serialized in CreateOrOverwriteUser(newUser)
        select unit;

    public static Aff<RT, Unit> AddKeyToMetadata(string userId, string key, string value) =>
        from user in GetUser(userId)
        from newUser in SuccessEff(user with { Metadata = user.Metadata.AddOrUpdate(key, value) })
        from serialized in CreateOrOverwriteUser(newUser)
        select unit;

    public static Aff<RT, Unit> RemoveKeyFromMetadata(string userId, string key) =>
        from user in GetUser(userId)
        from newUser in SuccessEff(user with { Metadata = user.Metadata.Remove(key) })
        from serialized in CreateOrOverwriteUser(newUser)
        select unit;

    public static Aff<RT, Option<User>> GetDefaultUser() =>
        from usersPath in UsersPath
        from usersEff in Directory<RT>.enumerateFiles(usersPath)
            .Map(x => x.Map(Deserialize).Sequence())
        from users in usersEff
        let user = users.Find(x => x.IsDefault)
        select user;

    public static Aff<RT, Unit> SetDefaultUser(string userId) =>
        from usersPath in UsersPath
        from users in Directory<RT>.enumerateFiles(usersPath)
        from _ in users.Map(file => Deserialize(file).Map(user => user with { IsDefault = user.Id == userId }))
            .Sequence()
            .Map(f => f.Map(x => SerializeAndWrite(x.Id, x)))
        select unit;

    private static Aff<RT, Unit> SerializeAndWrite(string file, User str) =>
        from bytes in Serialize(str)
        from _ in Lock()
        from __ in File<RT>.writeAllBytes(file, bytes)
        from ___ in Release()
        select unit;

    private static Eff<RT, byte[]> Serialize(User str) =>
        Eff(() => JsonSerializer.SerializeToUtf8Bytes(str));

    private static Eff<RT, User> Deserialize(string file) =>
        from _ in Lock()
        from bytes in File<RT>.readAllBytesSync(file)
        from __ in Release()
        select JsonSerializer.Deserialize<User>(bytes.Span);
}

public readonly record struct User(string Id,
    bool IsDefault,
    [property: JsonConverter(typeof(JsonOptionStringConverter))]
    Option<string> DisplayName,
    [property: JsonConverter(typeof(JsonOptionStringConverter))]
    Option<string> ImageId,
    [property: JsonConverter(typeof(JsonHashmapConverter))]
    HashMap<string, string> Metadata)
{
    public User SetDisplayName(Option<string> displayName)
    {
        return this with
        {
            DisplayName = displayName
        };
    }
}

internal sealed class JsonOptionStringConverter : JsonConverter<Option<string>>
{
    public override Option<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();
        return str == null ? Option<string>.None : Option<string>.Some(str);
    }

    public override void Write(Utf8JsonWriter writer, Option<string> value, JsonSerializerOptions options)
    {
        if (value.IsSome)
        {
            writer.WriteStringValue(value.ValueUnsafe());
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

internal sealed class JsonHashmapConverter : JsonConverter<HashMap<string, string>>
{
    public override HashMap<string, string> Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var result = new HashMap<string, string>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var key = reader.GetString();
                reader.Read();
                var value = reader.GetString();
                result = result.AddOrUpdate(key, value);
            }
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, HashMap<string, string> value, JsonSerializerOptions options)
    {
        //just a dictionary
        //write key value pairs
        writer.WriteStartObject();
        foreach (var (key, val) in value)
        {
            writer.WriteString(key, val);
        }

        writer.WriteEndObject();
    }
}