using System;
using System.Reflection;

namespace FixedWidthFileUtils
{
	/// <summary>
	/// Object that wraps up necessary information about serializable fields
	/// </summary>
	public class SerializerField
	{
		public int Position { get; private set; }
		public int Width { get; private set; }
		public char Padder { get; private set; }
		public FixedFieldAlignment Alignment { get; private set; }
		public FixedFieldOverflowMode OverflowMode { get; private set; }
		public PropertyInfo Property { get; private set; }
		public Type Converter { get; private set; }
		public bool IsComplexType { get; private set; }
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
			this.Property = prop;
			this.Converter = converter;
			this.IsComplexType = prop.PropertyType.IsComplexType();
		}
	}
}
