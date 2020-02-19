using System;
using System.Linq.Expressions;

namespace FixedWidthFileUtils.Serializers
{

    /// <summary>
    /// Helper class to contain compiled serialize/deserialize functions for a specific Serializer type.
    /// </summary>
    internal class FieldSerializer
    {
        private Func<object, string, object> DeserializeFunc { get; }
        private Func<object, object, string> SerializeFunc { get; }
        private object Serializer { get; }

        public object Deserialize(string input)
        {
            return DeserializeFunc(Serializer, input);
        }

        public string Serialize(object input)
        {
            return SerializeFunc(Serializer, input);
        }

        public FieldSerializer(Type type)
        {
            Serializer = Activator.CreateInstance(type);

            //INIT DESERIALIZE FUNC
            var deserializeMethodInfo = type.GetMethod("Deserialize");
            if (deserializeMethodInfo == null)
                throw new InvalidOperationException("Unable to locate Deserialize method for compilation");
            var instance = Expression.Parameter(typeof(object), "instance");
            var instanceCast = Expression.TypeAs(instance, type);
            var inputString = Expression.Parameter(typeof(string), "inputString");
            var dcall = Expression.Call(instanceCast, deserializeMethodInfo, inputString);
            var typeAs = Expression.TypeAs(dcall, typeof(object));
            DeserializeFunc = Expression.Lambda<Func<object, string, object>>(typeAs, instance, inputString).Compile();

            //INIT SERIALIZE FUNC
            var serializeMethodInfo = type.GetMethod("Serialize");
            if (serializeMethodInfo == null)
                throw new InvalidOperationException("Unable to locate Serialize method for compilation");
            var inputType = type.GetMethod("Serialize")?.GetParameters()[0].ParameterType;
            if (inputType == null) throw new InvalidOperationException("Unable to determine type of serializer - FieldSerializer construction failed");
            var inputObject = Expression.Parameter(typeof(object), "inputObject");
            var inputCast = inputType.IsValueType ? Expression.Convert(inputObject, inputType) : Expression.TypeAs(inputObject, inputType);
            var scall = Expression.Call(instanceCast, serializeMethodInfo, inputCast);
            SerializeFunc = Expression.Lambda<Func<object, object, string>>(scall, instance, inputObject).Compile();
        }
    }
}
