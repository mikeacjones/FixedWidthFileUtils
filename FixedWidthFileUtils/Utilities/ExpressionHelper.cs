using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using FixedWidthFileUtils.Serializers;

namespace FixedWidthFileUtils.Utilities
{
    /// <summary>
    /// Helper class for returning (and caching) compiled expressions for Deserialize/DeserializeEnumerable and FixedFieldSerializer{T}
    /// </summary>
    internal static class ExpressionHelper
    {
        #region CACHE
        private static readonly Dictionary<Type, Func<BufferedStreamReader, bool, object>> GenericMethodCache =
            new Dictionary<Type, Func<BufferedStreamReader, bool, object>>();
        private static readonly Dictionary<Type, Func<BufferedStreamReader, object>> GenericEnumerableMethodCache =
            new Dictionary<Type, Func<BufferedStreamReader, object>>();
        private static readonly Dictionary<Type, FieldSerializer> SerializerCache =
            new Dictionary<Type, FieldSerializer>();
        #endregion
        /// <summary>
        /// Returns the compiled, generic function for deserializing the specified type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>

        public static Func<BufferedStreamReader, bool, object> GetDeserialize(Type type)
        {
            if (GenericMethodCache.TryGetValue(type, out var func)) return func;

            var methodInfo = typeof(FixedWidthSerializer)
                .GetMethod("Deserialize", new[] { typeof(BufferedStreamReader), typeof(bool) })
                ?.MakeGenericMethod(type);
            if (methodInfo == null) throw new InvalidOperationException("Unable to reflect Deserialize method in ExpressionHelper.GetDeserialize");

            var inputStreamParam = Expression.Parameter(typeof(BufferedStreamReader), "inputStream");
            var partOfEnumParam = Expression.Parameter(typeof(bool), "partOfEnum");
            var call = Expression.Call(methodInfo, inputStreamParam, partOfEnumParam);
            var lambda = Expression.Lambda<Func<BufferedStreamReader, bool, object>>(call, inputStreamParam, partOfEnumParam).Compile();
            GenericMethodCache.Add(type, lambda);

            return lambda;
        }
        /// <summary>
        /// Returns the compiled, generic function for deserialized the Enumerable containing items of type itemType. TResult specifies the type of enumerable expected to be returned.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="itemType"></param>
        /// <returns></returns>
        public static Func<BufferedStreamReader, object> GetDeserializeEnumerable<TResult>(Type itemType)
        {
            if (GenericEnumerableMethodCache.TryGetValue(itemType, out var func)) return func;

            var methodInfo = typeof(FixedWidthSerializer)
                .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                .FirstOrDefault(m => m.Name == "DeserializeEnumerable")
                ?.MakeGenericMethod(typeof(TResult), itemType);
            if (methodInfo == null) 
                throw new InvalidOperationException("Unable to reflect DeserializeEnumerable method in ExpressionHelper.GetDeserializeEnumerable");

            var inputStreamParam = Expression.Parameter(typeof(BufferedStreamReader), "inputStream");
            var call = Expression.Call(methodInfo, inputStreamParam);
            var lambda = Expression.Lambda<Func<BufferedStreamReader, object>>(call, inputStreamParam).Compile();
            GenericEnumerableMethodCache.Add(itemType, lambda);

            return lambda;
        }
        /// <summary>
        /// Returns the FieldSerializer, which encapsulates the compiled Serialize and Deserialize methods, for the specified serializer type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static FieldSerializer GetFieldSerializer(Type type)
        {
            if (SerializerCache.TryGetValue(type, out var serializer)) return serializer;

            var fs = new FieldSerializer(type);
            SerializerCache.Add(type, fs);
            return fs;
        }
    }
}
