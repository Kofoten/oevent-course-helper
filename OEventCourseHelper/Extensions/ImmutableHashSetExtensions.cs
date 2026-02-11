using System.Collections.Frozen;
using System.Collections.Immutable;

namespace OEventCourseHelper.Extensions;

internal static class ImmutableHashSetExtensions
{
    /// <summary>
    /// Calculates the rarity score that would be gained if the specified controls were removed.
    /// </summary>
    /// <param name="unvisitedControls">The source set of unvisited controls to remove from.</param>
    /// <param name="courseControls">The enumerable containing the set of controls to remove.</param>
    /// <param name="controlRarityLookup">The lookup table for finding the rarity of a specific control.</param>
    /// <param name="defaultRarity">The default rarity to use if a control is not found in the lookup.</param>
    /// <returns>The rarity gain that would be gained by removing the courseControls from the set of unvisitedControls.</returns>
    public static float CalculatePotentialRarityGain(
        this ImmutableHashSet<string> unvisitedControls,
        IEnumerable<string> courseControls,
        FrozenDictionary<string, float> controlRarityLookup,
        float defaultRarity)
    {
        float rarityGain = 0.0f;

        foreach (var control in courseControls)
        {
            if (unvisitedControls.Contains(control))
            {
                rarityGain += controlRarityLookup.GetValueOrDefault(control, defaultRarity);
            }
        }

        return rarityGain;
    }

    /// <summary>
    /// Removes the specified controls and returns the new set and the calculated score gain.
    /// </summary>
    /// <param name="unvisitedControls">The source set of unvisited controls to remove from.</param>
    /// <param name="courseControls">The enumerable containing the set of controls to remove.</param>
    /// <param name="controlRarityLookup">The lookup table for finding the rarity of a specific control.</param>
    /// <param name="defaultRarity">The default rarity to use if a control is not found in the lookup.</param>
    /// <param name="rarityGain">This out parameter will contain the rarity gained from removing the courseControls from the unvisitedControls set.</param>
    /// <returns>Return the new set with all controls in the courseControls enumerable removed.</returns>
    public static ImmutableHashSet<string> RemoveControls(
        this ImmutableHashSet<string> unvisitedControls,
        IEnumerable<string> courseControls,
        FrozenDictionary<string, float> controlRarityLookup,
        float defaultRarity,
        out float rarityGain)
    {
        rarityGain = 0.0f;
        var builder = unvisitedControls.ToBuilder();

        foreach (var control in courseControls)
        {
            if (builder.Remove(control))
            {
                rarityGain += controlRarityLookup.GetValueOrDefault(control, defaultRarity);
            }
        }

        return builder.ToImmutable();
    }
}
