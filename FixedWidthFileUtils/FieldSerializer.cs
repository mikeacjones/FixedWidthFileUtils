using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace FixedWidthFileUtils
{
    internal class FieldSerializer
    {
        private Func<object, string, object> _DeserializeFunc { get; }
        private Func<object, object, string> _SerializeFunc { get; }
        private object Serializer { get; }

        public object Deserialize(string input)
        {
            return _DeserializeFunc(Serializer, input);
        }

        public string Serialize(object input)
        {
            return _SerializeFunc(Serializer, input);
        }

        public FieldSerializer(Type type)
        {
            Serializer = Activator.CreateInstance(type);

            //INIT DESERIALIZE FUNC
            var deserializeMethodInfo = type.GetMethod("Deserialize");
            var instance = Expression.Parameter(typeof(object), "instance");
            var instanceCast = Expression.TypeAs(instance, type);
            var inputString = Expression.Parameter(typeof(string), "inputString");
            var dcall = Expression.Call(instanceCast, deserializeMethodInfo, inputString);
            var typeAs = Expression.TypeAs(dcall, typeof(object));
            _DeserializeFunc = Expression.Lambda<Func<object, string, object>>(typeAs, instance, inputString).Compile();

            //INIT SERIALIZE FUNC
            var serializeMethodInfo = type.GetMethod("Serialize");
            var inputType = type.GetMethod("Serialize").GetParameters()[0].ParameterType;
            var inputObject = Expression.Parameter(typeof(object), "inputObject");
            UnaryExpression inputCast;
            if (inputType.IsValueType)
                inputCast = Expression.Convert(inputObject, inputType);
            else
                inputCast = Expression.TypeAs(inputObject, inputType);
            var scall = Expression.Call(instanceCast, serializeMethodInfo, inputCast);
            _SerializeFunc = Expression.Lambda<Func<object, object, string>>(scall, instance, inputObject).Compile();
        }
    }
}
