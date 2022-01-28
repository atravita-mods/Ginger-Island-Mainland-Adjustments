﻿using Microsoft.Xna.Framework;

namespace GingerIslandMainlandAdjustments.Utils;

/// <summary>
/// Utility methods.
/// </summary>
internal static class Utils
{
    /// <summary>
    /// Yields all tiles around a specific tile.
    /// </summary>
    /// <param name="tile">Vector2 location of tile.</param>
    /// <param name="radius">A radius to search in.</param>
    /// <returns>All tiles within radius.</returns>
    /// <remarks>This actually returns a square, not a circle.</remarks>
    internal static IEnumerable<Point> YieldSurroundingTiles(Vector2 tile, int radius = 1)
    {
        int x = (int)tile.X;
        int y = (int)tile.Y;
        for (int xdiff = -radius; xdiff <= radius; xdiff++)
        {
            for (int ydiff = -radius; ydiff <= radius; ydiff++)
            {
                Globals.ModMonitor.Log($"{x + xdiff} {y + ydiff}");
                yield return new Point(x + xdiff, y + ydiff);
            }
        }
    }

    /// <summary>
    /// Yields an iterator over all tiles on a location.
    /// </summary>
    /// <param name="location">Location to check.</param>
    /// <returns>IEnumerable of all tiles.</returns>
    internal static IEnumerable<Vector2> YieldAllTiles(GameLocation location)
    {
        for (int x = 0; x < location.Map.Layers[0].LayerWidth; x++)
        {
            for (int y = 0; y < location.Map.Layers[0].LayerHeight; y++)
            {
                yield return new Vector2(x, y);
            }
        }
    }
}