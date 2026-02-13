namespace OEventCourseHelper.Extensions;

internal static class Int32Extensions
{
    public static int GetUnsignedLongBucketCount(this int bits)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bits);
        return ((bits - 1) >> 6) + 1;
    }
}
