using System;

namespace FixedWidthFileUtils.Attributes
{
    /// <summary>
    /// Provides a way to set a regular expression pattern for an object
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class FixedObjectPatternAttribute : Attribute
    {
        public string MatchPattern { get; }

        public FixedObjectPatternAttribute(string matchPattern)
        {
            this.MatchPattern = matchPattern;
        }
    }
}
