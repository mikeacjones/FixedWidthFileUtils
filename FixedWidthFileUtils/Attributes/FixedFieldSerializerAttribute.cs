using System;

namespace FixedWidthFileUtils.Attributes
{
    /// <summary>
    /// Attribute for specifying a custom serializer for a field
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class FixedFieldSerializerAttribute : Attribute
    {
        public Type Type { get; }
        /// <summary>
        /// Creates a new FixedFieldSerializerAttribute which uses the specific FixedFieldSerializer{T}
        /// </summary>
        /// <param name="serializerType"></param>
        public FixedFieldSerializerAttribute(Type serializerType)
        {
            Type = serializerType;
        }
    }
}
