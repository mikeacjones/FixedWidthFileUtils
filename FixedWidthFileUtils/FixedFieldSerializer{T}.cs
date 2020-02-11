namespace FixedWidthFileUtils
{
    /// <summary>
    /// Abstract class definition for creating custom serializer
    /// </summary>
    /// <typeparam name="TType"></typeparam>
    public abstract class FixedFieldSerializer<TType>
    {
        /// <summary>
        /// Should return a deserialized object from the custom serializer
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public abstract TType Deserialize(string input);
        /// <summary>
        /// Returns a string representing an object serialized using custom serializer
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public abstract string Serialize(TType input);
    }
}
