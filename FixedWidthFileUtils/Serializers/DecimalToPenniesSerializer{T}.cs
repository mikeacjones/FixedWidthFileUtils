using System;

namespace FixedWidthFileUtils.Serializers
{
    public class DecimalToPenniesSerializer : FixedFieldSerializer<decimal>
    {
        public override decimal Deserialize(string input) => Decimal.Parse(input) / 100.00m;
        public override string Serialize(decimal input) => $"{(int)(input * 100.00m)}";
    }
}
