using System.Collections.Generic;
using UnityEngine;
using VoxReader;
using VoxReader.Interfaces;

public static class VoxLoader
{
    public class Model
    {
        public int sizeX, sizeY, sizeZ;
        public byte[] voxels;
        public Color32[] palette;
    }

    public static Model Load(byte[] data)
    {
        string tempPath = System.IO.Path.Combine(Application.temporaryCachePath, "vox_temp.vox");
        System.IO.File.WriteAllBytes(tempPath, data);

        IVoxFile file = VoxReader.VoxReader.Read(tempPath);
        if (file.Models == null || file.Models.Length == 0)
            throw new System.Exception("Arquivo .vox não contém modelos.");

        IModel source = file.Models[0];

        // ---------- Bounds (convenção MagicaVoxel: X, Y=depth, Z=up) ----------
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;   // Y do MagicaVoxel (depth)
        int minZ = int.MaxValue, maxZ = int.MinValue;   // Z do MagicaVoxel (up)
        int minW = int.MaxValue, maxW = int.MinValue;   // W do MagicaVoxel (up no formato antigo)

        foreach (var v in source.Voxels)
        {
            int gx = Mathf.RoundToInt(v.GlobalPosition.X);
            int gy = Mathf.RoundToInt(v.GlobalPosition.Y);
            int gz = Mathf.RoundToInt(v.GlobalPosition.Z);
            if (gx < minX) minX = gx; if (gx > maxX) maxX = gx;
            if (gy < minY) minY = gy; if (gy > maxY) maxY = gy;
            if (gz < minZ) minZ = gz; if (gz > maxZ) maxZ = gz;
        }

        // MagicaVoxel: X=width, Y=depth, Z=up
        // Unity:        X=width, Y=up,    Z=depth
        // Swap: Unity Y ← MagicaVoxel Z ;  Unity Z ← MagicaVoxel Y
        var model = new Model
        {
            sizeX = maxX - minX + 1,
            sizeY = maxZ - minZ + 1,   // altura
            sizeZ = maxY - minY + 1,   // profundidade
        };

        int total = model.sizeX * model.sizeY * model.sizeZ;
        model.voxels = new byte[total];

        // ---------- Paleta dinâmica ----------
        var paletteList = new List<Color32>(256);
        var paletteMap = new Dictionary<Color32, byte>(256);
        paletteList.Add(new Color32(255, 255, 255, 255));

        foreach (var v in source.Voxels)
        {
            Color32 color = new Color32(v.Color.R, v.Color.G, v.Color.B, v.Color.A);

            if (!paletteMap.TryGetValue(color, out byte idx))
            {
                if (paletteList.Count >= 256) idx = 255;
                else { idx = (byte)paletteList.Count; paletteList.Add(color); }
                paletteMap[color] = idx;
            }

            // Swap Y↔Z
            int x = Mathf.RoundToInt(v.GlobalPosition.X) - minX;
            int y = Mathf.RoundToInt(v.GlobalPosition.Z) - minZ;   // MagicaVoxel Z → Unity Y
            int z = Mathf.RoundToInt(v.GlobalPosition.Y) - minY;   // MagicaVoxel Y → Unity Z

            int gi = x + y * model.sizeX + z * model.sizeX * model.sizeY;
            if (gi >= 0 && gi < total)
                model.voxels[gi] = idx;
        }

        model.palette = new Color32[256];
        for (int i = 0; i < 256; i++)
            model.palette[i] = i < paletteList.Count
                ? paletteList[i]
                : new Color32(255, 255, 255, 255);

        return model;
    }
}