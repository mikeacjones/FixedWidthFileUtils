﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FixedWidthFileUtils.Attributes;
using FixedWidthFileUtils.Enums;
using FixedWidthFileUtils.Utilities;

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
            var output = new StringBuilder();

            //are we directly trying to serialize an array, list, etc?
            if (o is IEnumerable enumerable)
            {
                var i = 0;
                foreach (var item in enumerable)
                {
                    if (i++ > 0) output.Append(Environment.NewLine);
                    output.Append(Serialize(item));
                }
                return output.ToString();
            }

            //no? then lets serialize the fields
            var fields = o.GetType().GetSerializerFields();

            for (var i = 0; i < fields.Length; i++)
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
                    var result = serializer.Serialize(field.Property.Get(o));

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
            MemoryStream msr = null;
            try
            {
                msr = new MemoryStream(Encoding.UTF8.GetBytes(input));
                using (var sr = new BufferedStreamReader(msr))
                {
                    msr = null;
                    return Deserialize<TResult>(sr);
                }
            } finally
            {
                msr?.Dispose();
            }
        }
        /// <summary>
        /// Uses file stream to deserialize fixed width file to object
        /// </summary>
        /// <typeparam name="TResult">Object type to deserialize file to</typeparam>
        /// <param name="fileStream">FileStream to use when deserializing</param>
        /// <returns></returns>
        public static TResult Deserialize<TResult>(FileStream fileStream) where TResult : class
        {
            using (var sr = new BufferedStreamReader(fileStream))
                return Deserialize<TResult>(sr);
        }

        /// <summary>
        /// Deserializes the BufferedStreamReader to object
        /// </summary>
        /// <typeparam name="TResult">Object type to deserialize to</typeparam>
        /// <param name="inputStream">BufferedStreamReader to use when deserializing</param>
        /// <param name="partOfEnum"></param>
        /// <returns></returns>
        public static TResult Deserialize<TResult>(BufferedStreamReader inputStream, bool partOfEnum = false)
            where TResult : class
        {
            if (inputStream.EndOfStream) return default;
            if (!typeof(TResult).IsEnumerable()) return DeserializeObject<TResult>(inputStream, partOfEnum);
            var itemType = typeof(TResult).GetItemType();
            var deserializeFunc = ExpressionHelper.GetDeserializeEnumerable<TResult>(itemType);

            try
            {
                return deserializeFunc(inputStream) as TResult;
            } catch
            {
                return null;
            }
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
            if (!fields.Any()) return default;

            //now, one rule for deserializing is that you can't mix fields with objects - you need to create wrappers for the fields instead
            var firstField = fields.FirstOrDefault();
            if (fields.Any(f => f.IsComplexType != firstField?.IsComplexType))
                throw new Exception($"Type {typeof(TResult).GetItemType()} mixes field and complex types which is not supported");


            var parsingFields = !fields.Any(f => f.IsComplexType);
            if (parsingFields) return DeserializeObjectFields<TResult>(inputStream, fields, partOfEnum);


            var result = Activator.CreateInstance(typeof(TResult)) as TResult;
            bool setAField = false;
            foreach (var field in fields)
            {
                var deserializeFunc = ExpressionHelper.GetDeserialize(field.Property.PropertyType);
                object val = deserializeFunc(inputStream, partOfEnum);
                field.Property.Set(result, val);
                if (val != null) setAField = true;
            }
            if (partOfEnum && !setAField) return null; //assume we are outside of the array now. Fixed width files is such a horrid format...
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
        private static TResult DeserializeObjectFields<TResult>(BufferedStreamReader inputStream, SerializerField[] fields, bool partOfEnum)
            where TResult : class
        {
            var result = Activator.CreateInstance(typeof(TResult)) as TResult;

            long totalWidth = fields.Sum(f => f.Width);
            if (inputStream.PeekLine().Length != totalWidth)
            {
                if (!partOfEnum)
                    throw new FormatException($"Format does not match expected width.\r\nParsing object: {typeof(TResult)}\r\nExpected width: {totalWidth}\r\nLine in file: {inputStream.ReadLine()}");
                return default;
            }

            if (Attribute.IsDefined(typeof(TResult), typeof(FixedObjectPatternAttribute)))
            {
                if (Attribute.GetCustomAttribute(typeof(TResult), typeof(FixedObjectPatternAttribute)) is
                        FixedObjectPatternAttribute objectPattern &&
                    !Regex.IsMatch(inputStream.PeekLine(), objectPattern.MatchPattern))
                    if (!partOfEnum)
                        throw new FormatException($"Format does not match expected pattern.\r\nParsing object: {typeof(TResult)}\r\nMatch pattern: {objectPattern.MatchPattern}\r\nLine in file: {inputStream.ReadLine()}");
                    else return default;
            }

            var objectLine = inputStream.ReadLine();
            var currentWidth = 0;
            try
            {
                foreach (var field in fields)
                {
                    var fieldText = objectLine?.Substring(currentWidth, field.Width) ?? string.Empty;
                    currentWidth += field.Width;

                    if (!field.Property.CanWrite) continue;

                    var serializer = ExpressionHelper.GetFieldSerializer(field.Converter);
                    field.Property.Set(result, serializer.Deserialize(fieldText));
                }
            }
            catch
            {
                if (!partOfEnum) throw;
                inputStream.RequeueLine(objectLine);
                return default;
            }

            return result;
        }
        /// <summary>
        /// Deserializes an IEnumerable type (array, list, linked list, hashset, etc). Special type as rather than directly deserializing an object we are deserializing a collection of objects
        /// </summary>
        /// <typeparam name="TResult">Collection type to return</typeparam>
        /// <typeparam name="TType">Type of object contained in the collection</typeparam>
        /// <param name="inputStream">BufferedStreamReader to use while deserializing</param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Local
        private static TResult DeserializeEnumerable<TResult, TType>(BufferedStreamReader inputStream) //ignore the IDE, this is being called through reflection
            where TResult : class
            where TType : class
        {
            var list = new List<TType>();

            TType item;

            try
            {
                while ((item = Deserialize<TType>(inputStream, true)) != null)
                    list.Add(item);
            }
            catch (IndexOutOfRangeException) { }

            if (list.Count == 0) throw new IndexOutOfRangeException();

            if (typeof(TResult).IsArray)
                return list.ToArray<TType>() as TResult;

            var constructor = typeof(TResult).GetConstructor(new[] { typeof(IEnumerable<TType>) });
            if (constructor != null) return constructor.Invoke(new object[] { list }) as TResult;

            return (TResult)list.AsEnumerable();
        }
        #endregion
    }
}