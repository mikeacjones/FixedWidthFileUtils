using System;

namespace FixedWidthFileUtils.Serializers
{
    /// <summary>
    /// Default serializer used when a serializer isn't specified. Uses the Convert function to cast objects. You can implement IConvertible on a class to specify this behavior
    /// or specify a custom Serializer
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DefaultSerializer<T> : FixedFieldSerializer<T>
    {
        public override T Deserialize(string input) => (T)Convert.ChangeType(input, typeof(T));
        public override string Serialize(T input) => input.ToString();
    }
}
