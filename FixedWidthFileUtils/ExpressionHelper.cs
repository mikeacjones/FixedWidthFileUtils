using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace FixedWidthFileUtils
{
    internal class ExpressionHelper
    {
        #region CACHE
        private static readonly Dictionary<Type, Func<BufferedStreamReader, bool, object>> _GenericMethodCache = new Dictionary<Type, Func<BufferedStreamReader, bool, object>>();
        private static readonly Dictionary<Type, Func<BufferedStreamReader, object>> _GenericEnumerableMethodCache = new Dictionary<Type, Func<BufferedStreamReader, object>>();
        private static readonly Dictionary<Type, FieldSerializer> _SerializerCache = new Dictionary<Type, FieldSerializer>();
        #endregion

        public static Func<BufferedStreamReader, bool, object> GetDeserialize(Type type)
        {
            if (_GenericMethodCache.TryGetValue(type, out Func<BufferedStreamReader, bool, object> func)) return func;

            MethodInfo methodInfo = typeof(FixedWidthSerializer)
                .GetMethod("Deserialize", new[] { typeof(BufferedStreamReader), typeof(bool) })
                .MakeGenericMethod(new[] { type });

            var inputStreamParam = Expression.Parameter(typeof(BufferedStreamReader), "inputStream");
            var partOfEnumParam = Expression.Parameter(typeof(bool), "partOfEnum");
            var call = Expression.Call(methodInfo, inputStreamParam, partOfEnumParam);
            var lambda = Expression.Lambda<Func<BufferedStreamReader, bool, object>>(call, inputStreamParam, partOfEnumParam).Compile();
            _GenericMethodCache.Add(type, lambda);

            return lambda;
        }
        public static Func<BufferedStreamReader, object> GetDeserializeEnumerable<TResult>(Type itemType)
        {
            if (_GenericEnumerableMethodCache.TryGetValue(itemType, out Func<BufferedStreamReader, object> func)) return func;

            MethodInfo methodInfo = typeof(FixedWidthSerializer)
                .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                .FirstOrDefault(m => m.Name == "DeserializeEnumerable")
                .MakeGenericMethod(new[] { typeof(TResult), itemType });

            var inputStreamParam = Expression.Parameter(typeof(BufferedStreamReader), "inputStream");
            var call = Expression.Call(methodInfo, inputStreamParam);
            var lambda = Expression.Lambda<Func<BufferedStreamReader, object>>(call, inputStreamParam).Compile();
            _GenericEnumerableMethodCache.Add(itemType, lambda);

            return lambda;
        }
        public static FieldSerializer GetFieldSerializer(Type type)
        {
            if (_SerializerCache.TryGetValue(type, out FieldSerializer serializer)) return serializer;

            FieldSerializer fs = new FieldSerializer(type);
            _SerializerCache.Add(type, fs);
            return fs;
        }
    }
}
