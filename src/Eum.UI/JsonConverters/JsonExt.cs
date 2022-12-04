using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections;
using System.Reflection;

namespace Eum.UI.JsonConverters;

public static class JsonExt
{  
 
    public static void PopulateObject<T>(this string jsonSource, T target, JsonSerializerOptions opts)
    {
        //deserialize to instance 1)
        //next we enumerate over the fields of target and set each field
        var targetType = target.GetType();
        if (jsonSource == "{}")
        {
            //Empty json string
            //so a default
            return;
        }
        var dummy = JsonSerializer.Deserialize(jsonSource, targetType, opts);

        if (dummy != null)
        {
            foreach (PropertyInfo property in targetType.GetProperties().Where(p => p.CanWrite))
            {
                property.SetValue(target, property.GetValue(dummy, null), null);
            }
        }
    }

    public static void OverwriteProperty(object target, JsonProperty updatedProperty, Type type,
        JsonSerializerOptions opts)
    {
        var propertyInfo = type.GetProperty(updatedProperty.Name);

        if (propertyInfo == null)
        {
            return;
        }

        var propertyType = propertyInfo.PropertyType;
        object parsedValue;

        if (propertyType.IsValueType || propertyType == typeof(string))
        {
            parsedValue = JsonSerializer.Deserialize(
                updatedProperty.Value.GetRawText(),
                propertyType, opts)!;
        }
        else
        {
            parsedValue = propertyInfo.GetValue(target);
            PopulateObject(
                updatedProperty.Value.GetRawText(),
                parsedValue, opts);
        }

        if (propertyInfo.GetSetMethod() != null)
        {

            propertyInfo.SetValue(target, parsedValue);
        }
    }
}

// public static class JsonExt
// {
//     public static void PopulateObject(this string json, object obj, JsonSerializerOptions options)
//     {
//         var populator = new JsonPopulator();
//         populator.PopulateObject(obj, json, options);
//     }
// }
// public class JsonPopulator
// {
//     public void PopulateObject(object obj, string jsonString, JsonSerializerOptions options = null) => PopulateObject(obj, jsonString != null ? Encoding.UTF8.GetBytes(jsonString) : null, options);
//     public virtual void PopulateObject(object obj, ReadOnlySpan<byte> jsonData, JsonSerializerOptions options = null)
//     {
//         options ??= new JsonSerializerOptions();
//         var state = new JsonReaderState(new JsonReaderOptions { AllowTrailingCommas = options.AllowTrailingCommas, CommentHandling = options.ReadCommentHandling, MaxDepth = options.MaxDepth });
//         var reader = new Utf8JsonReader(jsonData, isFinalBlock: true, state);
//         new Worker(this, reader, obj, options);
//     }
//
//     protected virtual PropertyInfo GetProperty(ref Utf8JsonReader reader, JsonSerializerOptions options, object obj, string propertyName)
//     {
//         if (obj == null)
//             throw new ArgumentNullException(nameof(obj));
//
//         if (propertyName == null)
//             throw new ArgumentNullException(nameof(propertyName));
//
//         var prop = obj.GetType().GetProperty(propertyName);
//         return prop;
//     }
//
//     protected virtual bool SetPropertyValue(ref Utf8JsonReader reader, JsonSerializerOptions options, object obj, string propertyName)
//     {
//         if (obj == null)
//             throw new ArgumentNullException(nameof(obj));
//
//         if (propertyName == null)
//             throw new ArgumentNullException(nameof(propertyName));
//
//         var prop = GetProperty(ref reader, options, obj, propertyName);
//         if (prop == null)
//             return false;
//
//         if (!TryReadPropertyValue(ref reader, options, prop.PropertyType, out var value))
//             return false;
//
//         prop.SetValue(obj, value);
//         return true;
//     }
//
//     protected virtual bool TryReadPropertyValue(ref Utf8JsonReader reader, JsonSerializerOptions options, Type propertyType, out object value)
//     {
//         if (propertyType == null)
//             throw new ArgumentNullException(nameof(reader));
//
//         if (reader.TokenType == JsonTokenType.Null)
//         {
//             value = null;
//             return !propertyType.IsValueType || Nullable.GetUnderlyingType(propertyType) != null;
//         }
//
//         if (propertyType == typeof(object)) { value = ReadValue(ref reader); return true; }
//         if (propertyType == typeof(string)) { value = JsonSerializer.Deserialize<JsonElement>(ref reader, options).GetString(); return true; }
//         if (propertyType == typeof(int) && reader.TryGetInt32(out var i32)) { value = i32; return true; }
//         if (propertyType == typeof(long) && reader.TryGetInt64(out var i64)) { value = i64; return true; }
//         if (propertyType == typeof(DateTime) && reader.TryGetDateTime(out var dt)) { value = dt; return true; }
//         if (propertyType == typeof(DateTimeOffset) && reader.TryGetDateTimeOffset(out var dto)) { value = dto; return true; }
//         if (propertyType == typeof(Guid) && reader.TryGetGuid(out var guid)) { value = guid; return true; }
//         if (propertyType == typeof(decimal) && reader.TryGetDecimal(out var dec)) { value = dec; return true; }
//         if (propertyType == typeof(double) && reader.TryGetDouble(out var dbl)) { value = dbl; return true; }
//         if (propertyType == typeof(float) && reader.TryGetSingle(out var sgl)) { value = sgl; return true; }
//         if (propertyType == typeof(uint) && reader.TryGetUInt32(out var ui32)) { value = ui32; return true; }
//         if (propertyType == typeof(ulong) && reader.TryGetUInt64(out var ui64)) { value = ui64; return true; }
//         if (propertyType == typeof(byte[]) && reader.TryGetBytesFromBase64(out var bytes)) { value = bytes; return true; }
//
//         if (propertyType == typeof(bool))
//         {
//             if (reader.TokenType == JsonTokenType.False || reader.TokenType == JsonTokenType.True)
//             {
//                 value = reader.GetBoolean();
//                 return true;
//             }
//         }
//
//         // fallback here
//         return TryConvertValue(ref reader, propertyType, out value);
//     }
//
//     protected virtual object ReadValue(ref Utf8JsonReader reader)
//     {
//         switch (reader.TokenType)
//         {
//             case JsonTokenType.False: return false;
//             case JsonTokenType.True: return true;
//             case JsonTokenType.Null: return null;
//             case JsonTokenType.String: return reader.GetString();
//
//             case JsonTokenType.Number: // is there a better way?
//                 if (reader.TryGetInt32(out var i32))
//                     return i32;
//
//                 if (reader.TryGetInt64(out var i64))
//                     return i64;
//
//                 if (reader.TryGetUInt64(out var ui64)) // uint is already handled by i64
//                     return ui64;
//
//                 if (reader.TryGetSingle(out var sgl))
//                     return sgl;
//
//                 if (reader.TryGetDouble(out var dbl))
//                     return dbl;
//
//                 if (reader.TryGetDecimal(out var dec))
//                     return dec;
//
//                 break;
//         }
//         throw new NotSupportedException();
//     }
//
//     // we're here when json types & property types don't match exactly
//     protected virtual bool TryConvertValue(ref Utf8JsonReader reader, Type propertyType, out object value)
//     {
//         if (propertyType == null)
//             throw new ArgumentNullException(nameof(reader));
//
//         if (propertyType == typeof(bool))
//         {
//             if (reader.TryGetInt64(out var i64)) // one size fits all
//             {
//                 value = i64 != 0;
//                 return true;
//             }
//         }
//
//         // TODO: add other conversions
//
//         value = null;
//         return false;
//     }
//
//     protected virtual object CreateInstance(ref Utf8JsonReader reader, Type propertyType)
//     {
//         if (propertyType.GetConstructor(Type.EmptyTypes) == null)
//             return null;
//
//         // TODO: handle custom instance creation
//         try
//         {
//             return Activator.CreateInstance(propertyType);
//         }
//         catch
//         {
//             // swallow
//             return null;
//         }
//     }
//
//     private class Worker
//     {
//         private readonly Stack<WorkerProperty> _properties = new Stack<WorkerProperty>();
//         private readonly Stack<object> _objects = new Stack<object>();
//
//         public Worker(JsonPopulator populator, Utf8JsonReader reader, object obj, JsonSerializerOptions options)
//         {
//             _objects.Push(obj);
//             WorkerProperty prop;
//             WorkerProperty peek;
//             while (reader.Read())
//             {
//                 switch (reader.TokenType)
//                 {
//                     case JsonTokenType.PropertyName:
//                         prop = new WorkerProperty();
//                         prop.PropertyName = Encoding.UTF8.GetString(reader.ValueSpan);
//                         _properties.Push(prop);
//                         break;
//
//                     case JsonTokenType.StartObject:
//                     case JsonTokenType.StartArray:
//                         if (_properties.Count > 0)
//                         {
//                             object child = null;
//                             var parent = _objects.Peek();
//                             PropertyInfo pi = null;
//                             if (parent != null)
//                             {
//                                 pi = populator.GetProperty(ref reader, options, parent, _properties.Peek().PropertyName);
//                                 if (pi != null)
//                                 {
//                                     child = pi.GetValue(parent); // mimic ObjectCreationHandling.Auto
//                                     if (child == null && pi.CanWrite)
//                                     {
//                                         if (reader.TokenType == JsonTokenType.StartArray)
//                                         {
//                                             if (!typeof(IList).IsAssignableFrom(pi.PropertyType))
//                                                 break;  // don't create if we can't handle it
//                                         }
//
//                                         if (reader.TokenType == JsonTokenType.StartArray && pi.PropertyType.IsArray)
//                                         {
//                                             child = Activator.CreateInstance(typeof(List<>).MakeGenericType(pi.PropertyType.GetElementType())); // we can't add to arrays...
//                                         }
//                                         else
//                                         {
//                                             child = populator.CreateInstance(ref reader, pi.PropertyType);
//                                             if (child != null)
//                                             {
//                                                 pi.SetValue(parent, child);
//                                             }
//                                         }
//                                     }
//                                 }
//                             }
//
//                             if (reader.TokenType == JsonTokenType.StartObject)
//                             {
//                                 _objects.Push(child);
//                             }
//                             else if (child != null) // StartArray
//                             {
//                                 peek = _properties.Peek();
//                                 peek.IsArray = pi.PropertyType.IsArray;
//                                 peek.List = (IList)child;
//                                 peek.ListPropertyType = GetListElementType(child.GetType());
//                                 peek.ArrayPropertyInfo = pi;
//                             }
//                         }
//                         break;
//
//                     case JsonTokenType.EndObject:
//                         _objects.Pop();
//                         if (_properties.Count > 0)
//                         {
//                             _properties.Pop();
//                         }
//                         break;
//
//                     case JsonTokenType.EndArray:
//                         if (_properties.Count > 0)
//                         {
//                             prop = _properties.Pop();
//                             if (prop.IsArray)
//                             {
//                                 var array = Array.CreateInstance(GetListElementType(prop.ArrayPropertyInfo.PropertyType), prop.List.Count); // array is finished, convert list into a real array
//                                 prop.List.CopyTo(array, 0);
//                                 prop.ArrayPropertyInfo.SetValue(_objects.Peek(), array);
//                             }
//                         }
//                         break;
//
//                     case JsonTokenType.False:
//                     case JsonTokenType.Null:
//                     case JsonTokenType.Number:
//                     case JsonTokenType.String:
//                     case JsonTokenType.True:
//                         peek = _properties.Peek();
//                         if (peek.List != null)
//                         {
//                             if (populator.TryReadPropertyValue(ref reader, options, peek.ListPropertyType, out var item))
//                             {
//                                 peek.List.Add(item);
//                             }
//                             break;
//                         }
//
//                         prop = _properties.Pop();
//                         var current = _objects.Peek();
//                         if (current != null)
//                         {
//                             populator.SetPropertyValue(ref reader, options, current, prop.PropertyName);
//                         }
//                         break;
//                 }
//             }
//         }
//
//         private static Type GetListElementType(Type type)
//         {
//             if (type.IsArray)
//                 return type.GetElementType();
//
//             foreach (Type iface in type.GetInterfaces())
//             {
//                 if (!iface.IsGenericType) continue;
//                 if (iface.GetGenericTypeDefinition() == typeof(IDictionary<,>)) return iface.GetGenericArguments()[1];
//                 if (iface.GetGenericTypeDefinition() == typeof(IList<>)) return iface.GetGenericArguments()[0];
//                 if (iface.GetGenericTypeDefinition() == typeof(ICollection<>)) return iface.GetGenericArguments()[0];
//                 if (iface.GetGenericTypeDefinition() == typeof(IEnumerable<>)) return iface.GetGenericArguments()[0];
//             }
//             return typeof(object);
//         }
//     }
//
//     private class WorkerProperty
//     {
//         public string PropertyName;
//         public IList List;
//         public Type ListPropertyType;
//         public bool IsArray;
//         public PropertyInfo ArrayPropertyInfo;
//
//         public override string ToString() => PropertyName;
//     }
// }
