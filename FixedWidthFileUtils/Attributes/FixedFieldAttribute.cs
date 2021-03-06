﻿using System;
using FixedWidthFileUtils.Enums;

namespace FixedWidthFileUtils.Attributes
{
    /// <summary>
    /// Attribute for marking a field as a serializable field
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class FixedFieldAttribute : Attribute
    {
        public readonly int Position;
        public readonly int Width;
        public readonly char Padder;
        public readonly FixedFieldAlignment Alignment;
        public readonly FixedFieldOverflowMode OverflowMode;
        /// <summary>
        /// Creates a new FixedFieldAttribute
        /// </summary>
        /// <param name="position">Order of field in fixed field row</param>
        /// <param name="width">Number of characters that field should occupy in string</param>
        /// <param name="padder">Character used when padding value of field</param>
        /// <param name="alignment">The alignment of the value, IE: should it to the right or left of its padding; default is Right</param>
        /// <param name="overflowMode">How should a value which overflows the width of the field be handled; default is NoOverflow</param>
        public FixedFieldAttribute(
            int position,
            int width = 0,
            char padder = '0',
            FixedFieldAlignment alignment = FixedFieldAlignment.Right,
            FixedFieldOverflowMode overflowMode = FixedFieldOverflowMode.NoOverflow)
        {
            this.Position = position;
            this.Width = width;
            this.Padder = padder;
            this.Alignment = alignment;
            this.OverflowMode = overflowMode;
        }
    }
}
