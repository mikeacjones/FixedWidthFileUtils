using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace FixedWidthFileUtils
{
    /// <summary>
    /// Class for Serializing and Deserializing fixed width files
    /// </summary>
    public static class FixedWidthSerializer
    {
        #region SERIALIZE
        /// <summary>
        /// Serializes an object to a string containing fixed width file data. Only fields decorated with the FixedField attribute are serialized.
        /// </summary>
        /// <param name="o">Object to serialize</param>
        /// <returns>String containing fixed width file data</returns>
        public static string Serialize(object o)
        {
            StringBuilder output = new StringBuilder();

            //are we directly trying to serialize an array, list, etc?
            if (o is IEnumerable enumerable)
            {
                int i = 0;
                foreach (var item in enumerable)
                {
                    if (i++ > 0) output.Append(Environment.NewLine);
                    output.Append(Serialize(item));
                }
                return output.ToString();
            }

            //no? then lets serialize the fields
            var fields = o.GetType().GetSerializerFields();

            for (int i = 0; i < fields.Count(); i++)
            {
                var field = fields.ElementAt(i);

                if (field.IsComplexType)
                {
                    if (i > 0) output.Append(Environment.NewLine);
                    output.Append(Serialize(field.Property.Get(o)));                    
                }
                else
                {
                    var serializer = ExpressionHelper.GetFieldSerializer(field.Converter);
                    string result = serializer.Serialize(field.Property.Get(o));

                    if (result.Length > field.Width)
                        if (field.OverflowMode == FixedFieldOverflowMode.NoOverflow)
                            throw new OverflowException($"Field {field.Property.Name} has overflown specified width of {field.Width}. Value is {result}");
                        else
                            result = result.Substring(0, field.Width);
                    else if (result.Length < field.Width)
                        result = field.Alignment == FixedFieldAlignment.Right ? result.PadLeft(field.Width, field.Padder) : result.PadRight(field.Width, field.Padder);
                    output.Append(result);
                }
            }
            return output.ToString();
        }
        #endregion

        #region DESERIALIZE
        /// <summary>
        /// Deserializes a string to object
        /// </summary>
        /// <typeparam name="TResult">Object type to deserialize string to</typeparam>
        /// <param name="input">String to deserialize</param>
        /// <returns></returns>
        public static TResult Deserialize<TResult>(string input) where TResult : class
        {
            using (MemoryStream msr = new MemoryStream(Encoding.UTF8.GetBytes(input)))
            using (BufferedStreamReader sr = new BufferedStreamReader(msr))
                return Deserialize<TResult>(sr);
        }
        /// <summary>
        /// Uses file stream to deserialize fixed width file to object
        /// </summary>
        /// <typeparam name="TResult">Object type to deserialize file to</typeparam>
        /// <param name="fileStream">FileStream to use when deserializing</param>
        /// <returns></returns>
        public static TResult Deserialize<TResult>(FileStream fileStream) where TResult : class
        {
            using (BufferedStreamReader sr = new BufferedStreamReader(fileStream))
                return Deserialize<TResult>(sr);
        }
        /// <summary>
        /// Deserializes the BufferedStreamReader to object
        /// </summary>
        /// <typeparam name="TResult">Object type to deserialize to</typeparam>
        /// <param name="inputStream">BufferedStreamReader to use when deserializing</param>
        /// <returns></returns>
        public static TResult Deserialize<TResult>(BufferedStreamReader inputStream, bool partOfEnum = false)
            where TResult : class
        {
            if (inputStream.EndOfStream) return default;
            if (typeof(TResult).IsEnumerable()) //enumerables are a special case
            {
                Type itemType = typeof(TResult).GetItemType();
                var deserializeFunc = ExpressionHelper.GetDeserializeEnumerable<TResult>(itemType);
                return deserializeFunc(inputStream) as TResult;
            }

            return DeserializeObject<TResult>(inputStream, partOfEnum);
        }
        /// <summary>
        /// Deserializes an object
        /// </summary>
        /// <typeparam name="TResult">Type to return</typeparam>
        /// <param name="inputStream">BufferedStreamReader to use when deserializing</param>
        /// <param name="partOfEnum"></param>
        /// <returns></returns>
        private static TResult DeserializeObject<TResult>(BufferedStreamReader inputStream, bool partOfEnum)
            where TResult : class
        {
            //first get the fields
            var fields = typeof(TResult).GetSerializerFields();

            //now, one rule for deserializing is that you can't mix fields with objects - you need to create wrappers for the fields instead
            if (fields.Any(f => f.IsComplexType) && fields.Any(f => !f.IsComplexType))
                throw new Exception($"Type {typeof(TResult).GetItemType()} mixes field and complex types which is not supported");


            bool parsingFields = !fields.Any(f => f.IsComplexType);
            if (parsingFields) return DeserializeObjectFields<TResult>(inputStream, fields, partOfEnum);


            TResult result = Activator.CreateInstance(typeof(TResult)) as TResult;
            foreach (var field in fields)
            {
                var deserializeFunc = ExpressionHelper.GetDeserialize(field.Property.PropertyType);
                field.Property.Set(result, deserializeFunc(inputStream, partOfEnum));
            }
            return result;
        }
        /// <summary>
        /// Method for populating fields of an object
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="inputStream"></param>
        /// <param name="fields"></param>
        /// <param name="partOfEnum"></param>
        /// <returns></returns>
        private static TResult DeserializeObjectFields<TResult>(BufferedStreamReader inputStream, IEnumerable<SerializerField> fields, bool partOfEnum)
            where TResult : class
        {
            TResult result = Activator.CreateInstance(typeof(TResult)) as TResult;

            long totalWidth = fields.Sum(f => f.Width);
            if (inputStream.PeekLine().Length != totalWidth)
            {
                if (!partOfEnum)
                    throw new FormatException($"Format does not match expected width.\r\nParsing object: {typeof(TResult)}\r\nExpected width: {totalWidth}\r\nLine in file: {inputStream.ReadLine()}");
                return default;
            }

            if (Attribute.IsDefined(typeof(TResult), typeof(FixedObjectPatternAttribute)))
            {
                FixedObjectPatternAttribute objectPattern = Attribute.GetCustomAttribute(typeof(TResult), typeof(FixedObjectPatternAttribute)) as FixedObjectPatternAttribute;
                if (!Regex.IsMatch(inputStream.PeekLine(), objectPattern.MatchPattern))
                    if (!partOfEnum)
                        throw new FormatException($"Format does not match expected pattern.\r\nParsing object: {typeof(TResult)}\r\nMatch pattern: {objectPattern.MatchPattern}\r\nLine in file: {inputStream.ReadLine()}");
                    else return default;
            }

            string objectLine = inputStream.ReadLine();
            int currentWidth = 0;
            try
            {
                foreach (var field in fields)
                {
                    string fieldText = objectLine.Substring(currentWidth, field.Width);
                    currentWidth += field.Width;

                    if (!field.Property.CanWrite) continue;

                    var serializer = ExpressionHelper.GetFieldSerializer(field.Converter);
                    field.Property.Set(result, serializer.Deserialize(fieldText));
                }
            }
            catch
            {
                if (partOfEnum)
                {
                    inputStream.RequeueLine(objectLine);
                    return default;
                }
                throw;
            }

            return result;
        }
        /// <summary>
        /// Deserializes an IEnumerable type (array, list, linkedlist, hashset, etc). Special type as rather than directly deserializing an object we are deserializing a collection of objects
        /// </summary>
        /// <typeparam name="TResult">Collection type to return</typeparam>
        /// <typeparam name="TType">Type of object contained in the collection</typeparam>
        /// <param name="inputStream">BufferedStreamReader to use while deserializing</param>
        /// <returns></returns>
        private static TResult DeserializeEnumerable<TResult, TType>(BufferedStreamReader inputStream) //ignore the IDE, this is being called through reflection
            where TResult : class
            where TType : class
        {
            var list = new List<TType>();

            TType item;
            while ((item = Deserialize<TType>(inputStream, true)) != null)
                list.Add(item);

            if (typeof(TResult).IsArray)
                return list.ToArray<TType>() as TResult;

            var constructor = typeof(TResult).GetConstructor(new[] { typeof(IEnumerable<TType>) });
            if (constructor != null) return constructor.Invoke(new[] { list }) as TResult;

            return (TResult)list.AsEnumerable<TType>();
        }
        #endregion
    }
}