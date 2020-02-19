using System;
using System.Linq.Expressions;
using System.Reflection;

namespace FixedWidthFileUtils.Utilities
{
    /// <summary>
    /// Helper class which encapsulates the compiled Get and Set methods for the specified property.
    /// </summary>
    internal class FastProperty
    {
        private readonly Func<object, object> _getDelegate;
        private readonly Action<object, object> _setDelegate;
        public readonly bool CanWrite;
        public readonly string Name;
        public readonly Type PropertyType;

        /// <summary>
        /// Creates a new FastProperty for the specified property - this creates compiled delegates for the Get and Set method of the property
        /// </summary>
        /// <param name="property"></param>
        public FastProperty(PropertyInfo property)
        {
            CanWrite = property.CanWrite;
            Name = property.Name;
            PropertyType = property.PropertyType;
            _getDelegate = GetGetMethod(property);
            if (CanWrite)
                _setDelegate = GetSetMethod(property, false);
        }

        /// <summary>
        /// Get the value of the property on the specified instance
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        public object Get(object instance)
        {
            return _getDelegate(instance);
        }
        /// <summary>
        /// Sets the specified value of the property on the specified instance
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="value"></param>
        public void Set(object instance, object value)
        {
            _setDelegate(instance, value);
        }
        /// <summary>
        /// Returns the compiled Action for the Set method.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="includeNonPublic"></param>
        /// <returns></returns>
        private static Action<object, object> GetSetMethod(PropertyInfo property, bool includeNonPublic)
        {
            if (property?.DeclaringType == null) return null;
            var instance = Expression.Parameter(typeof(object), "instance");
            var value = Expression.Parameter(typeof(object), "value");

            // value as T is slightly faster than (T)value, so if it's not a value type, use that
            var instanceCast = property.DeclaringType.IsValueType
                ? Expression.Convert(instance, property.DeclaringType)
                : Expression.TypeAs(instance, property.DeclaringType);

            var valueCast = property.PropertyType.IsValueType
                ? Expression.Convert(value, property.PropertyType)
                : Expression.TypeAs(value, property.PropertyType);

            var call = Expression.Call(instanceCast, property.GetSetMethod(includeNonPublic), valueCast);

            return Expression.Lambda<Action<object, object>>(call, instance, value ).Compile();
        }
        /// <summary>
        /// Returns the compiled Func for the Get method
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        private static Func<object, object> GetGetMethod(PropertyInfo property)
        {
            if (property?.DeclaringType == null) return null;
            var instance = Expression.Parameter(typeof(object), "instance");
            var instanceCast = property.DeclaringType.IsValueType
                ? Expression.Convert(instance, property.DeclaringType)
                : Expression.TypeAs(instance, property.DeclaringType);

            var call = Expression.Call(instanceCast, property.GetGetMethod());
            var typeAs = Expression.TypeAs(call, typeof(object));

            return Expression.Lambda<Func<object, object>>(typeAs, instance).Compile();
        }
    }
}
