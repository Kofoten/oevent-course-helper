namespace OEventCourseHelper.Extensions;

internal static class Int32Extensions
{
    /// <summary>
    /// Calculates the number of 64 bit (ulong) buckets needed to store the given amount of bits.
    /// </summary>
    /// <param name="bits">The number of bits to store.</param>
    /// <returns>The number of buckets required.</returns>
    public static int Get64BitBucketCount(this int bits)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bits);
        return ((bits - 1) >> 6) + 1;
    }
}
