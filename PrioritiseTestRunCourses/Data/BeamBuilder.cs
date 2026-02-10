using System.Collections.Immutable;

namespace PrioritiseTestRunCourses.Data;

internal class BeamBuilder<T>(int BeamWidth, IComparer<T> comparer)
{
    private readonly List<T> beam = new(BeamWidth);

    public int Count => beam.Count;

    public bool IsFull => beam.Count == BeamWidth;

    public bool Insert(T item)
    {
        int index = beam.BinarySearch(item, comparer);

        if (index < 0)
        {
            index = ~index;
        }

        if (index < BeamWidth)
        {
            beam.Insert(index, item);

            if (beam.Count > BeamWidth)
            {
                beam.RemoveAt(BeamWidth);
            }

            return true;
        }

        return false;
    }

    public T? Worst() => beam.Count > 0 ? beam[^1] : default;

    public ImmutableList<T> ToImmutableList() => [.. beam];
}
