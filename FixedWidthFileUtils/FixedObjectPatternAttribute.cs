using System;

namespace FixedWidthFileUtils
{
    /// <summary>
    /// Provides a way to set a regular expression pattern for an object
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class FixedObjectPatternAttribute : Attribute
    {
        public string MatchPattern { get; private set; }
        
        public FixedObjectPatternAttribute(string matchPattern)
        {
            this.MatchPattern = matchPattern;
        }
    }
}
