using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FixedWidthFileUtils.Attributes;
using FixedWidthFileUtils.Serializers;

namespace FixedWidthFileUtils.Utilities
{
    /// <summary>
    /// Extensions class
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Indicates if the Type is complex, IE a class (not including strings)
        /// </summary>
        /// <param name="t">Type to check</param>
        /// <returns>If the Type is a class (not including string)</returns>
        public static bool IsComplexType(this Type t)
        {
            return !t.IsValueType && t != typeof(string);
        }

        private static readonly Dictionary<Type, SerializerField[]> FieldCache = new Dictionary<Type, SerializerField[]>();
        /// <summary>
        /// Gets a collection of SerializerField objects from the Type; these are found by looking
        /// for properties decorated with the FixedFieldAttribute
        /// </summary>
        /// <param name="o">Type to get SerializerField collection for</param>
        /// <returns>Collection of SerializerField objects</returns>
        /// 
        public static SerializerField[] GetSerializerFields(this Type o)
        {
            if (FieldCache.TryGetValue(o, out var fields)) return fields;
            FieldCache.Add(o, o.GetProperties()
                .Where(p => Attribute.IsDefined(p, typeof(FixedFieldAttribute)))
                .Select(p =>
                {
                    var serializerAttrs = p.GetCustomAttributes(typeof(FixedFieldAttribute), false).Cast<FixedFieldAttribute>();
                    var converterAttr = p.GetCustomAttributes(typeof(FixedFieldSerializerAttribute), false).FirstOrDefault() as FixedFieldSerializerAttribute;

                    return serializerAttrs.Select(sa =>
                    {
                        var serializer = converterAttr?.Type ?? typeof(DefaultSerializer<>).MakeGenericType(p.PropertyType);
                        return new SerializerField(sa, p, serializer);
                    });
                })
                .SelectMany(ffs => ffs)
                .OrderBy(f => f.Position)
                .ToArray());
            return FieldCache[o];
        }
        /// <summary>
        /// Checks if the Type is an IEnumerable
        /// </summary>
        /// <param name="t">Type to check</param>
        /// <returns>If the Type is a collection</returns>
        public static bool IsEnumerable(this Type t)
        {
            return typeof(IEnumerable).IsAssignableFrom(t);
        }
        /// <summary>
        /// Returns the Type of the objects in the collection
        /// </summary>
        /// <param name="type">Collection item Type</param>
        /// <returns></returns>
        public static Type GetItemType(this Type type)
        {
            // Type is Array
            // short-circuit if you expect lots of arrays 
            if (type.IsArray)
                return type.GetElementType();

            // type is IEnumerable<T>;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return type.GetGenericArguments()[0];

            // type implements/extends IEnumerable<T>;
            var enumType = type
                .GetInterfaces()
                .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                .Select(t => t.GetGenericArguments().FirstOrDefault()).FirstOrDefault();
            return enumType ?? type;
        }
    }
}
