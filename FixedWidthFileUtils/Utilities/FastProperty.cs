using System;
using System.Linq.Expressions;
using System.Reflection;

namespace FixedWidthFileUtils
{
    internal class FastProperty
    {
        public readonly Func<object, object> GetDelegate;
        public readonly Action<object, object> SetDelegate;
        public readonly bool CanWrite;
        public readonly string Name;
        public readonly Type PropertyType;

        public FastProperty(PropertyInfo property)
        {
            Property = property;
            CanWrite = property.CanWrite;
            Name = property.Name;
            PropertyType = property.PropertyType;
            GetDelegate = GetGetMethod(Property);
            if (CanWrite)
                SetDelegate = GetSetMethod(Property, false);
        }

        public FastProperty(PropertyInfo property, BindingFlags bindingFlags)
        {
            Property = property;
            GetDelegate = GetGetMethod(Property);
            if (CanWrite)
                SetDelegate = GetSetMethod(Property, (bindingFlags & BindingFlags.NonPublic) == BindingFlags.NonPublic);
        }

        public PropertyInfo Property { get; private set; }

        public object Get(object instance)
        {
            return GetDelegate(instance);
        }

        public void Set(object instance, object value)
        {
            SetDelegate(instance, value);
        }

        static Action<object, object> GetSetMethod(PropertyInfo property, bool includeNonPublic)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var value = Expression.Parameter(typeof(object), "value");

            // value as T is slightly faster than (T)value, so if it's not a value type, use that
            UnaryExpression instanceCast;
            if (property.DeclaringType.IsValueType)
                instanceCast = Expression.Convert(instance, property.DeclaringType);
            else
                instanceCast = Expression.TypeAs(instance, property.DeclaringType);

            UnaryExpression valueCast;
            if (property.PropertyType.IsValueType)
                valueCast = Expression.Convert(value, property.PropertyType);
            else
                valueCast = Expression.TypeAs(value, property.PropertyType);

            var call = Expression.Call(instanceCast, property.GetSetMethod(includeNonPublic), valueCast);

            return Expression.Lambda<Action<object, object>>(call, new[] { instance, value }).Compile();
        }

        static Func<object, object> GetGetMethod(PropertyInfo property)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            UnaryExpression instanceCast;
            if (property.DeclaringType.IsValueType)
                instanceCast = Expression.Convert(instance, property.DeclaringType);
            else
                instanceCast = Expression.TypeAs(instance, property.DeclaringType);

            var call = Expression.Call(instanceCast, property.GetGetMethod());
            var typeAs = Expression.TypeAs(call, typeof(object));

            return Expression.Lambda<Func<object, object>>(typeAs, instance).Compile();
        }
    }
}
