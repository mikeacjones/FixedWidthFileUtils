namespace FixedWidthFileUtils
{
    /// <summary>
    /// Provides a way to explicitly set how fields with a value that is too long is handled
    /// </summary>
    public enum FixedFieldOverflowMode
    {
        Truncate,
        NoOverflow
    }
}
