using System;

namespace FixedWidthFileUtils.Serializers
{
	public class DefaultSerializer<T> : FixedFieldSerializer<T>
	{
		public override T Deserialize(string input) => (T)Convert.ChangeType(input, typeof(T));
		public override string Serialize(T input) => input.ToString();
	}
}
