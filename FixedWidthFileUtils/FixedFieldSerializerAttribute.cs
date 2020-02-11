using System;

namespace FixedWidthFileUtils
{
	/// <summary>
	/// Attribute for specifying a custom serializer for a field
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class FixedFieldSerializerAttribute : Attribute
	{
		public Type Type { get; private set; }
		/// <summary>
		/// Creates a new FixedFieldSerializerAttribute which uses the specific FixedFieldSerializer<>
		/// </summary>
		/// <param name="serializerType"></param>
		public FixedFieldSerializerAttribute(Type serializerType)
		{
			Type = serializerType;
		}
	}
}
