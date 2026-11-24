using System;
using UnityEngine;

public static class RegionMapBuilder
{
    public static RegionMap Build(byte[] grid, Vector3Int size,
                                  Func<byte, Color> paletteLookup,
                                  RegionColorRules rules)
    {
        var map = new RegionMap(size);
        int n = size.x * size.y * size.z;
        for (int i = 0; i < n; i++)
        {
            byte idx = grid[i];
            if (idx == 0) continue;
            map.Grid[i] = (byte)rules.Classify(paletteLookup(idx));
        }
        return map;
    }
}