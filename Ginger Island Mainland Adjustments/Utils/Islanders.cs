﻿namespace GingerIslandMainlandAdjustments.Utils;

/// <summary>
/// Methods to handle the current set of Islanders.
/// </summary>
internal class Islanders
{
    /// <summary>
    /// Gets a list of Islanders.
    /// </summary>
    /// <returns>Sorted list of Islanders by name.</returns>
    public static List<string> Get()
    {
        List<string> islanders = new();
        foreach (string name in Game1.netWorldState.Value.IslandVisitors.Keys)
        {
            if (Game1.netWorldState.Value.IslandVisitors[name])
            {
                islanders.Add(name);
            }
        }
        islanders.Sort();
        return islanders;
    }
}