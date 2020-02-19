using System;
using System.Reflection;
using FixedWidthFileUtils.Attributes;
using FixedWidthFileUtils.Enums;

namespace FixedWidthFileUtils.Utilities
{
    /// <summary>
    /// Object that wraps up necessary information about serializable fields
    /// </summary>
    internal class SerializerField
    {
        public int Position { get; }
        public int Width { get; }
        public char Padder { get; }
        public FixedFieldAlignment Alignment { get; }
        public FixedFieldOverflowMode OverflowMode { get; }
        public FastProperty Property { get; }
        public Type Converter { get; }
        public bool IsComplexType { get; }
        /// <summary>
        /// Creates anew SerializerField that wraps up the necessary information for serializing/deserializing an object
        /// </summary>
        /// <param name="config">Config information pulled from the CustomAttribute, FixedFieldAttribute</param>
        /// <param name="prop">PropertyInfo representing the property on the object</param>
        /// <param name="converter">Custom serializer specified for teh property</param>
        public SerializerField(FixedFieldAttribute config, PropertyInfo prop, Type converter)
        {
            this.Position = config.Position;
            this.Width = config.Width;
            this.Padder = config.Padder;
            this.Alignment = config.Alignment;
            this.OverflowMode = config.OverflowMode;
            this.Property = new FastProperty(prop);
            this.Converter = converter;
            this.IsComplexType = prop.PropertyType.IsComplexType();
        }
    }
}
