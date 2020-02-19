namespace FixedWidthFileUtils.Serializers
{
    /// <summary>
    /// Serializes decimals as pennies. IE: Instead of 99.99, store 9999.
    /// </summary>
    public class DecimalToPenniesSerializer : FixedFieldSerializer<decimal>
    {
        public override decimal Deserialize(string input) => decimal.Parse(input) / 100.00m;
        public override string Serialize(decimal input) => $"{(int)(input * 100.00m)}";
    }
}
