using FixedWidthFileUtils.Serializers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FixedWidthFileUtils
{
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
        /// <summary>
        /// Gets a collection of SerializerField objects from the Type; these are found by looking
        /// for properties decorated with the FixedFieldAttribute
        /// </summary>
        /// <param name="o">Type to get SerializerField collection for</param>
        /// <returns>Collection of SerializerField objects</returns>
        public static IEnumerable<SerializerField> GetSerializerFields(this Type o)
        {
            return o.GetProperties()
                .Where(p => Attribute.IsDefined(p, typeof(FixedFieldAttribute)))
                .Select(p =>
                {
                    var serializerAttrs = p.GetCustomAttributes(typeof(FixedFieldAttribute)).Cast<FixedFieldAttribute>();
                    var conveterAttr = p.GetCustomAttribute(typeof(FixedFieldSerializerAttribute)) as FixedFieldSerializerAttribute;

                    return serializerAttrs.Select(sa =>
                    {
                        Type serializer = conveterAttr?.Type ?? typeof(DefaultSerializer<>).MakeGenericType(p.PropertyType);
                        return new SerializerField(sa, p, serializer);
                    });
                })
                .SelectMany(ffs => ffs)
                .OrderBy(f => f.Position);
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
                .Select(t => t.GenericTypeArguments[0]).FirstOrDefault();
            return enumType ?? type;
        }
    }
}
